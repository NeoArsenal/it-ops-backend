using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using it_ops_backend.Data;
using it_ops_backend.Models;
using it_ops_backend.Services.Interfaces;

namespace it_ops_backend.Services;

public class AcademiaService : IAcademiaService
{
    private readonly ApplicationDbContext _context;
    private readonly IGamificacionService _gamificacionService;

    public AcademiaService(ApplicationDbContext context, IGamificacionService gamificacionService)
    {
        _context = context;
        _gamificacionService = gamificacionService;
    }

    public async Task<List<object>> GetCursosAsync(Guid userId)
    {
        var cursos = await _context.Cursos
            .Include(c => c.Niveles)
            .OrderBy(c => c.Orden)
            .ToListAsync();

        var progresos = await _context.ProgresosAcademia
            .Where(p => p.UsuarioId == userId && p.Completado)
            .ToListAsync();

        var nivelesCompletadosIds = progresos.Select(p => p.NivelId).ToHashSet();

        return cursos.Select(curso =>
        {
            var nivelesOrdenados = curso.Niveles.OrderBy(n => n.Orden).ToList();
            var previousCompleted = true;

            var nivelesConEstado = nivelesOrdenados.Select(nivel =>
            {
                var isCompleted = nivelesCompletadosIds.Contains(nivel.Id);
                string status;
                if (isCompleted)
                {
                    status = "COMPLETED";
                    previousCompleted = true;
                }
                else if (previousCompleted)
                {
                    status = "UNLOCKED";
                    previousCompleted = false;
                }
                else
                {
                    status = "LOCKED";
                }

                return new
                {
                    id = nivel.Id,
                    titulo = nivel.Titulo,
                    descripcion = nivel.Descripcion,
                    orden = nivel.Orden,
                    xpRecompensa = nivel.XpRecompensa,
                    cursoId = nivel.CursoId,
                    status,
                    isCompleted,
                    isUnlocked = isCompleted || status == "UNLOCKED"
                };
            }).ToList();

            return (object)new
            {
                id = curso.Id,
                titulo = curso.Titulo,
                descripcion = curso.Descripcion,
                icono = curso.Icono,
                orden = curso.Orden,
                niveles = nivelesConEstado
            };
        }).ToList();
    }

    public async Task<object?> GetNivelConPreguntasAsync(Guid nivelId)
    {
        var nivel = await _context.NivelesAcademia
            .Include(n => n.Preguntas)
            .Include(n => n.Curso)
            .FirstOrDefaultAsync(n => n.Id == nivelId);

        if (nivel == null) return null;

        var preguntasFormateadas = nivel.Preguntas.Select(p =>
        {
            List<string> opciones = new();
            try
            {
                var parsed = JsonSerializer.Deserialize<List<string>>(p.OpcionesJson);
                if (parsed != null) opciones = parsed;
            }
            catch { }

            return new
            {
                id = p.Id,
                texto = p.Texto,
                opciones,
                respuestaCorrecta = p.RespuestaCorrecta,
                explicacion = p.Explicacion
            };
        }).ToList();

        return new
        {
            id = nivel.Id,
            titulo = nivel.Titulo,
            descripcion = nivel.Descripcion,
            orden = nivel.Orden,
            xpRecompensa = nivel.XpRecompensa,
            cursoId = nivel.CursoId,
            preguntas = preguntasFormateadas
        };
    }

    public async Task<object> CompletarNivelAsync(Guid userId, Guid nivelId)
    {
        var nivel = await _context.NivelesAcademia.FindAsync(nivelId)
            ?? throw new KeyNotFoundException("Nivel no encontrado.");

        var progreso = await _context.ProgresosAcademia
            .FirstOrDefaultAsync(p => p.UsuarioId == userId && p.NivelId == nivelId);

        var primeraVez = false;
        if (progreso == null)
        {
            progreso = new ProgresoUsuario
            {
                UsuarioId = userId,
                NivelId = nivelId,
                Completado = true,
                PuntajeMaximo = 100,
                FechaCompletado = DateTime.UtcNow
            };
            _context.ProgresosAcademia.Add(progreso);
            primeraVez = true;
        }
        else if (!progreso.Completado)
        {
            progreso.Completado = true;
            progreso.FechaCompletado = DateTime.UtcNow;
            primeraVez = true;
        }

        await _context.SaveChangesAsync();

        var xpOtorgado = 0;
        if (primeraVez)
        {
            xpOtorgado = nivel.XpRecompensa;
            try
            {
                await _gamificacionService.OtorgarXPAsync(
                    userId,
                    xpOtorgado,
                    "TICKET_RESUELTO",
                    $"Nivel de Academia superado: {nivel.Titulo}"
                );
            }
            catch { }
        }

        return new
        {
            exito = true,
            primeraVez,
            xpGanado = xpOtorgado,
            mensaje = primeraVez ? $"¡Ganaste {xpOtorgado} XP!" : "Nivel completado nuevamente (Práctica)."
        };
    }
}

