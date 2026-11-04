using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using it_ops_backend.DTOs;
using it_ops_backend.Models;
using it_ops_backend.Services.Interfaces;

namespace it_ops_backend.Controllers;

[ApiController]
[Route("api/activos")]
public class ActivosController : ControllerBase
{
    private readonly IActivosService _activosService;
    private readonly IStorageService _storageService;

    public ActivosController(IActivosService activosService, IStorageService storageService)
    {
        _activosService = activosService;
        _storageService = storageService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var activos = await _activosService.GetAllAsync();
        return Ok(activos);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var activo = await _activosService.GetByIdAsync(id);
        if (activo == null) return NotFound(new { message = "Activo no encontrado." });
        return Ok(activo);
    }

    [HttpGet("codigo/{codigo}")]
    public async Task<IActionResult> GetByCodigo(string codigo)
    {
        var activo = await _activosService.GetByCodigoAsync(codigo);
        if (activo == null) return NotFound(new { message = $"No se encontró el activo con código {codigo}." });
        return Ok(activo);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateActivoDto dto)
    {
        var activo = new Activo
        {
            Codigo = dto.Codigo,
            Tipo = dto.Tipo,
            Estado = dto.Estado ?? "DISPONIBLE",
            Marca = dto.Marca,
            Modelo = dto.Modelo,
            NumeroSerie = dto.NumeroSerie,
            CodigoFactura = dto.CodigoFactura,
            Sede = dto.Sede,
            Departamento = dto.Departamento,
            Ubicacion = dto.Ubicacion,
            Responsable = dto.Responsable ?? dto.ResponsableNombre,
            Observaciones = dto.Observaciones,
            UbicacionId = dto.UbicacionId,
            RegistradoPorId = dto.RegistradoPorId,
            FechaRegistro = DateTime.UtcNow,
            FechaActualizacion = DateTime.UtcNow
        };

        var created = await _activosService.CreateAsync(activo, GetCurrentUserId() ?? dto.RegistradoPorId);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Obtiene el Id del usuario autenticado desde el JWT.</summary>
    private Guid? GetCurrentUserId()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdStr, out var parsed) ? parsed : null;
    }

    [Authorize]
    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateActivoDto dto)
    {
        // La trazabilidad (antes → después) se registra automáticamente en el servicio
        var tecnicoId = GetCurrentUserId() ?? dto.TecnicoId;
        var updated = await _activosService.UpdateAsync(id, a =>
        {
            // Código y Tipo son obligatorios: nunca se sobrescriben con vacío
            if (!string.IsNullOrWhiteSpace(dto.Codigo)) a.Codigo = dto.Codigo.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Tipo)) a.Tipo = dto.Tipo.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Estado)) a.Estado = dto.Estado;
            if (dto.Marca != null) a.Marca = dto.Marca;
            if (dto.Modelo != null) a.Modelo = dto.Modelo;
            if (dto.NumeroSerie != null) a.NumeroSerie = dto.NumeroSerie;
            if (dto.CodigoFactura != null) a.CodigoFactura = dto.CodigoFactura;
            if (dto.Sede != null) a.Sede = dto.Sede;
            if (dto.Departamento != null) a.Departamento = dto.Departamento;
            if (dto.Ubicacion != null) a.Ubicacion = dto.Ubicacion;
            if (dto.UbicacionId.HasValue) a.UbicacionId = dto.UbicacionId;
            if (dto.Responsable != null) a.Responsable = dto.Responsable;
            if (dto.ResponsableNombre != null) a.Responsable = dto.ResponsableNombre;
            if (dto.Observaciones != null) a.Observaciones = dto.Observaciones;
        }, tecnicoId);

        if (updated == null) return NotFound(new { message = "Activo no encontrado." });
        return Ok(updated);
    }

    // Agregar intervención (desde escaneo QR o mantenimiento)
    [HttpPost("{id:guid}/intervenciones")]
    public async Task<IActionResult> AddIntervencion(Guid id, [FromBody] CreateIntervencionDto dto)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid? userId = Guid.TryParse(userIdStr, out var parsedGuid) ? parsedGuid : null;

        var intervencion = await _activosService.AddIntervencionAsync(id, dto.Descripcion, userId);
        return Ok(intervencion);
    }

    // Subir acta/cargo de entrega a Supabase Storage (carpeta 'cargos') y registrar en trazabilidad
    [Authorize]
    [HttpPost("{id:guid}/cargo")]
    public async Task<IActionResult> UploadCargo(
        Guid id, 
        IFormFile file, 
        [FromForm] string? cargoNumero, 
        [FromForm] string? responsable, 
        [FromForm] string? dni, 
        [FromForm] string? tecnico)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No se proporcionó ningún archivo de cargo." });
        }

        var activo = await _activosService.GetByIdAsync(id);
        if (activo == null)
        {
            return NotFound(new { message = "Activo no encontrado." });
        }

        // Subir a Supabase Storage dentro de la carpeta 'cargos'
        var url = await _storageService.UploadFileAsync(file, "cargos");

        // Registrar en el historial de intervenciones del equipo
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid? tecnicoId = Guid.TryParse(userIdStr, out var parsedGuid) ? parsedGuid : null;

        var num = !string.IsNullOrWhiteSpace(cargoNumero) ? cargoNumero : "S/N";
        var resp = !string.IsNullOrWhiteSpace(responsable) ? responsable : (activo.Responsable ?? "Usuario");
        var dniStr = !string.IsNullOrWhiteSpace(dni) ? $" (DNI: {dni})" : "";
        var tecStr = !string.IsNullOrWhiteSpace(tecnico) ? $" por {tecnico}" : "";

        var descripcion = $"[CARGO] Acta de Entrega N° {num} firmada por {resp}{dniStr}{tecStr}. Archivo: {url}";
        var intervencion = await _activosService.AddIntervencionAsync(id, descripcion, tecnicoId);

        return Ok(new 
        { 
            url, 
            cargoNumero = num, 
            intervencionId = intervencion.Id,
            message = "Cargo almacenado exitosamente en la carpeta 'cargos' y registrado en la trazabilidad del activo." 
        });
    }

    // Subida general de cargo
    [Authorize]
    [HttpPost("upload-cargo")]
    public async Task<IActionResult> UploadCargoGeneral(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No se proporcionó ningún archivo." });
        }

        var url = await _storageService.UploadFileAsync(file, "cargos");
        return Ok(new { url });
    }

    [Authorize]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await _activosService.DeleteAsync(id);
        if (!success) return NotFound(new { message = "Activo no encontrado." });
        return Ok(new { success = true });
    }
}

