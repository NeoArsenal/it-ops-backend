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

    public ActivosController(IActivosService activosService)
    {
        _activosService = activosService;
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

        var created = await _activosService.CreateAsync(activo);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [Authorize]
    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateActivoDto dto)
    {
        var updated = await _activosService.UpdateAsync(id, a =>
        {
            if (dto.Codigo != null) a.Codigo = dto.Codigo;
            if (dto.Tipo != null) a.Tipo = dto.Tipo;
            if (dto.Estado != null) a.Estado = dto.Estado;
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
        });

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

    [Authorize]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await _activosService.DeleteAsync(id);
        if (!success) return NotFound(new { message = "Activo no encontrado." });
        return Ok(new { success = true });
    }
}

