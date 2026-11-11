using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using it_ops_backend.DTOs;
using it_ops_backend.Services.Interfaces;

namespace it_ops_backend.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("GeneralApiPolicy")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IUsuariosService _usuariosService;

    public AuthController(IAuthService authService, IUsuariosService usuariosService)
    {
        _authService = authService;
        _usuariosService = usuariosService;
    }

    [HttpPost("login")]
    [EnableRateLimiting("LoginPolicy")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var result = await _authService.LoginAsync(dto.Email, dto.Password);
        if (result == null)
        {
            return Unauthorized(new { message = "Credenciales incorrectas o usuario no registrado." });
        }

        var (token, user) = result.Value;

        var userDto = new UsuarioDto
        {
            Id = user.Id,
            Nombre = user.Nombre,
            Email = user.Email,
            Rol = user.Rol,
            Nivel = user.Nivel,
            XpActual = user.XpActual,
            Avatar = user.Avatar,
            TituloRPG = user.TituloRPG,
            ModulosAccesibles = user.ModulosAccesibles
        };

        return Ok(new AuthResponseDto
        {
            AccessToken = token,
            access_token = token,
            User = userDto,
            Usuario = userDto
        });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var user = await _usuariosService.GetByIdAsync(userId);
        if (user == null) return NotFound(new { message = "Usuario no encontrado." });

        return Ok(user);
    }
}

