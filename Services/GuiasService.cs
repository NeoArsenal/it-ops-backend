using Microsoft.EntityFrameworkCore;
using it_ops_backend.Data;
using it_ops_backend.DTOs;
using it_ops_backend.Models;
using it_ops_backend.Services.Interfaces;

namespace it_ops_backend.Services;

public class GuiasService : IGuiasService
{
    private readonly ApplicationDbContext _context;
    private readonly IGamificacionService _gamificacionService;
    private readonly IConfiguracionService _configService;

    public GuiasService(
        ApplicationDbContext context,
        IGamificacionService gamificacionService,
        IConfiguracionService configService)
    {
        _context = context;
        _gamificacionService = gamificacionService;
        _configService = configService;
    }

    public async Task<List<Guia>> GetAllAsync()
    {
        return await _context.Guias
            .Include(g => g.Autor)
            .OrderByDescending(g => g.FechaSubida)
            .ToListAsync();
    }

    public async Task<List<Guia>> SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return await GetAllAsync();
        var q = query.ToLowerInvariant();
        return await _context.Guias
            .Include(g => g.Autor)
            .Where(g => g.Titulo.ToLower().Contains(q) || (g.ContenidoRichText != null && g.ContenidoRichText.ToLower().Contains(q)))
            .OrderByDescending(g => g.FechaSubida)
            .ToListAsync();
    }

    public async Task<Guia?> GetByIdAsync(Guid id)
    {
        return await _context.Guias
            .Include(g => g.Autor)
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task<Guia> CreateAsync(CreateGuiaDto dto)
    {
        var guia = new Guia
        {
            Titulo = dto.Titulo,
            UrlPdf = dto.UrlPdf,
            ContenidoRichText = dto.ContenidoRichText,
            Peso = dto.Peso,
            AutorId = dto.AutorId,
            FechaSubida = DateTime.UtcNow
        };

        _context.Guias.Add(guia);
        await _context.SaveChangesAsync();

        if (dto.AutorId.HasValue)
        {
            try
            {
                var reglas = await _configService.GetReglasGamificacionAsync();
                await _gamificacionService.OtorgarXPAsync(
                    dto.AutorId.Value,
                    reglas.PuntosPorArea.GuiaCreada,
                    "GUIA_CREADA",
                    $"Creó la guía \"{guia.Titulo}\""
                );
            }
            catch { }
        }

        return guia;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var guia = await _context.Guias.FindAsync(id);
        if (guia == null) return false;

        _context.Guias.Remove(guia);
        await _context.SaveChangesAsync();
        return true;
    }
}

