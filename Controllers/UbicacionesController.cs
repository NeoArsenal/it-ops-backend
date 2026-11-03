using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using it_ops_backend.DTOs;
using it_ops_backend.Models;
using it_ops_backend.Services.Interfaces;

namespace it_ops_backend.Controllers;

[ApiController]
[Route("api/ubicaciones")]
[Route("ubicaciones")]
public class UbicacionesController : ControllerBase
{
    private readonly IUbicacionesService _ubicacionesService;

    public UbicacionesController(IUbicacionesService ubicacionesService)
    {
        _ubicacionesService = ubicacionesService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _ubicacionesService.GetAllAsync();
        return Ok(result);
    }

    [HttpGet("sedes")]
    public async Task<IActionResult> GetSedes()
    {
        var sedes = await _ubicacionesService.GetSedesAsync();
        return Ok(sedes);
    }

    [HttpGet("departamentos")]
    public async Task<IActionResult> GetDepartamentos([FromQuery] string? sede)
    {
        if (string.IsNullOrWhiteSpace(sede)) return Ok(new List<string>());
        var depts = await _ubicacionesService.GetDepartamentosPorSedeAsync(sede);
        return Ok(depts);
    }

    [HttpGet("sedes/{sede}/departamentos")]
    public async Task<IActionResult> GetDepartamentosRoute(string sede)
    {
        if (string.IsNullOrWhiteSpace(sede)) return Ok(new List<string>());
        var depts = await _ubicacionesService.GetDepartamentosPorSedeAsync(sede);
        return Ok(depts);
    }

    [HttpGet("areas")]
    public async Task<IActionResult> GetAreas([FromQuery] string? sede, [FromQuery] string? departamento)
    {
        if (string.IsNullOrWhiteSpace(sede) || string.IsNullOrWhiteSpace(departamento)) 
            return Ok(new List<string>());

        var areas = await _ubicacionesService.GetAreasAsync(sede, departamento);
        return Ok(areas);
    }

    [HttpGet("sedes/{sede}/departamentos/{departamento}/areas")]
    public async Task<IActionResult> GetAreasRoute(string sede, string departamento)
    {
        if (string.IsNullOrWhiteSpace(sede) || string.IsNullOrWhiteSpace(departamento)) 
            return Ok(new List<string>());

        var areas = await _ubicacionesService.GetAreasAsync(sede, departamento);
        return Ok(areas);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUbicacionDto dto)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue("id")
                     ?? User.FindFirstValue("sub");
        Guid? userId = Guid.TryParse(userIdStr, out var parsedGuid) ? parsedGuid : null;

        var ubicacion = new Ubicacion
        {
            Sede = dto.Sede,
            Departamento = dto.Departamento,
            Area = dto.Area
        };

        var created = await _ubicacionesService.CreateAsync(ubicacion, userId);
        return Ok(created);
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _ubicacionesService.DeleteAsync(id);
        if (!deleted) return NotFound(new { message = "Ubicación no encontrada" });
        return Ok(new { success = true, message = "Ubicacion eliminada" });
    }
}
