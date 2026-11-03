using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace it_ops_backend.Models;

[Table("activos")]
public class Activo
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("codigo")]
    public string Codigo { get; set; } = string.Empty;

    [Required]
    [Column("tipo")]
    public string Tipo { get; set; } = string.Empty;

    [Column("estado")]
    public string Estado { get; set; } = "DISPONIBLE";

    [Column("marca")]
    public string? Marca { get; set; }

    [Column("modelo")]
    public string? Modelo { get; set; }

    [Column("numeroSerie")]
    public string? NumeroSerie { get; set; }

    [Column("codigoFactura")]
    public string? CodigoFactura { get; set; }

    [Column("sede")]
    public string? Sede { get; set; }

    [Column("departamento")]
    public string? Departamento { get; set; }

    [Column("ubicacion")]
    public string? UbicacionTexto { get; set; }

    [Column("ubicacion_id")]
    public Guid? UbicacionId { get; set; }

    [ForeignKey("UbicacionId")]
    public Ubicacion? UbicacionRelacion { get; set; }

    [NotMapped]
    public string? Ubicacion
    {
        get => UbicacionRelacion != null ? $"{UbicacionRelacion.Sede} - {UbicacionRelacion.Departamento} ({UbicacionRelacion.Area})" : UbicacionTexto;
        set => UbicacionTexto = value;
    }

    [Column("responsable")]
    public string? Responsable { get; set; }

    [NotMapped]
    public string? ResponsableNombre
    {
        get => Responsable;
        set => Responsable = value;
    }

    [Column("observaciones")]
    public string? Observaciones { get; set; }

    [Column("registradoPorId")]
    public Guid? RegistradoPorId { get; set; }

    [ForeignKey("RegistradoPorId")]
    public Usuario? RegistradoPor { get; set; }

    [Column("fechaRegistro")]
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public DateTime CreadoEn => FechaRegistro;

    [Column("fechaActualizacion")]
    public DateTime FechaActualizacion { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public DateTime ActualizadoEn => FechaActualizacion;

    public ICollection<Intervencion> Intervenciones { get; set; } = new List<Intervencion>();
}
