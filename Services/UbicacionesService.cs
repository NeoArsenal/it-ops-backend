using Microsoft.EntityFrameworkCore;
using it_ops_backend.Data;
using it_ops_backend.Models;
using it_ops_backend.Services.Interfaces;

namespace it_ops_backend.Services;

public class UbicacionesService : IUbicacionesService
{
    private readonly ApplicationDbContext _context;

    public UbicacionesService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Ubicacion>> GetAllAsync()
    {
        return await _context.Ubicaciones
            .Include(u => u.CreadoPor)
            .OrderByDescending(u => u.CreadoEn)
            .ToListAsync();
    }

    public async Task<List<string>> GetSedesAsync()
    {
        return await _context.Ubicaciones
            .Select(u => u.Sede)
            .Distinct()
            .ToListAsync();
    }

    public async Task<List<string>> GetDepartamentosPorSedeAsync(string sede)
    {
        return await _context.Ubicaciones
            .Where(u => u.Sede.ToLower() == sede.ToLower())
            .Select(u => u.Departamento)
            .Distinct()
            .ToListAsync();
    }

    public async Task<List<string>> GetAreasAsync(string sede, string departamento)
    {
        return await _context.Ubicaciones
            .Where(u => u.Sede.ToLower() == sede.ToLower() && u.Departamento.ToLower() == departamento.ToLower())
            .Select(u => u.Area)
            .Distinct()
            .ToListAsync();
    }

    public async Task<Ubicacion> CreateAsync(Ubicacion ubicacion, Guid? usuarioId)
    {
        ubicacion.CreadoPorId = usuarioId;
        _context.Ubicaciones.Add(ubicacion);
        await _context.SaveChangesAsync();
        return ubicacion;
    }
}

