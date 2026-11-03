using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace it_ops_backend.Models;

[Table("tickets_historial")]
public class TicketHistorial
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("ticketId")]
    public Guid TicketId { get; set; }

    [ForeignKey("TicketId")]
    public Ticket? Ticket { get; set; }

    [Column("usuarioId")]
    public Guid? UsuarioId { get; set; }

    [ForeignKey("UsuarioId")]
    public Usuario? Usuario { get; set; }

    [Column("usuarioNombre")]
    public string? UsuarioNombre { get; set; }

    [Column("tipoAccion")]
    public string TipoAccion { get; set; } = "CREADO";

    [Required]
    [Column("descripcion")]
    public string Descripcion { get; set; } = string.Empty;

    [Column("detalles")]
    public string? Detalles { get; set; }

    [Column("creadoEn")]
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}

