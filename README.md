# IT Ops Backend (.NET 8 Web API)

API RESTful y servicio de mensajería en tiempo real (SignalR) de alto rendimiento para el sistema de Mesa de Ayuda y Gestión de Operaciones TI (Helpdesk, Activos, Redes e Infraestructura, Analítica y SLAs).

## 🚀 Tecnologías

- **Runtime:** .NET 8 LTS (ASP.NET Core Web API)
- **ORM & DB:** Entity Framework Core 8 con Npgsql / Supabase PostgreSQL (AWS us-east-1)
- **Normalización de Datos:** Estructura en Tercera Forma Normal (3NF) con claves foráneas `ubicacion_id` e índices optimizados B-Tree.
- **Tiempo Real:** SignalR Hub (`/hubs/itops`) con compatibilidad para clientes WebSockets frontend.
- **Seguridad:** Autenticación JWT Bearer, Claims-based authorization y CORS configurable.
- **Almacenamiento:** Integración con Supabase Storage (`helpdesk-archivos`).
- **Monitoreo:** Endpoint de salud nativo `/health`.
- **Despliegue:** Dockerfile multi-etapa optimizado para Linux/Alpine en producción.

---

## 🛠️ Variables de Entorno para Producción

El backend soporta configuración directa por variables de entorno para plataformas como Docker, Railway, Render, AWS ECS, Azure App Service o Kubernetes:

| Variable | Descripción | Ejemplo / Default |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | Entorno de ejecución | `Production` |
| `DATABASE_URL` | Cadena de conexión PostgreSQL (formato Npgsql o URL `postgres://...`) | `Host=...;Port=5432;Database=...;...` |
| `JWT_SECRET` | Clave secreta para firma de tokens JWT (mínimo 32 caracteres) | `mi_secreto_super_seguro_produccion_2026` |
| `JWT_ISSUER` | Emisor del token | `ItOpsBackend` |
| `JWT_AUDIENCE` | Audiencia esperada | `ItOpsFrontend` |
| `SUPABASE_URL` | URL de la instancia de Supabase | `https://xxxx.supabase.co` |
| `SUPABASE_SERVICE_KEY` | Service Role Key o API Key de Supabase Storage | `sb_secret_...` |
| `SUPABASE_BUCKET` | Nombre del bucket para adjuntos | `helpdesk-archivos` |
| `ALLOWED_ORIGINS` | Orígenes CORS permitidos (separados por coma) | `https://midominio.com,https://app.midominio.com` |
| `PORT` | Puerto de escucha HTTP | `8080` (predeterminado en Docker) |

---

## 🐳 Despliegue con Docker

### Construir imagen
```bash
docker build -t it-ops-backend:latest .
```

### Ejecutar contenedor
```bash
docker run -d -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e DATABASE_URL="Host=...;Port=5432;Database=postgres;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true" \
  -e JWT_SECRET="super-secret-key-produccion" \
  -e ALLOWED_ORIGINS="https://tudominio.com" \
  --name it-ops-api it-ops-backend:latest
```

---

## 🩺 Health Check

El servicio expone un endpoint sin autenticación para Load Balancers y orquestadores:
```http
GET /health
```
Retorna `200 OK` (`Healthy`) cuando la aplicación está operativa.

---

## 📦 Estructura del Proyecto

```
it-ops-backend/
├── Controllers/       # Controladores API (Auth, Tickets, Activos, Red, Ubicaciones, Gamificacion)
├── Data/              # ApplicationDbContext (EF Core con mapeos Npgsql y 3NF)
├── DTOs/              # Data Transfer Objects para requests y responses
├── Hubs/              # Hubs de SignalR para eventos en tiempo real
├── Models/            # Entidades de dominio (Ticket, Activo, Usuario, Red, etc.)
├── Services/          # Lógica de negocio (TicketsService, ActivosService, SupabaseArchivosService)
├── Dockerfile         # Configuración multi-etapa para producción
└── Program.cs         # Inicialización, DI, CORS, JWT, SignalR y Forwarded Headers
```

