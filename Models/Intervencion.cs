using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

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

    // No se serializa la entidad Usuario completa (contiene password/hash).
    // Solo se expone el nombre del técnico mediante TecnicoNombre.
    [JsonIgnore]
    [ForeignKey("TecnicoId")]
    public Usuario? Tecnico { get; set; }

    [NotMapped]
    public string? TecnicoNombre => Tecnico?.Nombre;

    [Required]
    [Column("activoId")]
    public Guid ActivoId { get; set; }

    [JsonIgnore]
    [ForeignKey("ActivoId")]
    public Activo? Activo { get; set; }

    [Column("fecha")]
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}
