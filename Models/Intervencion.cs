using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace it_ops_backend.Models;

[Table("intervenciones")]
public class Intervencion
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("descripcion")]
    public string Descripcion { get; set; } = string.Empty;

    [Column("tecnicoId")]
    public Guid? TecnicoId { get; set; }

    [ForeignKey("TecnicoId")]
    public Usuario? Tecnico { get; set; }

    [Required]
    [Column("activoId")]
    public Guid ActivoId { get; set; }

    [ForeignKey("ActivoId")]
    public Activo? Activo { get; set; }

    [Column("fecha")]
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}

