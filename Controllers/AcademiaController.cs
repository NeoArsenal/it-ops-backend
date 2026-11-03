using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using it_ops_backend.DTOs;
using it_ops_backend.Services.Interfaces;

namespace it_ops_backend.Controllers;

[Authorize]
[ApiController]
[Route("api/academia")]
public class AcademiaController : ControllerBase
{
    private readonly IAcademiaService _academiaService;
    private static readonly Guid DefaultUserId = Guid.Parse("4e447e7e-0824-49d1-9abd-490209ad77de");

    public AcademiaController(IAcademiaService academiaService)
    {
        _academiaService = academiaService;
    }

    [HttpGet("cursos")]
    public async Task<IActionResult> GetCursos([FromQuery] Guid? userId)
    {
        var resolvedUserId = userId ?? GetCurrentUserId() ?? DefaultUserId;
        var cursos = await _academiaService.GetCursosAsync(resolvedUserId);
        return Ok(cursos);
    }

    [HttpGet("niveles/{id:guid}/preguntas")]
    public async Task<IActionResult> GetNivelConPreguntas(Guid id)
    {
        var nivel = await _academiaService.GetNivelConPreguntasAsync(id);
        if (nivel == null) return NotFound(new { message = "Nivel no encontrado." });
        return Ok(nivel);
    }

    [HttpPost("completar")]
    public async Task<IActionResult> CompletarNivel([FromBody] CompletarNivelDto dto)
    {
        var resolvedUserId = dto.UserId ?? GetCurrentUserId() ?? DefaultUserId;
        var result = await _academiaService.CompletarNivelAsync(resolvedUserId, dto.NivelId);
        return Ok(result);
    }

    private Guid? GetCurrentUserId()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdStr, out var parsed) ? parsed : null;
    }
}

