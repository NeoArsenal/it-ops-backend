using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using it_ops_backend.Services.Interfaces;

namespace it_ops_backend.Controllers;

[Authorize]
[ApiController]
[Route("api/gamificacion")]
public class GamificacionController : ControllerBase
{
    private readonly IGamificacionService _gamificacionService;

    public GamificacionController(IGamificacionService gamificacionService)
    {
        _gamificacionService = gamificacionService;
    }

    [HttpGet("leaderboard")]
    public async Task<IActionResult> GetLeaderboard()
    {
        var leaderboard = await _gamificacionService.GetLeaderboardAsync();
        return Ok(leaderboard);
    }

    [HttpGet("perfil/{id:guid}")]
    public async Task<IActionResult> GetPerfil(Guid id)
    {
        var perfil = await _gamificacionService.GetPerfilAsync(id);
        if (perfil == null) return NotFound(new { message = "Perfil no encontrado." });
        return Ok(perfil);
    }

    [HttpGet("historial/{userId:guid}")]
    public async Task<IActionResult> GetHistorial(Guid userId)
    {
        var historial = await _gamificacionService.GetHistorialAsync(userId);
        return Ok(historial);
    }

    [HttpGet("medallas/{userId:guid}")]
    public async Task<IActionResult> GetMedallas(Guid userId)
    {
        var medallas = await _gamificacionService.GetMedallasUsuarioAsync(userId);
        return Ok(medallas);
    }
}

