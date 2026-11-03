using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using it_ops_backend.Data;
using it_ops_backend.Models;
using it_ops_backend.Services.Interfaces;

namespace it_ops_backend.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _config;

    public AuthService(ApplicationDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    public async Task<(string token, Usuario user)?> LoginAsync(string email, string password)
    {
        var cleanEmail = email.Trim().ToLower();
        var user = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Email.ToLower() == cleanEmail);

        if (user == null) return null;

        // Validar contraseña hasheada con BCrypt
        bool isValid = BCrypt.Net.BCrypt.Verify(password, user.Password);
        if (!isValid) return null;

        // Generar JWT
        var tokenHandler = new JwtSecurityTokenHandler();
        var secret = _config["Jwt:Secret"] ?? "super-secret-key-cambiar-en-produccion";
        var key = Encoding.UTF8.GetBytes(secret);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Nombre),
                new Claim(ClaimTypes.Role, user.Rol)
            }),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        return (tokenString, user);
    }
}

