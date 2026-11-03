using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using it_ops_backend.DTOs;
using it_ops_backend.Models;
using it_ops_backend.Services.Interfaces;

namespace it_ops_backend.Controllers;

[ApiController]
[Route("api/ubicaciones")]
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

    [HttpGet("sedes/{sede}/departamentos")]
    public async Task<IActionResult> GetDepartamentos(string sede)
    {
        var depts = await _ubicacionesService.GetDepartamentosPorSedeAsync(sede);
        return Ok(depts);
    }

    [HttpGet("sedes/{sede}/departamentos/{departamento}/areas")]
    public async Task<IActionResult> GetAreas(string sede, string departamento)
    {
        var areas = await _ubicacionesService.GetAreasAsync(sede, departamento);
        return Ok(areas);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUbicacionDto dto)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
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
}

