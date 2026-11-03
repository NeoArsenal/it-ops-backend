using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using it_ops_backend.Data;
using it_ops_backend.DTOs;
using it_ops_backend.Models;
using it_ops_backend.Services.Interfaces;

namespace it_ops_backend.Services;

public class UsuariosService : IUsuariosService
{
    private readonly ApplicationDbContext _context;

    public UsuariosService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Usuario>> GetAllAsync()
    {
        return await _context.Usuarios
            .OrderByDescending(u => u.XpActual)
            .ToListAsync();
    }

    public async Task<Usuario?> GetByIdAsync(Guid id)
    {
        return await _context.Usuarios.FindAsync(id);
    }

    public async Task<Usuario> CreateAsync(CreateUsuarioDto dto)
    {
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(
            !string.IsNullOrWhiteSpace(dto.Password) ? dto.Password : "ItOps2026!"
        );

        var usuario = new Usuario
        {
            Nombre = dto.Nombre,
            Email = dto.Email,
            Password = hashedPassword,
            Rol = dto.Rol ?? "TECNICO",
            Avatar = !string.IsNullOrWhiteSpace(dto.Avatar)
                ? dto.Avatar
                : $"https://api.dicebear.com/9.x/bottts-neutral/svg?seed={Uri.EscapeDataString(dto.Nombre)}",
            TituloRPG = dto.TituloRPG ?? "Aprendiz de Soporte",
            ModulosAccesiblesRaw = FormatModulos(dto.ModulosAccesibles)
        };

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();
        return usuario;
    }

    public async Task<Usuario?> UpdateAsync(Guid id, UpdateUsuarioDto dto)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null) return null;

        if (!string.IsNullOrWhiteSpace(dto.Nombre)) usuario.Nombre = dto.Nombre;
        if (!string.IsNullOrWhiteSpace(dto.Email)) usuario.Email = dto.Email;
        if (!string.IsNullOrWhiteSpace(dto.Rol)) usuario.Rol = dto.Rol;
        if (!string.IsNullOrWhiteSpace(dto.Avatar)) usuario.Avatar = dto.Avatar;
        if (dto.TituloRPG != null) usuario.TituloRPG = dto.TituloRPG;
        if (dto.ModulosAccesibles != null) usuario.ModulosAccesiblesRaw = FormatModulos(dto.ModulosAccesibles);

        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            usuario.Password = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        }

        await _context.SaveChangesAsync();
        return usuario;
    }

    private static string? FormatModulos(object? raw)
    {
        if (raw == null) return null;
        if (raw is JsonElement elem)
        {
            if (elem.ValueKind == JsonValueKind.Array)
            {
                var items = elem.EnumerateArray().Select(x => x.GetString()).Where(x => !string.IsNullOrEmpty(x));
                return string.Join(",", items);
            }
            if (elem.ValueKind == JsonValueKind.String)
            {
                return elem.GetString();
            }
        }
        if (raw is IEnumerable<string> list)
        {
            return string.Join(",", list);
        }
        return raw.ToString();
    }

    public async Task<bool> DeleteAsync(Guid id, Guid? currentUserId)
    {
        if (currentUserId.HasValue && currentUserId.Value == id)
        {
            throw new InvalidOperationException("No puedes eliminar tu propia cuenta activa.");
        }

        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null) return false;

        _context.Usuarios.Remove(usuario);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<Usuario?> UpdatePreferenciasAsync(Guid id, UpdatePreferenciasDto dto)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null) return null;

        if (!string.IsNullOrWhiteSpace(dto.Avatar)) usuario.Avatar = dto.Avatar;
        if (dto.TituloRPG != null) usuario.TituloRPG = dto.TituloRPG;
        if (dto.Preferencias != null)
        {
            usuario.Preferencias = dto.Preferencias is string str
                ? str
                : JsonSerializer.Serialize(dto.Preferencias);
        }

        await _context.SaveChangesAsync();
        return usuario;
    }
}

