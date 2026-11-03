using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using it_ops_backend.DTOs;
using it_ops_backend.Services.Interfaces;

namespace it_ops_backend.Controllers;

[ApiController]
[Route("api/configuracion")]
public class ConfiguracionController : ControllerBase
{
    private readonly IConfiguracionService _configuracionService;

    public ConfiguracionController(IConfiguracionService configuracionService)
    {
        _configuracionService = configuracionService;
    }

    [HttpPost("verify-access")]
    public async Task<IActionResult> VerifyAccess([FromBody] VerifyAccessDto dto)
    {
        var isValid = await _configuracionService.VerifyAccessAsync(dto.Pin, dto.Token);
        return Ok(new { valid = isValid });
    }

    [HttpPost("verify-pin")]
    public async Task<IActionResult> VerifyPin([FromBody] VerifyAccessDto dto)
    {
        var isValid = await _configuracionService.VerifyPinAsync(dto.Pin ?? "");
        return Ok(new { valid = isValid });
    }

    [Authorize]
    [HttpGet("portal-config")]
    public async Task<IActionResult> GetPortalConfig()
    {
        var pin = await _configuracionService.GetValueAsync("PORTAL_PIN", "2026");
        var token = await _configuracionService.GetPortalTokenAsync();
        return Ok(new { pin, token });
    }

    [Authorize]
    [HttpGet("portal-pin")]
    public async Task<IActionResult> GetPortalPin()
    {
        var pin = await _configuracionService.GetValueAsync("PORTAL_PIN", "2026");
        return Ok(new { pin });
    }

    [Authorize]
    [HttpPost("portal-pin")]
    public async Task<IActionResult> SetPortalPin([FromBody] VerifyAccessDto dto)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid? userId = Guid.TryParse(userIdStr, out var parsed) ? parsed : null;
        await _configuracionService.SetValueAsync("PORTAL_PIN", dto.Pin ?? "2026", userId);
        return Ok(new { success = true });
    }

    [Authorize]
    [HttpPost("portal-token/regenerate")]
    public async Task<IActionResult> RegeneratePortalToken()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid? userId = Guid.TryParse(userIdStr, out var parsed) ? parsed : null;
        var token = await _configuracionService.RegeneratePortalTokenAsync(userId);
        return Ok(new { token });
    }

    [HttpGet("catalogos")]
    public async Task<IActionResult> GetCatalogos()
    {
        var catalogos = await _configuracionService.GetCatalogosAsync();
        return Ok(catalogos);
    }

    [Authorize]
    [HttpPut("catalogos/{tipo}")]
    public async Task<IActionResult> SetCatalogo(string tipo, [FromBody] SetCatalogoDto dto)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid? userId = Guid.TryParse(userIdStr, out var parsed) ? parsed : null;
        var items = await _configuracionService.SetCatalogoAsync(tipo, dto.Items, userId);
        return Ok(items);
    }

    [HttpGet("gamificacion")]
    public async Task<IActionResult> GetGamificacionConfig()
    {
        var reglas = await _configuracionService.GetReglasGamificacionAsync();
        return Ok(reglas);
    }

    [Authorize]
    [HttpPut("gamificacion")]
    public async Task<IActionResult> SetGamificacionConfig([FromBody] ReglasGamificacionDto dto)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid? userId = Guid.TryParse(userIdStr, out var parsed) ? parsed : null;
        var updated = await _configuracionService.SetReglasGamificacionAsync(dto, userId);
        return Ok(updated);
    }
}

