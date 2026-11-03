using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using it_ops_backend.Data;
using it_ops_backend.DTOs;
using it_ops_backend.Models;
using it_ops_backend.Services.Interfaces;

namespace it_ops_backend.Services;

public class ConfiguracionService : IConfiguracionService
{
    private readonly ApplicationDbContext _context;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public ConfiguracionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<string> GetValueAsync(string clave, string defaultValue = "")
    {
        var config = await _context.Configuraciones.FindAsync(clave);
        return config != null ? config.Valor : defaultValue;
    }

    public async Task SetValueAsync(string clave, string valor, Guid? usuarioId)
    {
        var config = await _context.Configuraciones.FindAsync(clave);
        if (config == null)
        {
            config = new Configuracion
            {
                Clave = clave,
                Valor = valor,
                ActualizadoPorId = usuarioId,
                ActualizadoEn = DateTime.UtcNow
            };
            _context.Configuraciones.Add(config);
        }
        else
        {
            config.Valor = valor;
            if (usuarioId.HasValue)
            {
                config.ActualizadoPorId = usuarioId;
            }
            config.ActualizadoEn = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<string> GetPortalTokenAsync()
    {
        var token = await GetValueAsync("PORTAL_TOKEN", "");
        if (string.IsNullOrWhiteSpace(token))
        {
            token = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
            await SetValueAsync("PORTAL_TOKEN", token, null);
        }
        return token;
    }

    public async Task<string> RegeneratePortalTokenAsync(Guid? usuarioId)
    {
        var newToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        await SetValueAsync("PORTAL_TOKEN", newToken, usuarioId);
        return newToken;
    }

    public async Task<bool> VerifyAccessAsync(string? pin, string? token)
    {
        if (!string.IsNullOrWhiteSpace(token))
        {
            var actualToken = await GetPortalTokenAsync();
            if (token.Trim() == actualToken.Trim())
            {
                return true;
            }
        }
        if (!string.IsNullOrWhiteSpace(pin))
        {
            return await VerifyPinAsync(pin);
        }
        return false;
    }

    public async Task<bool> VerifyPinAsync(string pin)
    {
        var actualPin = await GetValueAsync("PORTAL_PIN", "2026");
        return (pin ?? "").Trim() == actualPin.Trim();
    }

    public async Task<object> GetCatalogosAsync()
    {
        var rawDeptos = await GetValueAsync("CATALOGO_DEPARTAMENTOS", "");
        var rawCategorias = await GetValueAsync("CATALOGO_CATEGORIAS_ACTIVOS", "");

        var defaultDeptos = new List<string> { "Urgencias", "Quirófano", "Recursos Humanos", "Administración", "Farmacia", "Consultorios", "Admisión" };
        var defaultCategorias = new List<string> { "Desktop / Laptop", "Impresora", "Bomba de Infusión", "Monitor Vital", "Router / Switch", "PC", "Monitor" };

        var departamentos = defaultDeptos;
        var categoriasActivos = defaultCategorias;

        if (!string.IsNullOrWhiteSpace(rawDeptos))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<string>>(rawDeptos, JsonOptions);
                if (parsed != null) departamentos = parsed;
            }
            catch { }
        }

        if (!string.IsNullOrWhiteSpace(rawCategorias))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<string>>(rawCategorias, JsonOptions);
                if (parsed != null) categoriasActivos = parsed;
            }
            catch { }
        }

        return new { departamentos, categoriasActivos };
    }

    public async Task<List<string>> SetCatalogoAsync(string tipo, List<string> items, Guid? usuarioId)
    {
        var clave = tipo == "departamentos" ? "CATALOGO_DEPARTAMENTOS" : "CATALOGO_CATEGORIAS_ACTIVOS";
        await SetValueAsync(clave, JsonSerializer.Serialize(items, JsonOptions), usuarioId);
        return items;
    }

    public async Task<ReglasGamificacionDto> GetReglasGamificacionAsync()
    {
        var raw = await GetValueAsync("REGLAS_GAMIFICACION", "");
        if (string.IsNullOrWhiteSpace(raw)) return new ReglasGamificacionDto();

        try
        {
            var parsed = JsonSerializer.Deserialize<ReglasGamificacionDto>(raw, JsonOptions);
            if (parsed == null) return new ReglasGamificacionDto();
            if (parsed.Niveles == null || parsed.Niveles.Count == 0)
            {
                parsed.Niveles = new ReglasGamificacionDto().Niveles;
            }
            return parsed;
        }
        catch
        {
            return new ReglasGamificacionDto();
        }
    }

    public async Task<ReglasGamificacionDto> SetReglasGamificacionAsync(ReglasGamificacionDto reglas, Guid? usuarioId)
    {
        var actual = await GetReglasGamificacionAsync();
        if (reglas.PuntosPorArea != null)
        {
            actual.PuntosPorArea = reglas.PuntosPorArea;
        }
        if (reglas.Niveles != null && reglas.Niveles.Count > 0)
        {
            actual.Niveles = reglas.Niveles;
        }

        await SetValueAsync("REGLAS_GAMIFICACION", JsonSerializer.Serialize(actual, JsonOptions), usuarioId);
        return actual;
    }
}
