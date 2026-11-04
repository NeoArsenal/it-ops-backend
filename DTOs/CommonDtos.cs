namespace it_ops_backend.DTOs;

public class LoginDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AuthResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string access_token { get; set; } = string.Empty;
    public UsuarioDto User { get; set; } = null!;
    public UsuarioDto Usuario { get; set; } = null!;
}

public class UsuarioDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Rol { get; set; } = "TECNICO";
    public int Nivel { get; set; }
    public int XpActual { get; set; }
    public string Avatar { get; set; } = string.Empty;
    public string? TituloRPG { get; set; }
    public List<string> ModulosAccesibles { get; set; } = new();
}

public class CreateUsuarioDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Password { get; set; }
    public string? Rol { get; set; } = "TECNICO";
    public string? Avatar { get; set; }
    public string? TituloRPG { get; set; }
    public object? ModulosAccesibles { get; set; }
}

public class UpdateUsuarioDto
{
    public string? Nombre { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? Rol { get; set; }
    public string? Avatar { get; set; }
    public string? TituloRPG { get; set; }
    public object? ModulosAccesibles { get; set; }
}

public class UpdatePreferenciasDto
{
    public string? Avatar { get; set; }
    public string? TituloRPG { get; set; }
    public object? Preferencias { get; set; }
}

public class CreateTicketDto
{
    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? Prioridad { get; set; } = "MEDIA";
    public string? Solicitante { get; set; }
    public string? SolicitanteNombre { get; set; }
    public string? SolicitanteContacto { get; set; }
    public string? Departamento { get; set; }
    public string? Sede { get; set; }
    public string? UbicacionEspecifica { get; set; }
    public Guid? UbicacionId { get; set; }
    public string? FotoUrl { get; set; }
    public string? Website { get; set; } // Honeypot
}

public class UpdateTicketDto
{
    public string? Titulo { get; set; }
    public string? Descripcion { get; set; }
    public string? Estado { get; set; }
    public string? Prioridad { get; set; }
    public Guid? AsignadoAId { get; set; }
    public string? Solucion { get; set; }
}

public class CreateActivoDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string? Estado { get; set; } = "DISPONIBLE";
    public string? Marca { get; set; }
    public string? Modelo { get; set; }
    public string? NumeroSerie { get; set; }
    public string? CodigoFactura { get; set; }
    public string? Sede { get; set; }
    public string? Departamento { get; set; }
    public string? Ubicacion { get; set; }
    public Guid? UbicacionId { get; set; }
    public string? Responsable { get; set; }
    public string? ResponsableNombre { get; set; }
    public string? Observaciones { get; set; }
    public Guid? RegistradoPorId { get; set; }
}

/// <summary>
/// DTO para actualizaciones parciales (PATCH) de activos.
/// Todos los campos son opcionales: si no se envían (null) NO se modifican.
/// Evita que acciones parciales (reparar, asignar, taller) borren Código o Tipo.
/// </summary>
public class UpdateActivoDto
{
    public string? Codigo { get; set; }
    public string? Tipo { get; set; }
    public string? Estado { get; set; }
    public string? Marca { get; set; }
    public string? Modelo { get; set; }
    public string? NumeroSerie { get; set; }
    public string? CodigoFactura { get; set; }
    public string? Sede { get; set; }
    public string? Departamento { get; set; }
    public string? Ubicacion { get; set; }
    public Guid? UbicacionId { get; set; }
    public string? Responsable { get; set; }
    public string? ResponsableNombre { get; set; }
    public string? Observaciones { get; set; }
    public Guid? TecnicoId { get; set; }
}

public class CreateIntervencionDto
{
    public string Descripcion { get; set; } = string.Empty;
}

public class CreateUbicacionDto
{
    public string Sede { get; set; } = string.Empty;
    public string Departamento { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
}

public class VerifyAccessDto
{
    public string? Pin { get; set; }
    public string? Token { get; set; }
}

public class SetCatalogoDto
{
    public List<string> Items { get; set; } = new();
}

public class PuntosPorAreaDto
{
    public int TicketBaja { get; set; } = 50;
    public int TicketMedia { get; set; } = 150;
    public int TicketAlta { get; set; } = 350;
    public int TicketCritica { get; set; } = 750;
    public int ActivoReparado { get; set; } = 250;
    public int ActivoRescatado { get; set; } = 600;
    public int RedRestaurada { get; set; } = 300;
    public int GuiaCreada { get; set; } = 200;
    public int AcademiaNivel { get; set; } = 100;
}

public class ReglasGamificacionDto
{
    public PuntosPorAreaDto PuntosPorArea { get; set; } = new();
    public List<int> Niveles { get; set; } = new()
    {
        0, 500, 1200, 2000, 3500, 5000, 7500, 10000, 13000, 17000,
        22000, 28000, 35000, 43000, 52000, 62000, 73000, 85000, 100000, 120000
    };
}

public class CompletarNivelDto
{
    public Guid? UserId { get; set; }
    public Guid NivelId { get; set; }
}

public class CreateDispositivoRedDto
{
    public string Nombre { get; set; } = string.Empty;
    public string? Tipo { get; set; } = "SWITCH";
    public string? Estado { get; set; } = "ONLINE";
    public string? Ubicacion { get; set; }
    public string? IpAdministracion { get; set; }
    public Guid? RegistradoPorId { get; set; }
}

public class RestaurarDispositivoDto
{
    public string? TecnicoId { get; set; }
}

public class RegistrarIpDto
{
    public string Ip { get; set; } = string.Empty;
    public string? Sede { get; set; }
    public string? Area { get; set; }
    public string? Vlan { get; set; }
    public string? Estado { get; set; } = "LIBRE";
    public Guid? DispositivoId { get; set; }
}

public class AsignarIpDto
{
    public Guid DispositivoId { get; set; }
}

public class CreateGuiaDto
{
    public string Titulo { get; set; } = string.Empty;
    public string? UrlPdf { get; set; }
    public string? ContenidoRichText { get; set; }
    public string? Peso { get; set; }
    public Guid? AutorId { get; set; }
}

public class PushSubscriptionKeysDto
{
    public string P256dh { get; set; } = string.Empty;
    public string Auth { get; set; } = string.Empty;
}

public class CreatePushSubscriptionDto
{
    public string Endpoint { get; set; } = string.Empty;
    public PushSubscriptionKeysDto Keys { get; set; } = new();
    public string? UserAgent { get; set; }
}

public class UnsubscribePushDto
{
    public string Endpoint { get; set; } = string.Empty;
}
