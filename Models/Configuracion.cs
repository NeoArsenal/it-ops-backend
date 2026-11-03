using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace it_ops_backend.Models;

[Table("configuracion")]
public class Configuracion
{
    [Key]
    [Column("clave")]
    public string Clave { get; set; } = string.Empty;

    [Required]
    [Column("valor")]
    public string Valor { get; set; } = string.Empty;

    [Column("actualizadoPorId")]
    public Guid? ActualizadoPorId { get; set; }

    [ForeignKey("ActualizadoPorId")]
    public Usuario? ActualizadoPor { get; set; }

    [Column("actualizadoEn")]
    public DateTime ActualizadoEn { get; set; } = DateTime.UtcNow;
}

