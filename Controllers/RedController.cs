using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using it_ops_backend.DTOs;
using it_ops_backend.Services.Interfaces;

namespace it_ops_backend.Controllers;

[Authorize]
[ApiController]
[Route("api/red")]
public class RedController : ControllerBase
{
    private readonly IRedService _redService;

    public RedController(IRedService redService)
    {
        _redService = redService;
    }

    [HttpGet("dispositivos")]
    public async Task<IActionResult> GetDispositivos()
    {
        var list = await _redService.GetDispositivosAsync();
        return Ok(list);
    }

    [HttpGet("dispositivos/{id:guid}")]
    public async Task<IActionResult> GetDispositivo(Guid id)
    {
        var disp = await _redService.GetDispositivoAsync(id);
        if (disp == null) return NotFound(new { message = "Dispositivo no encontrado." });
        return Ok(disp);
    }

    [HttpPost("dispositivos")]
    public async Task<IActionResult> CreateDispositivo([FromBody] CreateDispositivoRedDto dto)
    {
        var created = await _redService.CreateDispositivoAsync(dto);
        return CreatedAtAction(nameof(GetDispositivo), new { id = created.Id }, created);
    }

    [HttpPatch("dispositivos/{id:guid}")]
    public async Task<IActionResult> UpdateDispositivo(Guid id, [FromBody] CreateDispositivoRedDto dto)
    {
        var updated = await _redService.UpdateDispositivoAsync(id, dto);
        if (updated == null) return NotFound(new { message = "Dispositivo no encontrado." });
        return Ok(updated);
    }

    [HttpPost("dispositivos/simular-caida")]
    public async Task<IActionResult> SimularCaida()
    {
        var victima = await _redService.SimularCaidaAsync();
        return Ok(victima);
    }

    [HttpPatch("dispositivos/{id:guid}/restaurar")]
    public async Task<IActionResult> RestaurarDispositivo(Guid id, [FromBody] RestaurarDispositivoDto? dto)
    {
        Guid? tecnicoId = null;
        if (!string.IsNullOrWhiteSpace(dto?.TecnicoId) && Guid.TryParse(dto.TecnicoId, out var parsedGuid))
        {
            tecnicoId = parsedGuid;
        }
        else
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (Guid.TryParse(userIdStr, out var currentGuid))
            {
                tecnicoId = currentGuid;
            }
        }

        var result = await _redService.RestaurarDispositivoAsync(id, tecnicoId);
        if (result == null) return NotFound(new { message = "Dispositivo no encontrado." });
        return Ok(result);
    }

    [HttpGet("ips")]
    public async Task<IActionResult> GetIPs()
    {
        var ips = await _redService.GetIPsAsync();
        return Ok(ips);
    }

    [HttpPost("ips")]
    public async Task<IActionResult> RegistrarIP([FromBody] RegistrarIpDto dto)
    {
        var ip = await _redService.RegistrarIPAsync(dto);
        return Ok(ip);
    }

    [HttpPatch("ips/{ip}/asignar")]
    public async Task<IActionResult> AsignarIP(string ip, [FromBody] AsignarIpDto dto)
    {
        var updated = await _redService.AsignarIPAsync(ip, dto.DispositivoId);
        if (updated == null) return NotFound(new { message = "IP no encontrada." });
        return Ok(updated);
    }

    [HttpPatch("ips/{ip}/liberar")]
    public async Task<IActionResult> LiberarIP(string ip)
    {
        var updated = await _redService.LiberarIPAsync(ip);
        if (updated == null) return NotFound(new { message = "IP no encontrada." });
        return Ok(updated);
    }
}

