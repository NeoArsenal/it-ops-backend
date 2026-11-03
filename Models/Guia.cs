using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace it_ops_backend.Models;

[Table("guias")]
public class Guia
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("titulo")]
    public string Titulo { get; set; } = string.Empty;

    [Column("urlPdf")]
    public string? UrlPdf { get; set; }

    [Column("contenidoRichText")]
    public string? ContenidoRichText { get; set; }

    [Column("peso")]
    public string? Peso { get; set; }

    [Column("autorId")]
    public Guid? AutorId { get; set; }

    [ForeignKey("AutorId")]
    public Usuario? Autor { get; set; }

    [Column("fechaSubida")]
    public DateTime FechaSubida { get; set; } = DateTime.UtcNow;
}

