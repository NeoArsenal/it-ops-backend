using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using it_ops_backend.Data;
using it_ops_backend.Hubs;
using it_ops_backend.Services;
using it_ops_backend.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Soporte para puerto dinámico en Render / Cloud (variable PORT)
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://*:{port}");
}

// 1. Configuración de Base de Datos PostgreSQL (Supabase / Render / Railway)
var rawConn = builder.Configuration.GetConnectionString("DefaultConnection") 
              ?? Environment.GetEnvironmentVariable("DATABASE_URL");

string connectionString;
if (!string.IsNullOrEmpty(rawConn) && (rawConn.StartsWith("postgres://") || rawConn.StartsWith("postgresql://")))
{
    // Convertir URI estándar de Postgres a formato Npgsql si viene de Render/Railway
    var uri = new Uri(rawConn);
    var userInfo = uri.UserInfo.Split(':');
    var user = userInfo[0];
    var pass = userInfo.Length > 1 ? userInfo[1] : "";
    connectionString = $"Host={uri.Host};Port={(uri.Port > 0 ? uri.Port : 5432)};Database={uri.AbsolutePath.TrimStart('/')};Username={user};Password={pass};SSL Mode=Require;Trust Server Certificate=true";
}
else
{
    connectionString = rawConn ?? "";
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// 2. Cliente HTTP y Servicios del Dominio (Inyección de Dependencias)
builder.Services.AddHttpClient<IStorageService, StorageService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITicketsService, TicketsService>();
builder.Services.AddScoped<IActivosService, ActivosService>();
builder.Services.AddScoped<IUbicacionesService, UbicacionesService>();
builder.Services.AddScoped<IConfiguracionService, ConfiguracionService>();
builder.Services.AddScoped<IUsuariosService, UsuariosService>();
builder.Services.AddScoped<IGamificacionService, GamificacionService>();
builder.Services.AddScoped<IAcademiaService, AcademiaService>();
builder.Services.AddScoped<IRedService, RedService>();
builder.Services.AddScoped<IGuiasService, GuiasService>();
builder.Services.AddScoped<IPushNotificationService, PushNotificationService>();

// 3. WebSockets en Tiempo Real (SignalR)
builder.Services.AddSignalR();

// 4. Configurar Autenticación JWT Bearer
var jwtSecret = builder.Configuration["Jwt:Secret"] 
                ?? Environment.GetEnvironmentVariable("JWT_SECRET") 
                ?? "super-secret-key-cambiar-en-produccion-it-ops-2026";
var key = Encoding.UTF8.GetBytes(jwtSecret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero
    };
});

// 5. Configurar CORS (Permite comunicación con Next.js y Kiosco)
var allowedOriginsEnv = Environment.GetEnvironmentVariable("ALLOWED_ORIGINS");
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        if (!string.IsNullOrWhiteSpace(allowedOriginsEnv))
        {
            var origins = allowedOriginsEnv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            policy.WithOrigins(origins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
        else
        {
            policy.SetIsOriginAllowed(_ => true)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
    });
});

// Configurar encabezados proxy para terminación SSL en producción (Docker, Render, Cloud)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Ignorar ciclos de referencia al serializar relaciones EF Core
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddEndpointsApiExplorer();

// 6. Swagger con soporte para Autorización JWT
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "IT Ops API (.NET 8)", 
        Version = "v1",
        Description = "Backend corporativo de Mesa de Ayuda, Gamificación y Gestión de Operaciones TI."
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Encabezado de autorización JWT. Ejemplo: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Manejo de Proxies reversos para SSL
app.UseForwardedHeaders();

// Pipeline de Middleware
var enableSwagger = app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("EnableSwagger");
if (enableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "IT Ops API v1");
    });
}

// Redirección HTTPS sólo en local fuera de contenedores Docker proxy
if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

// Endpoint de monitoreo para health checks en producción
app.MapGet("/health", () => Results.Ok(new 
{ 
    status = "healthy", 
    framework = ".NET 8", 
    service = "IT Ops Backend",
    timestamp = DateTime.UtcNow 
}));

app.MapControllers();

// Mapeo del canal de WebSockets
app.MapHub<NotificacionesHub>("/ws/notificaciones");

app.Run();
