using Microsoft.EntityFrameworkCore;
using it_ops_backend.Data;
using it_ops_backend.Models;
using it_ops_backend.Services.Interfaces;

namespace it_ops_backend.Services;

public class GamificacionService : IGamificacionService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguracionService _configService;

    public GamificacionService(ApplicationDbContext context, IConfiguracionService configService)
    {
        _context = context;
        _configService = configService;
    }

    private async Task<int> CalcularNivelAsync(int xp)
    {
        var config = await _configService.GetReglasGamificacionAsync();
        var niveles = config.Niveles;
        for (var i = niveles.Count - 1; i >= 0; i--)
        {
            if (xp >= niveles[i]) return i + 1;
        }
        return 1;
    }

    public async Task<object> OtorgarXPAsync(Guid usuarioId, int xp, string accion, string? descripcion = null)
    {
        var registro = new HistorialXP
        {
            UsuarioId = usuarioId,
            XpOtorgado = xp,
            Accion = accion,
            Descripcion = descripcion,
            Fecha = DateTime.UtcNow
        };
        _context.HistorialesXP.Add(registro);

        var usuario = await _context.Usuarios.FindAsync(usuarioId)
            ?? throw new KeyNotFoundException("Usuario no encontrado.");

        usuario.XpActual += xp;
        usuario.Nivel = await CalcularNivelAsync(usuario.XpActual);

        await _context.SaveChangesAsync();

        // Verificar medallas desbloqueables
        var todasMedallas = await _context.Medallas.ToListAsync();
        var desbloqueadas = await _context.UsuariosMedallas
            .Where(um => um.UsuarioId == usuario.Id)
            .Select(um => um.MedallaId)
            .ToListAsync();
        var setDesbloqueadas = desbloqueadas.ToHashSet();

        foreach (var medalla in todasMedallas)
        {
            if (!setDesbloqueadas.Contains(medalla.Id) && usuario.XpActual >= medalla.CondicionXP)
            {
                _context.UsuariosMedallas.Add(new UsuarioMedalla
                {
                    UsuarioId = usuario.Id,
                    MedallaId = medalla.Id,
                    DesbloqueadoEn = DateTime.UtcNow
                });
            }
        }
        await _context.SaveChangesAsync();

        return new { xpOtorgado = xp, nuevoXP = usuario.XpActual, nivel = usuario.Nivel };
    }

    public async Task<List<object>> GetLeaderboardAsync(int limit = 10)
    {
        var list = await _context.Usuarios
            .OrderByDescending(u => u.XpActual)
            .Take(limit)
            .Select(u => new
            {
                id = u.Id,
                nombre = u.Nombre,
                email = u.Email,
                nivel = u.Nivel,
                xpActual = u.XpActual,
                avatar = u.Avatar,
                tituloRPG = u.TituloRPG
            })
            .ToListAsync();

        return list.Cast<object>().ToList();
    }

    public async Task<List<HistorialXP>> GetHistorialAsync(Guid usuarioId)
    {
        return await _context.HistorialesXP
            .Where(h => h.UsuarioId == usuarioId)
            .OrderByDescending(h => h.Fecha)
            .Take(50)
            .ToListAsync();
    }

    public async Task<List<UsuarioMedalla>> GetMedallasUsuarioAsync(Guid usuarioId)
    {
        return await _context.UsuariosMedallas
            .Include(um => um.Medalla)
            .Where(um => um.UsuarioId == usuarioId)
            .ToListAsync();
    }

    public async Task<object?> GetPerfilAsync(Guid usuarioId)
    {
        var usuario = await _context.Usuarios.FindAsync(usuarioId);
        if (usuario == null) return null;

        var medallas = await GetMedallasUsuarioAsync(usuarioId);
        var config = await _configService.GetReglasGamificacionAsync();
        var niveles = config.Niveles;

        var xpParaSiguienteNivel = usuario.Nivel < niveles.Count
            ? niveles[usuario.Nivel]
            : niveles[^1];
        var xpNivelActual = (usuario.Nivel - 1 >= 0 && usuario.Nivel - 1 < niveles.Count)
            ? niveles[usuario.Nivel - 1]
            : 0;

        var divisor = Math.Max(1, xpParaSiguienteNivel - xpNivelActual);
        var progreso = (int)Math.Round((double)(usuario.XpActual - xpNivelActual) / divisor * 100);

        return new
        {
            id = usuario.Id,
            nombre = usuario.Nombre,
            email = usuario.Email,
            rol = usuario.Rol,
            nivel = usuario.Nivel,
            xpActual = usuario.XpActual,
            avatar = usuario.Avatar,
            tituloRPG = usuario.TituloRPG,
            medallas = medallas.Select(um => um.Medalla).ToList(),
            xpParaSiguienteNivel,
            progresoNivel = Math.Clamp(progreso, 0, 100)
        };
    }
}

