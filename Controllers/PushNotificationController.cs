using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using it_ops_backend.DTOs;
using it_ops_backend.Services.Interfaces;

namespace it_ops_backend.Controllers;

[ApiController]
[Route("api/notifications/push")]
[Route("notifications/push")]
public class PushNotificationController : ControllerBase
{
    private readonly IPushNotificationService _pushService;

    public PushNotificationController(IPushNotificationService pushService)
    {
        _pushService = pushService;
    }

    [HttpGet("public-key")]
    public IActionResult GetPublicKey()
    {
        var key = _pushService.GetPublicKey();
        if (string.IsNullOrEmpty(key))
        {
            return BadRequest(new { message = "Las claves VAPID no están configuradas en el servidor" });
        }
        return Ok(new { publicKey = key });
    }

    [Authorize]
    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] CreatePushSubscriptionDto dto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                     ?? User.FindFirst("id")?.Value 
                     ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Usuario no identificado en el token" });
        }

        var sub = await _pushService.SaveSubscriptionAsync(userId, dto);
        return Ok(new { success = true, id = sub.Id });
    }

    [Authorize]
    [HttpPost("unsubscribe")]
    public async Task<IActionResult> Unsubscribe([FromBody] UnsubscribePushDto dto)
    {
        await _pushService.RemoveSubscriptionAsync(dto.Endpoint);
        return Ok(new { success = true });
    }

    [Authorize]
    [HttpPost("test")]
    public async Task<IActionResult> TestNotification()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                     ?? User.FindFirst("id")?.Value 
                     ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Usuario no identificado en el token" });
        }

        await _pushService.SendToUserAsync(userId, new PushNotificationPayload
        {
            Title = "🔔 Notificación de Prueba",
            Body = "¡Excelente! Tu dispositivo está listo para recibir alertas en tiempo real de Helpdesk Limatambo.",
            Icon = "/icon-192.png",
            Badge = "/icon-192.png",
            Tag = "test-notification",
            Url = "/?view=tickets"
        });

        return Ok(new { success = true, message = "Alerta de prueba enviada" });
    }
}

