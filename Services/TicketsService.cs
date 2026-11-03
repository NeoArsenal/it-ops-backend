using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using it_ops_backend.Data;
using it_ops_backend.Hubs;
using it_ops_backend.Models;
using it_ops_backend.Services.Interfaces;

namespace it_ops_backend.Services;

public class TicketsService : ITicketsService
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<NotificacionesHub> _hub;
    private readonly IStorageService _storageService;

    public TicketsService(ApplicationDbContext context, IHubContext<NotificacionesHub> hub, IStorageService storageService)
    {
        _context = context;
        _hub = hub;
        _storageService = storageService;
    }

    public async Task<List<Ticket>> GetAllAsync()
    {
        return await _context.Tickets
            .Include(t => t.AsignadoA)
            .Include(t => t.Ubicacion)
            .OrderByDescending(t => t.CreadoEn)
            .ToListAsync();
    }

    public async Task<List<object>> GetPublicAllAsync()
    {
        var tickets = await _context.Tickets
            .OrderByDescending(t => t.CreadoEn)
            .Select(t => new
            {
                id = t.Id,
                titulo = t.Titulo,
                estado = t.Estado,
                prioridad = t.Prioridad,
                sede = t.Sede,
                departamento = t.Departamento,
                ubicacionEspecifica = t.UbicacionEspecifica,
                creadoEn = t.CreadoEn,
                xpRecompensa = t.XpRecompensa,
                fotoUrl = t.FotoUrl
            })
            .ToListAsync();

        return tickets.Cast<object>().ToList();
    }

    public async Task<List<object>> GetPublicActiveAsync()
    {
        var fifteenMinutesAgo = DateTime.UtcNow.AddMinutes(-15);
        var tickets = await _context.Tickets
            .Include(t => t.AsignadoA)
            .Where(t => t.Estado == "ABIERTO" || t.Estado == "EN_PROGRESO" || (t.Estado == "RESUELTO" && (t.ResueltoEn >= fifteenMinutesAgo || t.ActualizadoEn >= fifteenMinutesAgo)))
            .OrderByDescending(t => t.CreadoEn)
            .Take(50)
            .ToListAsync();

        return tickets.Select(MapPublicTicket).ToList();
    }

    public async Task<List<object>> TrackTicketAsync(string query)
    {
        var raw = (query ?? "").Trim();
        if (raw.Length < 3)
        {
            return new List<object>();
        }

        var cleanCode = Regex.Replace(raw, @"^[#\s]*(TK-?)?", "", RegexOptions.IgnoreCase)
            .Replace(" ", "")
            .ToLowerInvariant();
        var digitsOnly = Regex.Replace(raw, @"\D", "");

        var allTickets = await _context.Tickets
            .Include(t => t.AsignadoA)
            .OrderByDescending(t => t.CreadoEn)
            .ToListAsync();

        var filtered = allTickets.Where(t =>
        {
            var idStr = t.Id.ToString().ToLowerInvariant();
            var matchCode = cleanCode.Length >= 3 && idStr.Contains(cleanCode);
            var matchPhone = digitsOnly.Length >= 3 && !string.IsNullOrEmpty(t.SolicitanteContacto) && t.SolicitanteContacto.Contains(digitsOnly);
            return matchCode || matchPhone;
        }).Take(15).Select(MapPublicTicket).ToList();

        return filtered;
    }

    private static object MapPublicTicket(Ticket t)
    {
        var idStr = t.Id.ToString();
        return new
        {
            id = t.Id,
            ticketCode = $"TK-{idStr[..Math.Min(6, idStr.Length)].ToUpperInvariant()}",
            titulo = t.Titulo,
            descripcion = t.Descripcion,
            estado = t.Estado,
            prioridad = t.Prioridad,
            sede = t.Sede,
            departamento = t.Departamento,
            ubicacionEspecifica = t.UbicacionEspecifica,
            solicitanteNombre = t.SolicitanteNombre,
            creadoEn = t.CreadoEn,
            actualizadoEn = t.ActualizadoEn,
            resueltoEn = t.ResueltoEn,
            solucion = t.Solucion,
            fotoUrl = t.FotoUrl,
            tecnicoAsignado = t.AsignadoA != null ? new
            {
                nombre = t.AsignadoA.Nombre,
                avatar = t.AsignadoA.Avatar,
                rol = t.AsignadoA.Rol
            } : null
        };
    }

    public async Task<Ticket?> GetByIdAsync(Guid id)
    {
        return await _context.Tickets
            .Include(t => t.AsignadoA)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<Ticket> CreateAsync(Ticket ticket, string? solicitanteNombre)
    {
        // Normalización automática de ubicación (3NF)
        if (!ticket.UbicacionId.HasValue && !string.IsNullOrWhiteSpace(ticket.Sede))
        {
            var sedeNorm = ticket.Sede.Trim().ToLower();
            var deptoNorm = (ticket.Departamento ?? "General").Trim().ToLower();
            var matchUbi = await _context.Ubicaciones
                .FirstOrDefaultAsync(u => u.Sede.ToLower() == sedeNorm && u.Departamento.ToLower() == deptoNorm);
            if (matchUbi != null)
            {
                ticket.UbicacionId = matchUbi.Id;
            }
        }
        else if (ticket.UbicacionId.HasValue)
        {
            var ubi = await _context.Ubicaciones.FindAsync(ticket.UbicacionId.Value);
            if (ubi != null)
            {
                ticket.Sede = ubi.Sede;
                ticket.Departamento = ubi.Departamento;
                if (string.IsNullOrWhiteSpace(ticket.UbicacionEspecifica))
                {
                    ticket.UbicacionEspecifica = ubi.Area;
                }
            }
        }

        _context.Tickets.Add(ticket);
        await _context.SaveChangesAsync();

        // Registrar auditoría de creación
        var audit = new TicketHistorial
        {
            TicketId = ticket.Id,
            UsuarioNombre = solicitanteNombre ?? "Portal de Auto-servicio",
            TipoAccion = "CREADO",
            Descripcion = $"Ticket creado con urgencia {ticket.Prioridad} para {ticket.Departamento ?? "General"} en sede {ticket.Sede ?? "Central"}."
        };
        _context.TicketsHistorial.Add(audit);
        await _context.SaveChangesAsync();

        // Emitir por WebSocket (SignalR)
        await _hub.Clients.All.SendAsync("nuevoTicket", ticket);

        return ticket;
    }

    public async Task<Ticket?> UpdateAsync(Guid id, Action<Ticket> applyChanges, Guid? usuarioId, string usuarioNombre)
    {
        var ticket = await _context.Tickets.FindAsync(id);
        if (ticket == null) return null;

        var estadoPrevio = ticket.Estado;
        var prioridadPrevia = ticket.Prioridad;
        var solucionPrevia = ticket.Solucion;
        var asignadoPrevio = ticket.AsignadoAId;

        applyChanges(ticket);
        ticket.ActualizadoEn = DateTime.UtcNow;

        if (ticket.Estado == "RESUELTO" && estadoPrevio != "RESUELTO")
        {
            ticket.ResueltoEn = DateTime.UtcNow;
        }

        // Trazabilidad / Auditoría
        if (ticket.Estado != estadoPrevio)
        {
            _context.TicketsHistorial.Add(new TicketHistorial
            {
                TicketId = ticket.Id,
                UsuarioId = usuarioId,
                UsuarioNombre = usuarioNombre,
                TipoAccion = ticket.Estado == "CERRADO" ? "CERRADO" : "CAMBIO_ESTADO",
                Descripcion = $"Estado actualizado de {estadoPrevio} a {ticket.Estado} por {usuarioNombre}."
            });
        }

        if (ticket.Prioridad != prioridadPrevia)
        {
            _context.TicketsHistorial.Add(new TicketHistorial
            {
                TicketId = ticket.Id,
                UsuarioId = usuarioId,
                UsuarioNombre = usuarioNombre,
                TipoAccion = "CAMBIO_PRIORIDAD",
                Descripcion = $"Urgencia reclasificada de {prioridadPrevia} a {ticket.Prioridad} por {usuarioNombre}."
            });
        }

        if (ticket.AsignadoAId != asignadoPrevio && ticket.AsignadoAId.HasValue)
        {
            _context.TicketsHistorial.Add(new TicketHistorial
            {
                TicketId = ticket.Id,
                UsuarioId = usuarioId,
                UsuarioNombre = usuarioNombre,
                TipoAccion = "ASIGNADO",
                Descripcion = $"Ticket asignado a técnico por {usuarioNombre}."
            });
        }

        if (!string.IsNullOrEmpty(ticket.Solucion) && ticket.Solucion != solucionPrevia)
        {
            _context.TicketsHistorial.Add(new TicketHistorial
            {
                TicketId = ticket.Id,
                UsuarioId = usuarioId,
                UsuarioNombre = usuarioNombre,
                TipoAccion = "SOLUCION_REGISTRADA",
                Descripcion = $"Solución técnica registrada: {ticket.Solucion}"
            });
        }

        await _context.SaveChangesAsync();

        var updated = await GetByIdAsync(id);
        await _hub.Clients.All.SendAsync("ticketActualizado", updated);

        return updated;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var ticket = await _context.Tickets.FindAsync(id);
        if (ticket == null) return false;

        if (!string.IsNullOrEmpty(ticket.FotoUrl))
        {
            try
            {
                await _storageService.DeleteFileAsync(ticket.FotoUrl);
            }
            catch { }
        }

        _context.Tickets.Remove(ticket);
        await _context.SaveChangesAsync();

        await _hub.Clients.All.SendAsync("ticketEliminado", id.ToString());
        return true;
    }

    public async Task<List<TicketHistorial>> GetHistorialAsync(Guid ticketId)
    {
        return await _context.TicketsHistorial
            .Include(h => h.Usuario)
            .Where(h => h.TicketId == ticketId)
            .OrderByDescending(h => h.CreadoEn)
            .ToListAsync();
    }

    public async Task<object> GetStatsAsync()
    {
        var total = await _context.Tickets.CountAsync();
        var abiertos = await _context.Tickets.CountAsync(t => t.Estado == "ABIERTO");
        var enProgreso = await _context.Tickets.CountAsync(t => t.Estado == "EN_PROGRESO");
        var resueltos = await _context.Tickets.CountAsync(t => t.Estado == "RESUELTO");
        return new { total, abiertos, enProgreso, resueltos };
    }

    public async Task<object> GetAnalyticsAsync(string? sedeFiltro)
    {
        var allTickets = await _context.Tickets.ToListAsync();

        // 1. Obtener sedes registradas en Ubicaciones + sedes encontradas en Tickets
        var sedesSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var sedesUbicaciones = await _context.Ubicaciones
            .Where(u => !string.IsNullOrWhiteSpace(u.Sede))
            .Select(u => u.Sede.Trim())
            .Distinct()
            .ToListAsync();

        foreach (var s in sedesUbicaciones)
        {
            sedesSet.Add(s);
        }

        foreach (var t in allTickets)
        {
            if (!string.IsNullOrWhiteSpace(t.Sede))
            {
                sedesSet.Add(t.Sede.Trim());
            }
        }

        if (sedesSet.Count == 0)
        {
            sedesSet.Add("Clínica");
            sedesSet.Add("Tower 1");
        }
        var sedesDisponibles = sedesSet.OrderBy(s => s).ToList();

        // 2. Benchmark comparativo inter-sedes (incluyendo sedes registradas sin tickets aún)
        var porSedeMap = new Dictionary<string, (int total, int resueltos, int abiertos, int criticos, double sumaDias, int conFechas)>(StringComparer.OrdinalIgnoreCase);

        foreach (var s in sedesSet)
        {
            porSedeMap[s] = (0, 0, 0, 0, 0.0, 0);
        }

        foreach (var t in allTickets)
        {
            var s = string.IsNullOrWhiteSpace(t.Sede) ? "Sin Asignar" : t.Sede.Trim();
            if (!porSedeMap.TryGetValue(s, out var current))
            {
                current = (0, 0, 0, 0, 0.0, 0);
            }

            var resuelto = (t.Estado == "RESUELTO" || t.Estado == "CERRADO") ? 1 : 0;
            var abierto = resuelto == 0 ? 1 : 0;
            var critico = (t.Prioridad == "CRITICA" || t.Prioridad == "ALTA") ? 1 : 0;
            var sumaDias = current.sumaDias;
            var conFechas = current.conFechas;

            if (t.ResueltoEn.HasValue)
            {
                sumaDias += (t.ResueltoEn.Value - t.CreadoEn).TotalDays;
                conFechas++;
            }

            porSedeMap[s] = (
                current.total + 1,
                current.resueltos + resuelto,
                current.abiertos + abierto,
                current.criticos + critico,
                sumaDias,
                conFechas
            );
        }

        var porSede = porSedeMap.Select(kv =>
        {
            var item = kv.Value;
            var promSLADias = item.conFechas > 0 ? (item.sumaDias / item.conFechas) : 0.0;
            return new
            {
                sede = kv.Key,
                total = item.total,
                resueltos = item.resueltos,
                abiertos = item.abiertos,
                criticos = item.criticos,
                promedioSLADias = promSLADias,
                promedioSLAHoras = Math.Round(promSLADias * 24, 1),
                porcentajeResolucion = item.total > 0 ? (int)Math.Round((double)item.resueltos / item.total * 100) : 0
            };
        }).OrderByDescending(x => x.total).ThenBy(x => x.sede).ToList();

        var isFiltrado = !string.IsNullOrWhiteSpace(sedeFiltro) && !sedeFiltro.Equals("TODAS", StringComparison.OrdinalIgnoreCase);
        var tickets = isFiltrado
            ? allTickets.Where(t => (t.Sede ?? "").Trim().Equals(sedeFiltro!.Trim(), StringComparison.OrdinalIgnoreCase)).ToList()
            : allTickets;

        var conFechasGlobal = tickets.Where(t => t.ResueltoEn.HasValue).ToList();
        var promedioSLADias = conFechasGlobal.Count > 0
            ? conFechasGlobal.Average(t => (t.ResueltoEn!.Value - t.CreadoEn).TotalDays)
            : 0.0;

        var heatmapDept = tickets
            .GroupBy(t => string.IsNullOrWhiteSpace(t.Departamento) ? "General" : t.Departamento)
            .Select(g =>
            {
                var baja = g.Count(t => t.Prioridad == "BAJA");
                var media = g.Count(t => t.Prioridad == "MEDIA");
                var alta = g.Count(t => t.Prioridad == "ALTA");
                var critica = g.Count(t => t.Prioridad == "CRITICA");
                return new Dictionary<string, object>
                {
                    ["departamento"] = g.Key,
                    ["BAJA"] = baja,
                    ["baja"] = baja,
                    ["MEDIA"] = media,
                    ["media"] = media,
                    ["ALTA"] = alta,
                    ["alta"] = alta,
                    ["CRITICA"] = critica,
                    ["critica"] = critica,
                    ["total"] = baja + media + alta + critica
                };
            })
            .OrderByDescending(x => Convert.ToInt32(x["total"]))
            .ToList();

        return new
        {
            heatmapDept,
            promedioSLADias,
            totalTickets = tickets.Count,
            resueltosCount = tickets.Count(t => t.Estado == "RESUELTO" || t.Estado == "CERRADO"),
            sedesDisponibles,
            porSede,
            sedeActiva = isFiltrado ? sedeFiltro : "TODAS",
            totalTicketsGlobal = allTickets.Count
        };
    }
}
