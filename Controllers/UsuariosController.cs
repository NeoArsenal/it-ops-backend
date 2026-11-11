using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using it_ops_backend.DTOs;
using it_ops_backend.Services.Interfaces;

namespace it_ops_backend.Controllers;

[Authorize]
[ApiController]
[Route("api/usuarios")]
[EnableRateLimiting("GeneralApiPolicy")]
public class UsuariosController : ControllerBase
{
    private readonly IUsuariosService _usuariosService;
    private readonly IStorageService _storageService;

    public UsuariosController(IUsuariosService usuariosService, IStorageService storageService)
    {
        _usuariosService = usuariosService;
        _storageService = storageService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var usuarios = await _usuariosService.GetAllAsync();
        return Ok(usuarios);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var usuario = await _usuariosService.GetByIdAsync(id);
        if (usuario == null) return NotFound(new { message = "Usuario no encontrado." });
        return Ok(usuario);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUsuarioDto dto)
    {
        var created = await _usuariosService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUsuarioDto dto)
    {
        var updated = await _usuariosService.UpdateAsync(id, dto);
        if (updated == null) return NotFound(new { message = "Usuario no encontrado." });
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid? currentUserId = Guid.TryParse(userIdStr, out var parsed) ? parsed : null;

        try
        {
            var deleted = await _usuariosService.DeleteAsync(id, currentUserId);
            if (!deleted) return NotFound(new { message = "Usuario no encontrado." });
            return Ok(new { deleted = true, id });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/preferencias")]
    public async Task<IActionResult> UpdatePreferencias(Guid id, [FromBody] UpdatePreferenciasDto dto)
    {
        var updated = await _usuariosService.UpdatePreferenciasAsync(id, dto);
        if (updated == null) return NotFound(new { message = "Usuario no encontrado." });
        return Ok(updated);
    }

    [HttpPost("upload-avatar")]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No se proporcionó ningún archivo." });
        }
        if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Solo se permiten imágenes (JPG, PNG, WebP)." });
        }
        if (file.Length > 5 * 1024 * 1024)
        {
            return BadRequest(new { message = "La imagen no debe superar los 5MB." });
        }

        var url = await _storageService.UploadFileAsync(file, "perfiles");
        return Ok(new { url });
    }
}

