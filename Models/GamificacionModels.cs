using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace it_ops_backend.Models;

[Table("historial_xp")]
public class HistorialXP
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("usuarioId")]
    public Guid UsuarioId { get; set; }

    [ForeignKey("UsuarioId")]
    public Usuario? Usuario { get; set; }

    [Required]
    [Column("accion")]
    public string Accion { get; set; } = "TICKET_RESUELTO";

    [Column("descripcion")]
    public string? Descripcion { get; set; }

    [Column("xpOtorgado")]
    public int XpOtorgado { get; set; }

    [Column("fecha")]
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}

[Table("medallas")]
public class Medalla
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Column("descripcion")]
    public string? Descripcion { get; set; }

    [Column("icono")]
    public string Icono { get; set; } = "🏆";

    [Column("condicionXP")]
    public int CondicionXP { get; set; } = 0;
}

[Table("usuario_medallas")]
public class UsuarioMedalla
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("usuarioId")]
    public Guid UsuarioId { get; set; }

    [ForeignKey("UsuarioId")]
    public Usuario? Usuario { get; set; }

    [Required]
    [Column("medallaId")]
    public Guid MedallaId { get; set; }

    [ForeignKey("MedallaId")]
    public Medalla? Medalla { get; set; }

    [Column("desbloqueadoEn")]
    public DateTime DesbloqueadoEn { get; set; } = DateTime.UtcNow;
}

