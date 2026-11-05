using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace it_ops_backend.Models;

[Table("tickets")]
public class Ticket
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("titulo")]
    public string Titulo { get; set; } = string.Empty;

    [Column("descripcion")]
    public string? Descripcion { get; set; }

    [Column("estado")]
    public string Estado { get; set; } = "ABIERTO";

    [Column("prioridad")]
    public string Prioridad { get; set; } = "MEDIA";

    [Column("categoria")]
    public string Categoria { get; set; } = "GENERAL";

    [Column("asignadoAId")]
    public Guid? AsignadoAId { get; set; }

    [ForeignKey("AsignadoAId")]
    public Usuario? AsignadoA { get; set; }

    [Column("solicitante")]
    public string? Solicitante { get; set; }

    [Column("departamento")]
    public string? Departamento { get; set; } = "General";

    [Column("sede")]
    public string? Sede { get; set; }

    [Column("ubicacionEspecifica")]
    public string? UbicacionEspecifica { get; set; }

    [Column("ubicacion_id")]
    public Guid? UbicacionId { get; set; }

    [ForeignKey("UbicacionId")]
    public Ubicacion? Ubicacion { get; set; }

    [Column("solicitanteNombre")]
    public string? SolicitanteNombre { get; set; }

    [Column("solicitanteContacto")]
    public string? SolicitanteContacto { get; set; }

    [Column("creadoEn")]
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    [Column("resueltoEn")]
    public DateTime? ResueltoEn { get; set; }

    [Column("solucion")]
    public string? Solucion { get; set; }

    [Column("fotoUrl")]
    public string? FotoUrl { get; set; }

    [Column("actualizadoEn")]
    public DateTime ActualizadoEn { get; set; } = DateTime.UtcNow;
}

