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
            .Include(a => a.UbicacionRelacion)
            .OrderByDescending(a => a.FechaRegistro)
            .ToListAsync();
    }

    public async Task<Activo?> GetByIdAsync(Guid id)
    {
        return await _context.Activos
            .Include(a => a.Intervenciones)
                .ThenInclude(i => i.Tecnico)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<Activo?> GetByCodigoAsync(string codigo)
    {
        return await _context.Activos
            .Include(a => a.Intervenciones)
            .FirstOrDefaultAsync(a => a.Codigo.ToLower() == codigo.Trim().ToLower());
    }

    public async Task<Activo> CreateAsync(Activo activo)
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
        await _context.SaveChangesAsync();
        return activo;
    }

    public async Task<Activo?> UpdateAsync(Guid id, Action<Activo> applyChanges)
    {
        var activo = await _context.Activos.FindAsync(id);
        if (activo == null) return null;

        applyChanges(activo);
        activo.FechaActualizacion = DateTime.UtcNow;

        await _context.SaveChangesAsync();
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
}

