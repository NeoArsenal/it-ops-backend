using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WebPush;
using it_ops_backend.Data;
using it_ops_backend.DTOs;
using it_ops_backend.Models;
using it_ops_backend.Services.Interfaces;
using DbPushSub = it_ops_backend.Models.PushSubscription;

namespace it_ops_backend.Services;

public class PushNotificationService : IPushNotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _config;
    private readonly ILogger<PushNotificationService> _logger;
    private readonly VapidDetails? _vapidDetails;
    private readonly bool _vapidConfigured;

    private const string DefaultVapidPublicKey = "BEBWWjLDPLE1DJaY7MHN6Y63viz5QCP7YmKa0j7JLOjQcuhuNGP9r_y2Bm1RyxfZ140bzIIgbBWyAjCp9eo95bs";
    private const string DefaultVapidPrivateKey = "SQdZm13gJ8h_R4X6r-g3c6mOdLqaL1aMCZqEusU7VKE";
    private const string DefaultVapidSubject = "mailto:soporte@clinicalimatambo.com";

    public PushNotificationService(ApplicationDbContext context, IConfiguration config, ILogger<PushNotificationService> logger)
    {
        _context = context;
        _config = config;
        _logger = logger;

        var rawPublic = Environment.GetEnvironmentVariable("VAPID_PUBLIC_KEY") 
                        ?? _config["Vapid:PublicKey"] 
                        ?? DefaultVapidPublicKey;
        var rawPrivate = Environment.GetEnvironmentVariable("VAPID_PRIVATE_KEY") 
                         ?? _config["Vapid:PrivateKey"] 
                         ?? DefaultVapidPrivateKey;
        var rawSubject = Environment.GetEnvironmentVariable("VAPID_SUBJECT") 
                         ?? _config["Vapid:Subject"] 
                         ?? DefaultVapidSubject;

        var publicKey = SanitizeKey(rawPublic);
        var privateKey = SanitizeKey(rawPrivate);
        var subject = string.IsNullOrWhiteSpace(rawSubject) ? DefaultVapidSubject : rawSubject.Trim();

        if (!string.IsNullOrWhiteSpace(publicKey) && !string.IsNullOrWhiteSpace(privateKey))
        {
            try
            {
                _vapidDetails = new VapidDetails(subject, publicKey, privateKey);
                _vapidConfigured = true;
                _logger.LogInformation("Web Push VAPID inicializado correctamente en .NET 8.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al configurar VapidDetails");
            }
        }
    }

    private static string SanitizeKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return string.Empty;
        return key.Replace("\"", "").Replace("'", "").Replace("\r", "").Replace("\n", "").Trim();
    }

    public string? GetPublicKey()
    {
        var raw = Environment.GetEnvironmentVariable("VAPID_PUBLIC_KEY") 
                  ?? _config["Vapid:PublicKey"] 
                  ?? DefaultVapidPublicKey;
        return SanitizeKey(raw);
    }

    public async Task<DbPushSub> SaveSubscriptionAsync(string userId, CreatePushSubscriptionDto dto)
    {
        var existing = await _context.PushSubscriptions.FirstOrDefaultAsync(s => s.Endpoint == dto.Endpoint);
        if (existing != null)
        {
            existing.UserId = userId;
            existing.P256dh = dto.Keys.P256dh;
            existing.Auth = dto.Keys.Auth;
            if (!string.IsNullOrEmpty(dto.UserAgent)) existing.UserAgent = dto.UserAgent;
            existing.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return existing;
        }

        var sub = new DbPushSub
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Endpoint = dto.Endpoint,
            P256dh = dto.Keys.P256dh,
            Auth = dto.Keys.Auth,
            UserAgent = dto.UserAgent ?? string.Empty,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.PushSubscriptions.Add(sub);
        await _context.SaveChangesAsync();
        return sub;
    }

    public async Task RemoveSubscriptionAsync(string endpoint)
    {
        var sub = await _context.PushSubscriptions.FirstOrDefaultAsync(s => s.Endpoint == endpoint);
        if (sub != null)
        {
            _context.PushSubscriptions.Remove(sub);
            await _context.SaveChangesAsync();
        }
    }

    public async Task RemoveUserSubscriptionsAsync(string userId)
    {
        var subs = await _context.PushSubscriptions.Where(s => s.UserId == userId).ToListAsync();
        if (subs.Any())
        {
            _context.PushSubscriptions.RemoveRange(subs);
            await _context.SaveChangesAsync();
        }
    }

    public async Task SendToUserAsync(string userId, PushNotificationPayload payload)
    {
        var subs = await _context.PushSubscriptions.Where(s => s.UserId == userId).ToListAsync();
        if (!subs.Any()) return;

        foreach (var sub in subs)
        {
            await SendToSubscriptionAsync(sub, payload);
        }
    }

    public async Task SendToRolesAsync(List<string> roles, PushNotificationPayload payload)
    {
        var upperRoles = roles.Select(r => r.ToUpperInvariant()).ToList();
        var userIds = await _context.Usuarios
            .Where(u => upperRoles.Contains(u.Rol.ToUpper()))
            .Select(u => u.Id.ToString())
            .ToListAsync();

        if (!userIds.Any()) return;

        var subs = await _context.PushSubscriptions
            .Where(s => userIds.Contains(s.UserId))
            .ToListAsync();

        if (!subs.Any()) return;

        _logger.LogInformation("Enviando Web Push a {Count} dispositivos suscritos.", subs.Count);
        foreach (var sub in subs)
        {
            await SendToSubscriptionAsync(sub, payload);
        }
    }

    public async Task SendNewTicketAlertAsync(Ticket ticket)
    {
        var idStr = ticket.Id.ToString();
        var ticketNum = $"TK-{idStr[..Math.Min(6, idStr.Length)].ToUpperInvariant()}";

        var title = $"🎫 Nuevo Ticket #{ticketNum}";
        var sede = !string.IsNullOrEmpty(ticket.Sede) ? $"Sede: {ticket.Sede}" : "";
        var depto = !string.IsNullOrEmpty(ticket.Departamento) ? $"Depto: {ticket.Departamento}" : "";
        var ubicacion = string.Join(" | ", new[] { sede, depto }.Where(s => !string.IsNullOrEmpty(s)));

        var body = ticket.Titulo + (!string.IsNullOrEmpty(ubicacion) ? $"\n📍 {ubicacion}" : "");

        await SendToRolesAsync(new List<string> { "ADMIN", "TECNICO" }, new PushNotificationPayload
        {
            Title = title,
            Body = body,
            Icon = "/icon-192.png",
            Badge = "/icon-192.png",
            Url = $"/?view=tickets&ticketId={ticket.Id}",
            Tag = $"ticket-{ticket.Id}",
            Data = new { ticketId = ticket.Id }
        });
    }

    private async Task<bool> SendToSubscriptionAsync(DbPushSub sub, PushNotificationPayload payload)
    {
        if (!_vapidConfigured || _vapidDetails == null)
        {
            _logger.LogWarning("Intento de envío Push sin VAPID configurado.");
            return false;
        }

        var webPushSub = new WebPush.PushSubscription(sub.Endpoint, sub.P256dh, sub.Auth);
        var jsonPayload = JsonSerializer.Serialize(new
        {
            title = payload.Title,
            body = payload.Body,
            icon = payload.Icon ?? "/icon-192.png",
            badge = payload.Badge ?? "/icon-192.png",
            tag = payload.Tag ?? "limatambo-helpdesk",
            data = new
            {
                url = payload.Url ?? "/",
                custom = payload.Data
            }
        });

        try
        {
            var client = new WebPushClient();
            await client.SendNotificationAsync(webPushSub, jsonPayload, _vapidDetails);
            return true;
        }
        catch (WebPushException ex)
        {
            if (ex.StatusCode == System.Net.HttpStatusCode.Gone || ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Suscripción Push expirada ({StatusCode}). Eliminando endpoint.", ex.StatusCode);
                _context.PushSubscriptions.Remove(sub);
                await _context.SaveChangesAsync();
            }
            else
            {
                _logger.LogError(ex, "Error enviando Web Push a endpoint.");
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado enviando Web Push.");
            return false;
        }
    }
}
