using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace it_ops_backend.Models;

[Table("cursos")]
public class Curso
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("titulo")]
    public string Titulo { get; set; } = string.Empty;

    [Required]
    [Column("descripcion")]
    public string Descripcion { get; set; } = string.Empty;

    [Required]
    [Column("icono")]
    public string Icono { get; set; } = "Network";

    [Column("orden")]
    public int Orden { get; set; } = 1;

    public ICollection<NivelAcademia> Niveles { get; set; } = new List<NivelAcademia>();
}

[Table("niveles_academia")]
public class NivelAcademia
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("titulo")]
    public string Titulo { get; set; } = string.Empty;

    [Required]
    [Column("descripcion")]
    public string Descripcion { get; set; } = string.Empty;

    [Column("orden")]
    public int Orden { get; set; } = 1;

    [Column("xpRecompensa")]
    public int XpRecompensa { get; set; } = 50;

    [Required]
    [Column("cursoId")]
    public Guid CursoId { get; set; }

    [ForeignKey("CursoId")]
    public Curso? Curso { get; set; }

    public ICollection<Pregunta> Preguntas { get; set; } = new List<Pregunta>();
}

[Table("preguntas_academia")]
public class Pregunta
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("texto")]
    public string Texto { get; set; } = string.Empty;

    [Column("opciones", TypeName = "json")]
    public string OpcionesJson { get; set; } = "[]";

    [Column("respuestaCorrecta")]
    public int RespuestaCorrecta { get; set; }

    [Column("explicacion")]
    public string? Explicacion { get; set; }

    [Required]
    [Column("nivelId")]
    public Guid NivelId { get; set; }

    [ForeignKey("NivelId")]
    public NivelAcademia? Nivel { get; set; }
}

[Table("progreso_academia")]
public class ProgresoUsuario
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
    [Column("nivelId")]
    public Guid NivelId { get; set; }

    [ForeignKey("NivelId")]
    public NivelAcademia? Nivel { get; set; }

    [Column("completado")]
    public bool Completado { get; set; } = false;

    [Column("puntajeMaximo")]
    public int PuntajeMaximo { get; set; } = 0;

    [Column("fechaCompletado")]
    public DateTime FechaCompletado { get; set; } = DateTime.UtcNow;
}

