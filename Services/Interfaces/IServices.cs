using Microsoft.AspNetCore.Http;
using it_ops_backend.DTOs;
using it_ops_backend.Models;

namespace it_ops_backend.Services.Interfaces;

public interface IAuthService
{
    Task<(string token, Usuario user)?> LoginAsync(string email, string password);
}

public interface IStorageService
{
    Task<string> UploadFileAsync(IFormFile file, string folder = "evidencias");
    Task DeleteFileAsync(string publicUrl);
}

public interface ITicketsService
{
    Task<List<Ticket>> GetAllAsync();
    Task<List<object>> GetPublicAllAsync();
    Task<List<object>> GetPublicActiveAsync();
    Task<List<object>> TrackTicketAsync(string query);
    Task<Ticket?> GetByIdAsync(Guid id);
    Task<Ticket> CreateAsync(Ticket ticket, string? solicitanteNombre);
    Task<Ticket?> UpdateAsync(Guid id, Action<Ticket> applyChanges, Guid? usuarioId, string usuarioNombre);
    Task<bool> DeleteAsync(Guid id);
    Task<List<TicketHistorial>> GetHistorialAsync(Guid ticketId);
    Task<object> GetStatsAsync();
    Task<object> GetAnalyticsAsync(string? sedeFiltro);
}

public interface IActivosService
{
    Task<List<Activo>> GetAllAsync();
    Task<Activo?> GetByIdAsync(Guid id);
    Task<Activo?> GetByCodigoAsync(string codigo);
    Task<Activo> CreateAsync(Activo activo, Guid? tecnicoId = null);
    Task<Activo?> UpdateAsync(Guid id, Action<Activo> applyChanges, Guid? tecnicoId = null);
    Task<bool> DeleteAsync(Guid id);
    Task<Intervencion> AddIntervencionAsync(Guid activoId, string descripcion, Guid? tecnicoId);
}

public interface IUbicacionesService
{
    Task<List<Ubicacion>> GetAllAsync();
    Task<List<string>> GetSedesAsync();
    Task<List<string>> GetDepartamentosPorSedeAsync(string sede);
    Task<List<string>> GetAreasAsync(string sede, string departamento);
    Task<Ubicacion> CreateAsync(Ubicacion ubicacion, Guid? usuarioId);
    Task<bool> DeleteAsync(Guid id);
}

public interface IConfiguracionService
{
    Task<string> GetValueAsync(string clave, string defaultValue = "");
    Task SetValueAsync(string clave, string valor, Guid? usuarioId);
    Task<string> GetPortalTokenAsync();
    Task<string> RegeneratePortalTokenAsync(Guid? usuarioId);
    Task<bool> VerifyAccessAsync(string? pin, string? token);
    Task<bool> VerifyPinAsync(string pin);
    Task<object> GetCatalogosAsync();
    Task<List<string>> SetCatalogoAsync(string tipo, List<string> items, Guid? usuarioId);
    Task<ReglasGamificacionDto> GetReglasGamificacionAsync();
    Task<ReglasGamificacionDto> SetReglasGamificacionAsync(ReglasGamificacionDto reglas, Guid? usuarioId);
}

public interface IUsuariosService
{
    Task<List<Usuario>> GetAllAsync();
    Task<Usuario?> GetByIdAsync(Guid id);
    Task<Usuario> CreateAsync(CreateUsuarioDto dto);
    Task<Usuario?> UpdateAsync(Guid id, UpdateUsuarioDto dto);
    Task<bool> DeleteAsync(Guid id, Guid? currentUserId);
    Task<Usuario?> UpdatePreferenciasAsync(Guid id, UpdatePreferenciasDto dto);
}

public interface IGamificacionService
{
    Task<object> OtorgarXPAsync(Guid usuarioId, int xp, string accion, string? descripcion = null);
    Task<List<object>> GetLeaderboardAsync(int limit = 10);
    Task<List<HistorialXP>> GetHistorialAsync(Guid usuarioId);
    Task<List<UsuarioMedalla>> GetMedallasUsuarioAsync(Guid usuarioId);
    Task<object?> GetPerfilAsync(Guid usuarioId);
}

public interface IAcademiaService
{
    Task<List<object>> GetCursosAsync(Guid userId);
    Task<object?> GetNivelConPreguntasAsync(Guid nivelId);
    Task<object> CompletarNivelAsync(Guid userId, Guid nivelId);
}

public interface IRedService
{
    Task<List<DispositivoRed>> GetDispositivosAsync();
    Task<DispositivoRed?> GetDispositivoAsync(Guid id);
    Task<DispositivoRed> CreateDispositivoAsync(CreateDispositivoRedDto dto);
    Task<DispositivoRed?> UpdateDispositivoAsync(Guid id, CreateDispositivoRedDto dto);
    Task<DispositivoRed?> SimularCaidaAsync();
    Task<object?> RestaurarDispositivoAsync(Guid id, Guid? tecnicoId);
    Task<List<DireccionIP>> GetIPsAsync();
    Task<DireccionIP> RegistrarIPAsync(RegistrarIpDto dto);
    Task<DireccionIP?> AsignarIPAsync(string ip, Guid dispositivoId);
    Task<DireccionIP?> LiberarIPAsync(string ip);
}

public interface IGuiasService
{
    Task<List<Guia>> GetAllAsync();
    Task<List<Guia>> SearchAsync(string query);
    Task<Guia?> GetByIdAsync(Guid id);
    Task<Guia> CreateAsync(CreateGuiaDto dto);
    Task<bool> DeleteAsync(Guid id);
}

public class PushNotificationPayload
{
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? Icon { get; set; } = "/icon-192.png";
    public string? Badge { get; set; } = "/icon-192.png";
    public string? Url { get; set; } = "/";
    public string? Tag { get; set; } = "limatambo-helpdesk";
    public object? Data { get; set; }
}

public interface IPushNotificationService
{
    string? GetPublicKey();
    Task<PushSubscription> SaveSubscriptionAsync(string userId, CreatePushSubscriptionDto dto);
    Task RemoveSubscriptionAsync(string endpoint);
    Task RemoveUserSubscriptionsAsync(string userId);
    Task SendToUserAsync(string userId, PushNotificationPayload payload);
    Task SendToRolesAsync(List<string> roles, PushNotificationPayload payload);
    Task SendNewTicketAlertAsync(Ticket ticket);
}
