using System.Text;
using Microsoft.EntityFrameworkCore;
using it_ops_backend.Data;
using it_ops_backend.Models;
using it_ops_backend.Services.Interfaces;

namespace it_ops_backend.Services;

public class ActivosService : IActivosService
{
    private readonly ApplicationDbContext _context;

    public ActivosService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Activo>> GetAllAsync()
    {
        return await _context.Activos
            .Include(a => a.Intervenciones)
                .ThenInclude(i => i.Tecnico)
            .Include(a => a.UbicacionRelacion)
            .AsSplitQuery()
            .OrderByDescending(a => a.FechaRegistro)
            .ToListAsync();
    }

    public async Task<Activo?> GetByIdAsync(Guid id)
    {
        return await _context.Activos
            .Include(a => a.Intervenciones)
                .ThenInclude(i => i.Tecnico)
            .Include(a => a.UbicacionRelacion)
            .AsSplitQuery()
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<Activo?> GetByCodigoAsync(string codigo)
    {
        return await _context.Activos
            .Include(a => a.Intervenciones)
                .ThenInclude(i => i.Tecnico)
            .AsSplitQuery()
            .FirstOrDefaultAsync(a => a.Codigo.ToLower() == codigo.Trim().ToLower());
    }

    public async Task<Activo> CreateAsync(Activo activo, Guid? tecnicoId = null)
    {
        if (!activo.UbicacionId.HasValue && !string.IsNullOrWhiteSpace(activo.Sede))
        {
            var sedeNorm = activo.Sede.Trim().ToLower();
            var deptoNorm = (activo.Departamento ?? "General").Trim().ToLower();
            var matchUbi = await _context.Ubicaciones
                .FirstOrDefaultAsync(u => u.Sede.ToLower() == sedeNorm && u.Departamento.ToLower() == deptoNorm);
            if (matchUbi != null)
            {
                activo.UbicacionId = matchUbi.Id;
            }
        }
        else if (activo.UbicacionId.HasValue)
        {
            var ubi = await _context.Ubicaciones.FindAsync(activo.UbicacionId.Value);
            if (ubi != null)
            {
                activo.Sede = ubi.Sede;
                activo.Departamento = ubi.Departamento;
            }
        }

        _context.Activos.Add(activo);

        // Hito inicial de trazabilidad: alta del equipo
        var detalle = new StringBuilder($"[ALTA] Equipo {activo.Codigo} registrado en inventario");
        var desc = $"{activo.Tipo} {activo.Marca} {activo.Modelo}".Trim();
        if (!string.IsNullOrWhiteSpace(desc)) detalle.Append($" ({desc})");
        detalle.Append('.');
        detalle.Append($"\nEstado inicial: {activo.Estado}");
        var ubicacionInicial = FormatUbicacion(activo.Sede, activo.Departamento, activo.UbicacionTexto);
        detalle.Append($"\nUbicación: {ubicacionInicial}");
        if (!string.IsNullOrWhiteSpace(activo.Responsable)) detalle.Append($"\nResponsable: {activo.Responsable}");

        _context.Intervenciones.Add(new Intervencion
        {
            ActivoId = activo.Id,
            Descripcion = detalle.ToString(),
            TecnicoId = tecnicoId ?? activo.RegistradoPorId,
            Fecha = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return activo;
    }

    /// <summary>
    /// Aplica cambios parciales y registra automáticamente un hito de trazabilidad
    /// con el detalle (antes → después) de cada campo modificado.
    /// </summary>
    public async Task<Activo?> UpdateAsync(Guid id, Action<Activo> applyChanges, Guid? tecnicoId = null)
    {
        var activo = await _context.Activos.FindAsync(id);
        if (activo == null) return null;

        var antes = Snapshot.From(activo);
        applyChanges(activo);
        var despues = Snapshot.From(activo);

        var descripcion = BuildAuditDescription(antes, despues);
        if (descripcion != null)
        {
            activo.FechaActualizacion = DateTime.UtcNow;
            _context.Intervenciones.Add(new Intervencion
            {
                ActivoId = activo.Id,
                Descripcion = descripcion,
                TecnicoId = tecnicoId,
                Fecha = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        return activo;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var activo = await _context.Activos.FindAsync(id);
        if (activo == null) return false;

        _context.Activos.Remove(activo);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<Intervencion> AddIntervencionAsync(Guid activoId, string descripcion, Guid? tecnicoId)
    {
        var intervencion = new Intervencion
        {
            ActivoId = activoId,
            Descripcion = descripcion,
            TecnicoId = tecnicoId,
            Fecha = DateTime.UtcNow
        };

        _context.Intervenciones.Add(intervencion);
        await _context.SaveChangesAsync();
        return intervencion;
    }

    // ------------------------------------------------------------------
    // Helpers de auditoría
    // ------------------------------------------------------------------

    private record Snapshot(
        string Codigo, string Tipo, string Estado, string? Marca, string? Modelo,
        string? NumeroSerie, string? CodigoFactura, string? Sede, string? Departamento,
        string? Ubicacion, string? Responsable, string? Observaciones)
    {
        public static Snapshot From(Activo a) => new(
            a.Codigo, a.Tipo, a.Estado, a.Marca, a.Modelo, a.NumeroSerie, a.CodigoFactura,
            a.Sede, a.Departamento, a.UbicacionTexto, a.Responsable, a.Observaciones);
    }

    private static bool Changed(string? a, string? b) =>
        !string.Equals((a ?? "").Trim(), (b ?? "").Trim(), StringComparison.OrdinalIgnoreCase);

    private static string Show(string? v) => string.IsNullOrWhiteSpace(v) ? "—" : v.Trim();

    private static string FormatUbicacion(string? sede, string? depto, string? ubic)
    {
        var parts = new[] { sede, depto, ubic }.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p!.Trim());
        var s = string.Join(" / ", parts);
        return string.IsNullOrWhiteSpace(s) ? "Almacén TI" : s;
    }

    private static string? BuildAuditDescription(Snapshot a, Snapshot d)
    {
        var estadoCambio = Changed(a.Estado, d.Estado);
        var ubicCambio = Changed(a.Sede, d.Sede) || Changed(a.Departamento, d.Departamento) || Changed(a.Ubicacion, d.Ubicacion);
        var respCambio = Changed(a.Responsable, d.Responsable);
        var obsCambio = Changed(a.Observaciones, d.Observaciones);

        var datosCambio = new List<string>();
        if (Changed(a.Codigo, d.Codigo)) datosCambio.Add($"Código: {Show(a.Codigo)} → {Show(d.Codigo)}");
        if (Changed(a.Tipo, d.Tipo)) datosCambio.Add($"Tipo: {Show(a.Tipo)} → {Show(d.Tipo)}");
        if (Changed(a.Marca, d.Marca)) datosCambio.Add($"Marca: {Show(a.Marca)} → {Show(d.Marca)}");
        if (Changed(a.Modelo, d.Modelo)) datosCambio.Add($"Modelo: {Show(a.Modelo)} → {Show(d.Modelo)}");
        if (Changed(a.NumeroSerie, d.NumeroSerie)) datosCambio.Add($"S/N: {Show(a.NumeroSerie)} → {Show(d.NumeroSerie)}");
        if (Changed(a.CodigoFactura, d.CodigoFactura)) datosCambio.Add($"Factura: {Show(a.CodigoFactura)} → {Show(d.CodigoFactura)}");

        if (!estadoCambio && !ubicCambio && !respCambio && !obsCambio && datosCambio.Count == 0)
            return null; // Sin cambios reales: no se ensucia el historial

        var nuevoEstado = (d.Estado ?? "").ToUpperInvariant();
        string tag;
        string titulo;

        if (estadoCambio && nuevoEstado == "REPARACION")
        {
            tag = "[TALLER]"; titulo = "Equipo ingresado a taller por falla";
        }
        else if (estadoCambio && nuevoEstado == "OPERATIVO" && string.Equals(a.Estado, "REPARACION", StringComparison.OrdinalIgnoreCase))
        {
            tag = "[REPARADO]"; titulo = "Equipo reparado y devuelto a operación";
        }
        else if (estadoCambio && nuevoEstado == "BAJA")
        {
            tag = "[BAJA]"; titulo = "Equipo declarado en baja / chatarra";
        }
        else if (estadoCambio && nuevoEstado == "RESCATADO")
        {
            tag = "[RESCATE]"; titulo = "Piezas rescatadas para repuestos";
        }
        else if (ubicCambio || respCambio)
        {
            var estabaAsignado = !string.IsNullOrWhiteSpace(a.Sede) || !string.IsNullOrWhiteSpace(a.Responsable);
            tag = estabaAsignado ? "[TRASLADO]" : "[ASIGNACION]";
            titulo = estabaAsignado ? "Equipo reasignado / trasladado" : "Equipo asignado a área";
        }
        else if (estadoCambio)
        {
            tag = "[ESTADO]"; titulo = $"Cambio de estado a {d.Estado}";
        }
        else
        {
            tag = "[EDICION]"; titulo = "Datos del equipo actualizados";
        }

        var sb = new StringBuilder($"{tag} {titulo}.");
        if (estadoCambio) sb.Append($"\nEstado: {Show(a.Estado)} → {Show(d.Estado)}");
        if (ubicCambio)
            sb.Append($"\nUbicación: {FormatUbicacion(a.Sede, a.Departamento, a.Ubicacion)} → {FormatUbicacion(d.Sede, d.Departamento, d.Ubicacion)}");
        if (respCambio) sb.Append($"\nResponsable: {Show(a.Responsable)} → {Show(d.Responsable)}");
        foreach (var linea in datosCambio) sb.Append($"\n{linea}");
        if (obsCambio && !string.IsNullOrWhiteSpace(d.Observaciones)) sb.Append($"\nObservación: {d.Observaciones!.Trim()}");

        return sb.ToString();
    }
}
