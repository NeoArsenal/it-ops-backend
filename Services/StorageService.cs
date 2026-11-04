using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using it_ops_backend.Services.Interfaces;

namespace it_ops_backend.Services;

public class StorageService : IStorageService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<StorageService> _logger;

    public StorageService(HttpClient httpClient, IConfiguration config, ILogger<StorageService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
    }

    public async Task<string> UploadFileAsync(IFormFile file, string folder = "incidencias")
    {
        var rawUrl = Environment.GetEnvironmentVariable("SUPABASE_URL") 
                     ?? _config["Supabase:Url"];
        var rawKey = Environment.GetEnvironmentVariable("SUPABASE_SERVICE_KEY") 
                     ?? _config["Supabase:ServiceKey"];
        var bucket = Environment.GetEnvironmentVariable("SUPABASE_BUCKET") 
                     ?? _config["Supabase:Bucket"] 
                     ?? "helpdesk-archivos";

        var supabaseUrl = rawUrl?.TrimEnd('/');
        var serviceKey = rawKey?.Replace("\"", "")?.Replace("'", "")?.Trim();

        if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(serviceKey))
        {
            throw new InvalidOperationException("Credenciales de Supabase Storage no configuradas.");
        }

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(ext)) ext = ".jpg";

        var rawBaseName = Path.GetFileNameWithoutExtension(file.FileName);
        var cleanBaseName = System.Text.RegularExpressions.Regex.Replace(rawBaseName ?? "", @"[^a-zA-Z0-9_\-]", "_");
        if (string.IsNullOrWhiteSpace(cleanBaseName)) cleanBaseName = Guid.NewGuid().ToString("N");

        var fileName = $"{folder}/{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-{cleanBaseName}{ext}";

        var requestUrl = $"{supabaseUrl}/storage/v1/object/{bucket}/{fileName}";
        using var stream = file.OpenReadStream();
        using var content = new StreamContent(stream);
        var mime = !string.IsNullOrWhiteSpace(file.ContentType)
            ? file.ContentType
            : (ext.Equals(".pdf", StringComparison.OrdinalIgnoreCase) ? "application/pdf" : "image/jpeg");
        content.Headers.ContentType = new MediaTypeHeaderValue(mime);

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
        {
            Content = content
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", serviceKey);
        request.Headers.Add("apikey", serviceKey);

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var errBody = await response.Content.ReadAsStringAsync();
            _logger.LogError("Error subiendo archivo a Supabase Storage: {Status} - {Body}", response.StatusCode, errBody);
            throw new InvalidOperationException($"No se pudo subir la imagen a Storage: {response.StatusCode} - {errBody}");
        }

        return $"{supabaseUrl}/storage/v1/object/public/{bucket}/{fileName}";
    }

    public async Task DeleteFileAsync(string publicUrl)
    {
        if (string.IsNullOrWhiteSpace(publicUrl)) return;

        var rawUrl = Environment.GetEnvironmentVariable("SUPABASE_URL") 
                     ?? _config["Supabase:Url"];
        var rawKey = Environment.GetEnvironmentVariable("SUPABASE_SERVICE_KEY") 
                     ?? _config["Supabase:ServiceKey"];
        var bucket = Environment.GetEnvironmentVariable("SUPABASE_BUCKET") 
                     ?? _config["Supabase:Bucket"] 
                     ?? "helpdesk-archivos";

        var supabaseUrl = rawUrl?.TrimEnd('/');
        var serviceKey = rawKey?.Replace("\"", "")?.Replace("'", "")?.Trim();

        if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(serviceKey)) return;

        var marker = $"/storage/v1/object/public/{bucket}/";
        var idx = publicUrl.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return;

        var relativePath = publicUrl[(idx + marker.Length)..];
        var requestUrl = $"{supabaseUrl}/storage/v1/object/{bucket}/{relativePath}";

        using var request = new HttpRequestMessage(HttpMethod.Delete, requestUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", serviceKey);
        request.Headers.Add("apikey", serviceKey);

        await _httpClient.SendAsync(request);
    }
}
