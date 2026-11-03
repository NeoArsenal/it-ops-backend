using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace it_ops_backend.Models;

[Table("ubicaciones")]
public class Ubicacion
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("sede")]
    public string Sede { get; set; } = string.Empty;

    [Required]
    [Column("departamento")]
    public string Departamento { get; set; } = string.Empty;

    [Required]
    [Column("area")]
    public string Area { get; set; } = string.Empty;

    [Column("creadoPorId")]
    public Guid? CreadoPorId { get; set; }

    [ForeignKey("CreadoPorId")]
    public Usuario? CreadoPor { get; set; }

    [Column("creadoEn")]
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    [Column("actualizadoEn")]
    public DateTime ActualizadoEn { get; set; } = DateTime.UtcNow;
}

