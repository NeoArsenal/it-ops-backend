using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using it_ops_backend.DTOs;
using it_ops_backend.Services.Interfaces;

namespace it_ops_backend.Controllers;

[Authorize]
[ApiController]
[Route("api/guias")]
public class GuiasController : ControllerBase
{
    private readonly IGuiasService _guiasService;

    public GuiasController(IGuiasService guiasService)
    {
        _guiasService = guiasService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var guias = await _guiasService.GetAllAsync();
        return Ok(guias);
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        var guias = await _guiasService.SearchAsync(q);
        return Ok(guias);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var guia = await _guiasService.GetByIdAsync(id);
        if (guia == null) return NotFound(new { message = "Guía no encontrada." });
        return Ok(guia);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateGuiaDto dto)
    {
        var created = await _guiasService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _guiasService.DeleteAsync(id);
        if (!deleted) return NotFound(new { message = "Guía no encontrada." });
        return Ok(new { deleted = true });
    }
}

