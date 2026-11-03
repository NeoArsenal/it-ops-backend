using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using it_ops_backend.Data;
using it_ops_backend.DTOs;
using it_ops_backend.Hubs;
using it_ops_backend.Models;
using it_ops_backend.Services.Interfaces;

namespace it_ops_backend.Services;

public class RedService : IRedService
{
    private readonly ApplicationDbContext _context;
    private readonly IGamificacionService _gamificacionService;
    private readonly IHubContext<NotificacionesHub> _hub;
    private readonly IConfiguracionService _configService;

    public RedService(
        ApplicationDbContext context,
        IGamificacionService gamificacionService,
        IHubContext<NotificacionesHub> hub,
        IConfiguracionService configService)
    {
        _context = context;
        _gamificacionService = gamificacionService;
        _hub = hub;
        _configService = configService;
    }

    public async Task<List<DispositivoRed>> GetDispositivosAsync()
    {
        return await _context.DispositivosRed
            .Include(d => d.DireccionesIP)
            .Include(d => d.RegistradoPor)
            .ToListAsync();
    }

    public async Task<DispositivoRed?> GetDispositivoAsync(Guid id)
    {
        return await _context.DispositivosRed
            .Include(d => d.DireccionesIP)
            .Include(d => d.RegistradoPor)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<DispositivoRed> CreateDispositivoAsync(CreateDispositivoRedDto dto)
    {
        var disp = new DispositivoRed
        {
            Nombre = dto.Nombre,
            Tipo = dto.Tipo ?? "SWITCH",
            Estado = dto.Estado ?? "ONLINE",
            Ubicacion = dto.Ubicacion,
            IpAdministracion = dto.IpAdministracion,
            RegistradoPorId = dto.RegistradoPorId,
            UltimoPing = DateTime.UtcNow,
            CreadoEn = DateTime.UtcNow,
            ActualizadoEn = DateTime.UtcNow
        };

        _context.DispositivosRed.Add(disp);
        await _context.SaveChangesAsync();
        return disp;
    }

    public async Task<DispositivoRed?> UpdateDispositivoAsync(Guid id, CreateDispositivoRedDto dto)
    {
        var disp = await _context.DispositivosRed.FindAsync(id);
        if (disp == null) return null;

        if (!string.IsNullOrWhiteSpace(dto.Nombre)) disp.Nombre = dto.Nombre;
        if (!string.IsNullOrWhiteSpace(dto.Tipo)) disp.Tipo = dto.Tipo;
        if (!string.IsNullOrWhiteSpace(dto.Estado)) disp.Estado = dto.Estado;
        if (dto.Ubicacion != null) disp.Ubicacion = dto.Ubicacion;
        if (dto.IpAdministracion != null) disp.IpAdministracion = dto.IpAdministracion;
        disp.ActualizadoEn = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return disp;
    }

    public async Task<DispositivoRed?> SimularCaidaAsync()
    {
        var online = await _context.DispositivosRed
            .Where(d => d.Estado == "ONLINE")
            .ToListAsync();

        if (online.Count == 0) return null;

        var victima = online[Random.Shared.Next(online.Count)];
        victima.Estado = "OFFLINE";
        victima.ActualizadoEn = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _hub.Clients.All.SendAsync("alertaRed", victima);
        return victima;
    }

    public async Task<object?> RestaurarDispositivoAsync(Guid id, Guid? tecnicoId)
    {
        var disp = await _context.DispositivosRed.FindAsync(id);
        if (disp == null) return null;

        disp.Estado = "ONLINE";
        disp.UltimoPing = DateTime.UtcNow;
        disp.ActualizadoEn = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        object? xpResult = null;
        if (tecnicoId.HasValue)
        {
            var reglas = await _configService.GetReglasGamificacionAsync();
            xpResult = await _gamificacionService.OtorgarXPAsync(
                tecnicoId.Value,
                reglas.PuntosPorArea.RedRestaurada,
                "RED_RESTAURADA",
                $"Restauró {disp.Nombre} ({disp.Tipo})"
            );
        }

        await _hub.Clients.All.SendAsync("redRestaurada", disp);
        return new { dispositivo = disp, xpResult };
    }

    public async Task<List<DireccionIP>> GetIPsAsync()
    {
        return await _context.DireccionesIP
            .Include(ip => ip.Dispositivo)
            .OrderBy(ip => ip.Ip)
            .ToListAsync();
    }

    public async Task<DireccionIP> RegistrarIPAsync(RegistrarIpDto dto)
    {
        var ip = new DireccionIP
        {
            Ip = dto.Ip,
            Sede = dto.Sede,
            Area = dto.Area,
            Vlan = dto.Vlan,
            Estado = dto.Estado ?? "LIBRE",
            DispositivoId = dto.DispositivoId
        };
        _context.DireccionesIP.Add(ip);
        await _context.SaveChangesAsync();
        return ip;
    }

    public async Task<DireccionIP?> AsignarIPAsync(string ipStr, Guid dispositivoId)
    {
        var ip = await _context.DireccionesIP.FindAsync(ipStr);
        if (ip == null) return null;

        ip.Estado = "EN_USO";
        ip.DispositivoId = dispositivoId;
        await _context.SaveChangesAsync();
        return ip;
    }

    public async Task<DireccionIP?> LiberarIPAsync(string ipStr)
    {
        var ip = await _context.DireccionesIP.FindAsync(ipStr);
        if (ip == null) return null;

        ip.Estado = "LIBRE";
        ip.DispositivoId = null;
        await _context.SaveChangesAsync();
        return ip;
    }
}

