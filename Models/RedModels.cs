using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace it_ops_backend.Models;

[Table("dispositivos_red")]
public class DispositivoRed
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Column("tipo")]
    public string Tipo { get; set; } = "SWITCH";

    [Column("estado")]
    public string Estado { get; set; } = "ONLINE";

    [Column("ubicacion")]
    public string? Ubicacion { get; set; }

    [Column("ipAdministracion")]
    public string? IpAdministracion { get; set; }

    [Column("ultimoPing")]
    public DateTime? UltimoPing { get; set; }

    [Column("registradoPorId")]
    public Guid? RegistradoPorId { get; set; }

    [ForeignKey("RegistradoPorId")]
    public Usuario? RegistradoPor { get; set; }

    [Column("creadoEn")]
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    [Column("actualizadoEn")]
    public DateTime ActualizadoEn { get; set; } = DateTime.UtcNow;

    public ICollection<DireccionIP> DireccionesIP { get; set; } = new List<DireccionIP>();
}

[Table("direcciones_ip")]
public class DireccionIP
{
    [Key]
    [Column("ip")]
    public string Ip { get; set; } = string.Empty;

    [Column("sede")]
    public string? Sede { get; set; }

    [Column("area")]
    public string? Area { get; set; }

    [Column("vlan")]
    public string? Vlan { get; set; }

    [Column("estado")]
    public string Estado { get; set; } = "LIBRE";

    [Column("dispositivoId")]
    public Guid? DispositivoId { get; set; }

    [ForeignKey("DispositivoId")]
    public DispositivoRed? Dispositivo { get; set; }

    [Column("ubicacion_id")]
    public Guid? UbicacionId { get; set; }

    [ForeignKey("UbicacionId")]
    public Ubicacion? Ubicacion { get; set; }
}

[Table("push_subscriptions")]
public class PushSubscription
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("userId")]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [Column("endpoint")]
    public string Endpoint { get; set; } = string.Empty;

    [Required]
    [Column("p256dh")]
    public string P256dh { get; set; } = string.Empty;

    [Required]
    [Column("auth")]
    public string Auth { get; set; } = string.Empty;

    [Column("userAgent")]
    public string? UserAgent { get; set; }

    [Column("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

