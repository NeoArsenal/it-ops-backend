using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using it_ops_backend.DTOs;
using it_ops_backend.Models;
using it_ops_backend.Services.Interfaces;

namespace it_ops_backend.Controllers;

[ApiController]
[Route("api/tickets")]
public class TicketsController : ControllerBase
{
    private readonly ITicketsService _ticketsService;
    private readonly IStorageService _storageService;

    public TicketsController(ITicketsService ticketsService, IStorageService storageService)
    {
        _ticketsService = ticketsService;
        _storageService = storageService;
    }

    // Ruta protegida por JWT para personal TI
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var tickets = await _ticketsService.GetAllAsync();
        return Ok(tickets);
    }

    // Ruta pública sanitizada
    [HttpGet("public")]
    public async Task<IActionResult> GetPublicAll()
    {
        var tickets = await _ticketsService.GetPublicAllAsync();
        return Ok(tickets);
    }

    // Ruta pública para la pantalla y el portal
    [HttpGet("public/active")]
    public async Task<IActionResult> GetPublicActive()
    {
        var tickets = await _ticketsService.GetPublicActiveAsync();
        return Ok(tickets);
    }

    // Seguimiento público por código o teléfono
    [HttpGet("track")]
    public async Task<IActionResult> TrackTicket([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 3)
        {
            return NotFound(new { message = "Ingresa al menos 3 caracteres del código o teléfono" });
        }
        var results = await _ticketsService.TrackTicketAsync(q);
        if (results.Count == 0)
        {
            return NotFound(new { message = $"No se encontró ningún ticket con \"{q}\"" });
        }
        return Ok(results);
    }

    [Authorize]
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var stats = await _ticketsService.GetStatsAsync();
        return Ok(stats);
    }

    [Authorize]
    [HttpGet("analytics")]
    public async Task<IActionResult> GetAnalytics([FromQuery] string? sede)
    {
        var analytics = await _ticketsService.GetAnalyticsAsync(sede);
        return Ok(analytics);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var ticket = await _ticketsService.GetByIdAsync(id);
        if (ticket == null) return NotFound(new { message = $"Ticket con ID {id} no encontrado." });
        return Ok(ticket);
    }

    // Historial y auditoría
    [Authorize]
    [HttpGet("{id:guid}/historial")]
    public async Task<IActionResult> GetHistorial(Guid id)
    {
        var historial = await _ticketsService.GetHistorialAsync(id);
        return Ok(historial);
    }

    // Subida de foto de evidencia a Supabase Storage
    [HttpPost("upload-foto")]
    public async Task<IActionResult> UploadFoto(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No se proporcionó ningún archivo." });
        }
        if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Solo se permiten archivos de imagen." });
        }

        var url = await _storageService.UploadFileAsync(file, "evidencias");
        return Ok(new { url });
    }

    // Crear ticket desde el portal o panel
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTicketDto dto)
    {
        // Protección Honeypot
        if (!string.IsNullOrWhiteSpace(dto.Website))
        {
            return Ok(new
            {
                id = "bot-discarded",
                titulo = dto.Titulo,
                estado = "ABIERTO",
                prioridad = "BAJA",
                xpRecompensa = 0,
                creadoEn = DateTime.UtcNow,
                actualizadoEn = DateTime.UtcNow
            });
        }

        var ticket = new Ticket
        {
            Titulo = dto.Titulo,
            Descripcion = dto.Descripcion,
            Prioridad = dto.Prioridad ?? "MEDIA",
            Solicitante = dto.Solicitante,
            SolicitanteNombre = dto.SolicitanteNombre,
            SolicitanteContacto = dto.SolicitanteContacto,
            Departamento = dto.Departamento ?? "General",
            Sede = dto.Sede,
            UbicacionEspecifica = dto.UbicacionEspecifica,
            UbicacionId = dto.UbicacionId,
            FotoUrl = dto.FotoUrl,
            CreadoEn = DateTime.UtcNow,
            ActualizadoEn = DateTime.UtcNow
        };

        var created = await _ticketsService.CreateAsync(ticket, dto.SolicitanteNombre);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    // Actualizar estado / prioridad / solución
    [Authorize]
    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTicketDto dto)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid? userId = Guid.TryParse(userIdStr, out var parsedGuid) ? parsedGuid : null;
        var userName = User.FindFirstValue(ClaimTypes.Name) ?? "Técnico TI";

        var updated = await _ticketsService.UpdateAsync(id, ticket =>
        {
            if (dto.Titulo != null) ticket.Titulo = dto.Titulo;
            if (dto.Descripcion != null) ticket.Descripcion = dto.Descripcion;
            if (dto.Estado != null) ticket.Estado = dto.Estado;
            if (dto.Prioridad != null) ticket.Prioridad = dto.Prioridad;
            if (dto.AsignadoAId.HasValue) ticket.AsignadoAId = dto.AsignadoAId.Value;
            if (dto.Solucion != null) ticket.Solucion = dto.Solucion;
        }, userId, userName);

        if (updated == null) return NotFound(new { message = "Ticket no encontrado." });
        return Ok(updated);
    }

    [Authorize]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await _ticketsService.DeleteAsync(id);
        if (!success) return NotFound(new { message = "Ticket no encontrado." });
        return Ok(new { success = true });
    }
}
