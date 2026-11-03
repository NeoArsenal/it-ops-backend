using Microsoft.EntityFrameworkCore;
using it_ops_backend.Models;

namespace it_ops_backend.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<Ticket> Tickets { get; set; }
    public DbSet<TicketHistorial> TicketsHistorial { get; set; }
    public DbSet<Activo> Activos { get; set; }
    public DbSet<Intervencion> Intervenciones { get; set; }
    public DbSet<Ubicacion> Ubicaciones { get; set; }
    public DbSet<Configuracion> Configuraciones { get; set; }
    public DbSet<Guia> Guias { get; set; }
    public DbSet<HistorialXP> HistorialesXP { get; set; }
    public DbSet<Medalla> Medallas { get; set; }
    public DbSet<UsuarioMedalla> UsuariosMedallas { get; set; }
    public DbSet<Curso> Cursos { get; set; }
    public DbSet<NivelAcademia> NivelesAcademia { get; set; }
    public DbSet<Pregunta> PreguntasAcademia { get; set; }
    public DbSet<ProgresoUsuario> ProgresosAcademia { get; set; }
    public DbSet<DispositivoRed> DispositivosRed { get; set; }
    public DbSet<DireccionIP> DireccionesIP { get; set; }
    public DbSet<PushSubscription> PushSubscriptions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Usuario>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Activo>()
            .HasIndex(a => a.Codigo)
            .IsUnique();
    }
}
