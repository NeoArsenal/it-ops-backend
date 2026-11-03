using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace it_ops_backend.Models;

[Table("usuarios")]
public class Usuario
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Column("password")]
    public string Password { get; set; } = string.Empty;

    [Column("rol")]
    public string Rol { get; set; } = "TECNICO";

    [Column("nivel")]
    public int Nivel { get; set; } = 1;

    [Column("xpActual")]
    public int XpActual { get; set; } = 0;

    [Column("avatar")]
    public string Avatar { get; set; } = string.Empty;

    [Column("tituloRPG")]
    public string? TituloRPG { get; set; }

    [Column("preferencias", TypeName = "jsonb")]
    public string? Preferencias { get; set; }

    [JsonIgnore]
    [Column("modulosAccesibles")]
    public string? ModulosAccesiblesRaw { get; set; }

    [NotMapped]
    [JsonPropertyName("modulosAccesibles")]
    public List<string> ModulosAccesibles
    {
        get
        {
            if (string.IsNullOrWhiteSpace(ModulosAccesiblesRaw))
                return new List<string>();
            try
            {
                if (ModulosAccesiblesRaw.TrimStart().StartsWith("["))
                {
                    var parsed = JsonSerializer.Deserialize<List<string>>(ModulosAccesiblesRaw);
                    if (parsed != null) return parsed;
                }
            }
            catch { }
            return ModulosAccesiblesRaw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        }
        set
        {
            ModulosAccesiblesRaw = value != null && value.Count > 0 ? string.Join(",", value) : null;
        }
    }
}
