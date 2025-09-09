# 🚀 Capturer v4.0 - Plan Técnico de Implementación

## 📋 Resumen Ejecutivo

### Visión del Proyecto
Capturer v4.0 representa una evolución arquitectural mayor, transformando la aplicación de escritorio standalone en un **sistema distribuido empresa-ready** con capacidades de administración centralizada, análisis avanzado y acceso multiplataforma.

### Componentes Principales
1. **Capturer Desktop v4.0** - Cliente de captura mejorado con API REST integrada
2. **Capturer Dashboard Web** - Portal administrativo centralizado (aplicación separada)
3. **Capturer API Gateway** - Capa de comunicación bidireccional
4. **Capturer Analytics Engine** - Motor de análisis y reportería avanzado

---

## 🏗️ Arquitectura Técnica v4.0

### Diagrama de Alto Nivel
```
┌────────────────────────────────────────────────────────────────────┐
│                     CAPTURER ECOSYSTEM v4.0                        │
├────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌─────────────────┐         ┌─────────────────────┐              │
│  │ CAPTURER CLIENT │  REST   │  DASHBOARD WEB APP  │              │
│  │     (v4.0)      │◄──────►│   (Separate App)    │              │
│  └────────┬────────┘         └──────────┬──────────┘              │
│           │                              │                         │
│           ▼                              ▼                         │
│  ┌─────────────────────────────────────────────────┐              │
│  │            CAPTURER API GATEWAY                 │              │
│  │         (ASP.NET Core 8 Minimal API)           │              │
│  └─────────────────────┬───────────────────────────┘              │
│                        │                                           │
│           ┌────────────┴────────────┐                             │
│           ▼                          ▼                             │
│  ┌──────────────────┐      ┌──────────────────┐                  │
│  │   DATA LAYER     │      │  ANALYTICS ENGINE │                  │
│  │  (PostgreSQL)    │      │  (Background Jobs) │                  │
│  └──────────────────┘      └──────────────────┘                  │
└────────────────────────────────────────────────────────────────────┘
```

### Stack Tecnológico Detallado

#### Capturer Desktop Client v4.0
```yaml
Framework: .NET 8 Windows Forms
Nuevas Dependencias:
  - ASP.NET Core 8 (Hosted Service para API)
  - Microsoft.AspNetCore.Mvc.NewtonsoftJson
  - Polly (Retry policies)
  - Serilog.AspNetCore (Structured logging)
  - System.IdentityModel.Tokens.Jwt (JWT auth)
  - RestSharp (HTTP client mejorado)
  
Arquitectura Interna:
  - MVVM Pattern migration (parcial)
  - Dependency Injection expandido
  - Background Services para API
  - Event-driven architecture
  - Circuit breaker patterns
```

#### Dashboard Web Application
```yaml
Backend:
  Framework: ASP.NET Core 8 MVC + Web API
  ORM: Entity Framework Core 8
  Database: PostgreSQL 16 / SQLite (dev)
  Caching: Redis / In-Memory
  Background Jobs: Hangfire
  Real-time: SignalR Core
  
Frontend:
  Framework: Vanilla JS + Web Components
  UI Library: Bootstrap 5.3
  Charts: Chart.js 4.4 + ApexCharts
  State Management: Redux-like pattern
  Build Tool: Vite
  PWA: Workbox 7
  
Testing:
  Unit: xUnit + Moq
  Integration: WebApplicationFactory
  E2E: Playwright
  Performance: k6
```

---

## 🔌 Integración Capturer-Dashboard

### 1. Protocolo de Comunicación

#### Modelo de Datos Compartido
```csharp
// Shared.Models/DTOs/ActivityReportDto.cs
namespace Capturer.Shared.Models.DTOs
{
    public class ActivityReportDto
    {
        public Guid Id { get; set; }
        public string ComputerId { get; set; }
        public string ComputerName { get; set; }
        public DateTime ReportDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public List<QuadrantActivityDto> QuadrantActivities { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
        public string Version { get; set; } = "4.0.0";
    }

    public class QuadrantActivityDto
    {
        public string QuadrantName { get; set; }
        public long TotalComparisons { get; set; }
        public long ActivityDetectionCount { get; set; }
        public double ActivityRate { get; set; }
        public double AverageChangePercentage { get; set; }
        public TimeSpan ActiveDuration { get; set; }
        public List<ActivityTimelineEntry> Timeline { get; set; }
    }

    public class ActivityTimelineEntry
    {
        public DateTime Timestamp { get; set; }
        public double ActivityLevel { get; set; }
        public Dictionary<string, double> QuadrantLevels { get; set; }
    }
}
```

#### API Contract Definition
```csharp
// Capturer.Api/Contracts/ICapturerApi.cs
public interface ICapturerApi
{
    // Health & Status
    [HttpGet("/api/v1/health")]
    Task<HealthCheckResult> GetHealthAsync();
    
    [HttpGet("/api/v1/status")]
    Task<SystemStatusDto> GetStatusAsync();
    
    // Configuration
    [HttpGet("/api/v1/config")]
    Task<ConfigurationDto> GetConfigurationAsync();
    
    [HttpPut("/api/v1/config")]
    Task<IActionResult> UpdateConfigurationAsync(ConfigurationDto config);
    
    // Activity Reports
    [HttpGet("/api/v1/activity/current")]
    Task<ActivityReportDto> GetCurrentActivityAsync();
    
    [HttpGet("/api/v1/activity/history")]
    Task<List<ActivityReportDto>> GetActivityHistoryAsync(
        DateTime? from, DateTime? to, int? limit);
    
    [HttpPost("/api/v1/activity/sync")]
    Task<SyncResult> SyncActivityToDashboardAsync(ActivityReportDto report);
    
    // Real-time Streaming
    [HttpGet("/api/v1/stream/activity")]
    Task StreamActivityUpdatesAsync(CancellationToken ct);
    
    // Commands
    [HttpPost("/api/v1/commands/capture")]
    Task<CommandResult> TriggerCaptureAsync();
    
    [HttpPost("/api/v1/commands/report")]
    Task<CommandResult> GenerateReportAsync(ReportRequest request);
}
```

### 2. Implementación en Capturer v4.0

#### API Service Implementation
```csharp
// Services/CapturerApiService.cs
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Capturer.Services;

public class CapturerApiService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CapturerApiService> _logger;
    private WebApplication? _app;
    
    public CapturerApiService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<CapturerApiService> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var apiConfig = _configuration.GetSection("CapturerApi");
            if (!apiConfig.GetValue<bool>("Enabled"))
            {
                _logger.LogInformation("Capturer API is disabled in configuration");
                return;
            }
            
            var port = apiConfig.GetValue<int>("Port", 8080);
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                ApplicationName = "Capturer API",
                ContentRootPath = AppContext.BaseDirectory
            });
            
            // Configure services
            ConfigureServices(builder.Services);
            
            _app = builder.Build();
            
            // Configure middleware pipeline
            ConfigureMiddleware(_app);
            
            // Map endpoints
            MapEndpoints(_app);
            
            _logger.LogInformation("Starting Capturer API on port {Port}", port);
            
            await _app.RunAsync($"http://localhost:{port}", stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Capturer API");
            throw;
        }
    }
    
    private void ConfigureServices(IServiceCollection services)
    {
        // Add API services
        services.AddControllers()
            .AddNewtonsoftJson();
        
        // Add authentication
        services.AddAuthentication("ApiKey")
            .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
                "ApiKey", null);
        
        // Add authorization
        services.AddAuthorization(options =>
        {
            options.AddPolicy("ApiKeyPolicy", policy =>
                policy.RequireAuthenticatedUser());
        });
        
        // Add CORS
        services.AddCors(options =>
        {
            options.AddPolicy("DashboardPolicy", builder =>
            {
                builder
                    .WithOrigins(_configuration["CapturerApi:DashboardUrl"])
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
        });
        
        // Add SignalR for real-time
        services.AddSignalR();
        
        // Add health checks
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy());
        
        // Register application services
        services.AddSingleton(_serviceProvider.GetRequiredService<ScreenshotService>());
        services.AddSingleton(_serviceProvider.GetRequiredService<ActivityReportService>());
        services.AddSingleton(_serviceProvider.GetRequiredService<QuadrantService>());
        
        // Add HttpClient for dashboard communication
        services.AddHttpClient<DashboardSyncService>(client =>
        {
            client.BaseAddress = new Uri(_configuration["CapturerApi:DashboardUrl"]);
            client.DefaultRequestHeaders.Add("X-Api-Key", 
                _configuration["CapturerApi:ApiKey"]);
        })
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy());
    }
    
    private void ConfigureMiddleware(WebApplication app)
    {
        // Exception handling
        app.UseExceptionHandler("/error");
        
        // Security headers
        app.Use(async (context, next) =>
        {
            context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Add("X-Frame-Options", "DENY");
            context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
            await next();
        });
        
        // Request logging
        app.UseSerilogRequestLogging();
        
        // CORS
        app.UseCors("DashboardPolicy");
        
        // Authentication & Authorization
        app.UseAuthentication();
        app.UseAuthorization();
        
        // Health checks
        app.MapHealthChecks("/health");
    }
    
    private void MapEndpoints(WebApplication app)
    {
        // Map controllers
        app.MapControllers()
            .RequireAuthorization("ApiKeyPolicy");
        
        // Map SignalR hub
        app.MapHub<ActivityHub>("/hubs/activity")
            .RequireAuthorization("ApiKeyPolicy");
        
        // Map minimal API endpoints
        var apiGroup = app.MapGroup("/api/v1")
            .RequireAuthorization("ApiKeyPolicy");
        
        // Status endpoint
        apiGroup.MapGet("/status", async (
            ScreenshotService screenshotService,
            ActivityReportService reportService) =>
        {
            var status = new SystemStatusDto
            {
                IsCapturing = screenshotService.IsCapturing,
                LastCaptureTime = screenshotService.LastCaptureTime,
                TotalScreenshots = await reportService.GetTotalScreenshotsAsync(),
                CurrentActivity = await reportService.GetCurrentActivityAsync(),
                SystemInfo = GetSystemInfo()
            };
            return Results.Ok(status);
        });
        
        // Activity sync endpoint
        apiGroup.MapPost("/activity/sync", async (
            ActivityReportDto report,
            DashboardSyncService syncService,
            ILogger<Program> logger) =>
        {
            try
            {
                var result = await syncService.SyncReportAsync(report);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to sync activity report");
                return Results.Problem("Sync failed", statusCode: 500);
            }
        });
        
        // Command endpoints
        apiGroup.MapPost("/commands/capture", async (
            ScreenshotService screenshotService) =>
        {
            await screenshotService.CaptureScreenshotAsync();
            return Results.Ok(new { success = true, timestamp = DateTime.UtcNow });
        });
    }
    
    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => !msg.IsSuccessStatusCode)
            .WaitAndRetryAsync(
                3,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    var logger = context.Values["logger"] as ILogger;
                    logger?.LogWarning("Retry {RetryCount} after {TimeSpan}s", 
                        retryCount, timespan.TotalSeconds);
                });
    }
    
    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                5,
                TimeSpan.FromSeconds(30),
                onBreak: (result, timespan) =>
                {
                    // Log circuit breaker opened
                },
                onReset: () =>
                {
                    // Log circuit breaker closed
                });
    }
}
```

### 3. Dashboard Sync Service
```csharp
// Services/DashboardSyncService.cs
namespace Capturer.Services;

public class DashboardSyncService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DashboardSyncService> _logger;
    private readonly IConfiguration _configuration;
    private readonly Queue<ActivityReportDto> _pendingReports;
    private readonly SemaphoreSlim _syncSemaphore;
    
    public DashboardSyncService(
        HttpClient httpClient,
        ILogger<DashboardSyncService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
        _pendingReports = new Queue<ActivityReportDto>();
        _syncSemaphore = new SemaphoreSlim(1, 1);
    }
    
    public async Task<SyncResult> SyncReportAsync(ActivityReportDto report)
    {
        await _syncSemaphore.WaitAsync();
        try
        {
            // Add to pending queue
            _pendingReports.Enqueue(report);
            
            // Try to sync
            var response = await _httpClient.PostAsJsonAsync(
                "/api/reports", report);
            
            if (response.IsSuccessStatusCode)
            {
                _pendingReports.Dequeue();
                var result = await response.Content.ReadFromJsonAsync<SyncResult>();
                _logger.LogInformation("Report synced successfully: {ReportId}", 
                    result?.ReportId);
                return result ?? new SyncResult { Success = true };
            }
            else
            {
                _logger.LogWarning("Failed to sync report: {StatusCode}", 
                    response.StatusCode);
                return new SyncResult 
                { 
                    Success = false, 
                    Error = $"HTTP {response.StatusCode}" 
                };
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error during sync");
            return new SyncResult 
            { 
                Success = false, 
                Error = "Network error" 
            };
        }
        finally
        {
            _syncSemaphore.Release();
        }
    }
    
    public async Task SyncPendingReportsAsync()
    {
        while (_pendingReports.Count > 0)
        {
            var report = _pendingReports.Peek();
            var result = await SyncReportAsync(report);
            
            if (!result.Success)
            {
                _logger.LogWarning("Failed to sync pending report, will retry later");
                break;
            }
        }
    }
}
```

---

## 📊 Dashboard Web Application - Arquitectura

### Estructura del Proyecto
```
CapturerDashboard/
├── src/
│   ├── CapturerDashboard.Web/           # MVC Web Application
│   │   ├── Controllers/
│   │   ├── Views/
│   │   ├── wwwroot/
│   │   └── Program.cs
│   │
│   ├── CapturerDashboard.Api/           # Web API
│   │   ├── Controllers/
│   │   ├── Hubs/
│   │   ├── Middleware/
│   │   └── Program.cs
│   │
│   ├── CapturerDashboard.Core/          # Business Logic
│   │   ├── Services/
│   │   ├── Interfaces/
│   │   └── Models/
│   │
│   ├── CapturerDashboard.Data/          # Data Access Layer
│   │   ├── Context/
│   │   ├── Entities/
│   │   ├── Repositories/
│   │   └── Migrations/
│   │
│   └── CapturerDashboard.Shared/        # Shared DTOs
│       └── DTOs/
│
├── tests/
│   ├── CapturerDashboard.Tests.Unit/
│   ├── CapturerDashboard.Tests.Integration/
│   └── CapturerDashboard.Tests.E2E/
│
├── docker/
│   ├── Dockerfile
│   └── docker-compose.yml
│
└── docs/
    ├── API.md
    ├── DEPLOYMENT.md
    └── ARCHITECTURE.md
```

### Database Schema Actualizado
```sql
-- Main database: capturer_dashboard

-- Organizations (Multi-tenant support)
CREATE TABLE organizations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL,
    slug VARCHAR(50) UNIQUE NOT NULL,
    subscription_tier VARCHAR(20) DEFAULT 'free',
    max_computers INT DEFAULT 5,
    data_retention_days INT DEFAULT 90,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
);

-- Computers with enhanced tracking
CREATE TABLE computers (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID REFERENCES organizations(id) ON DELETE CASCADE,
    computer_id VARCHAR(100) UNIQUE NOT NULL, -- Hardware ID from Capturer
    name VARCHAR(100) NOT NULL,
    ip_address INET,
    mac_address MACADDR,
    api_key VARCHAR(255) UNIQUE NOT NULL,
    api_key_hash VARCHAR(255) NOT NULL, -- Hashed for security
    last_seen_at TIMESTAMPTZ,
    is_active BOOLEAN DEFAULT TRUE,
    version VARCHAR(20),
    os_info JSONB, -- {"name": "Windows 11", "version": "22H2", "arch": "x64"}
    hardware_info JSONB, -- {"cpu": "Intel i7", "ram": 16, "monitors": 2}
    location JSONB, -- {"office": "Main", "department": "IT", "floor": 3}
    tags TEXT[],
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    
    INDEX idx_computers_org (organization_id),
    INDEX idx_computers_active (is_active, last_seen_at),
    INDEX idx_computers_tags (tags)
);

-- Activity reports with partitioning
CREATE TABLE activity_reports (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    computer_id UUID REFERENCES computers(id) ON DELETE CASCADE,
    report_date DATE NOT NULL,
    start_time TIMESTAMPTZ NOT NULL,
    end_time TIMESTAMPTZ NOT NULL,
    session_name VARCHAR(100),
    total_quadrants INT DEFAULT 0,
    total_comparisons BIGINT DEFAULT 0,
    total_activities BIGINT DEFAULT 0,
    average_activity_rate DECIMAL(5,2),
    peak_activity_time TIMESTAMPTZ,
    idle_time_minutes INT DEFAULT 0,
    report_data JSONB NOT NULL, -- Complete report JSON
    summary JSONB, -- Quick access summary
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    
    UNIQUE(computer_id, report_date),
    INDEX idx_reports_date (report_date DESC),
    INDEX idx_reports_computer (computer_id, report_date DESC)
) PARTITION BY RANGE (report_date);

-- Create monthly partitions
CREATE TABLE activity_reports_2024_01 PARTITION OF activity_reports
    FOR VALUES FROM ('2024-01-01') TO ('2024-02-01');
-- ... more partitions

-- Quadrant activities (denormalized for performance)
CREATE TABLE quadrant_activities (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    activity_report_id UUID REFERENCES activity_reports(id) ON DELETE CASCADE,
    quadrant_name VARCHAR(50) NOT NULL,
    total_comparisons BIGINT DEFAULT 0,
    activity_detection_count BIGINT DEFAULT 0,
    activity_rate DECIMAL(5,2) DEFAULT 0,
    average_change_percentage DECIMAL(5,2) DEFAULT 0,
    active_duration INTERVAL,
    timeline_data JSONB, -- Compressed timeline
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    
    INDEX idx_quadrant_report (activity_report_id),
    INDEX idx_quadrant_name (quadrant_name)
);

-- Real-time metrics (time-series data)
CREATE TABLE metrics (
    time TIMESTAMPTZ NOT NULL,
    computer_id UUID NOT NULL,
    metric_type VARCHAR(50) NOT NULL,
    quadrant_name VARCHAR(50),
    value DOUBLE PRECISION NOT NULL,
    tags JSONB,
    
    PRIMARY KEY (computer_id, time, metric_type, quadrant_name)
);

-- Create hypertable for time-series (if using TimescaleDB)
-- SELECT create_hypertable('metrics', 'time');

-- Alerts and notifications
CREATE TABLE alerts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID REFERENCES organizations(id),
    computer_id UUID REFERENCES computers(id),
    alert_type VARCHAR(50) NOT NULL, -- 'offline', 'low_activity', 'high_activity'
    severity VARCHAR(20) NOT NULL, -- 'info', 'warning', 'critical'
    title VARCHAR(200) NOT NULL,
    message TEXT,
    metadata JSONB,
    is_acknowledged BOOLEAN DEFAULT FALSE,
    acknowledged_by UUID REFERENCES users(id),
    acknowledged_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    
    INDEX idx_alerts_unack (organization_id, is_acknowledged, created_at DESC)
);

-- Users with role-based access
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID REFERENCES organizations(id),
    email VARCHAR(100) UNIQUE NOT NULL,
    username VARCHAR(50) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    full_name VARCHAR(100),
    role VARCHAR(20) NOT NULL DEFAULT 'viewer',
    permissions JSONB DEFAULT '{}',
    is_active BOOLEAN DEFAULT TRUE,
    last_login_at TIMESTAMPTZ,
    email_verified BOOLEAN DEFAULT FALSE,
    two_factor_enabled BOOLEAN DEFAULT FALSE,
    two_factor_secret VARCHAR(100),
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    
    INDEX idx_users_org (organization_id),
    INDEX idx_users_email (email)
);

-- Audit log
CREATE TABLE audit_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID REFERENCES users(id),
    organization_id UUID REFERENCES organizations(id),
    action VARCHAR(100) NOT NULL,
    entity_type VARCHAR(50),
    entity_id UUID,
    old_values JSONB,
    new_values JSONB,
    ip_address INET,
    user_agent TEXT,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    
    INDEX idx_audit_org (organization_id, created_at DESC),
    INDEX idx_audit_user (user_id, created_at DESC)
);
```

---

## 🔧 Fases de Implementación Detalladas

### Fase 1: Preparación de Capturer v4.0 (2 semanas)

#### Sprint 1.1: Refactoring Base (Semana 1)
```yaml
Tareas:
  - Migrar a .NET 8 completo
  - Implementar patrón Repository
  - Expandir Dependency Injection
  - Agregar Serilog para logging estructurado
  - Crear proyecto Capturer.Shared para DTOs
  
Entregables:
  - Capturer.sln restructurado
  - Capturer.Desktop (WinForms)
  - Capturer.Core (Business Logic)
  - Capturer.Shared (DTOs/Models)
  - Unit tests básicos
```

#### Sprint 1.2: API Integration (Semana 2)
```yaml
Tareas:
  - Implementar CapturerApiService
  - Crear endpoints REST
  - Agregar autenticación por API Key
  - Implementar DashboardSyncService
  - Testing de integración
  
Entregables:
  - API funcionando en puerto 8080
  - Swagger documentation
  - Postman collection
  - Integration tests
```

### Fase 2: Dashboard Foundation (3 semanas)

#### Sprint 2.1: Backend Setup (Semana 3)
```yaml
Tareas:
  - Crear solución CapturerDashboard
  - Configurar Entity Framework Core
  - Implementar modelos y repositorios
  - Crear API controllers
  - Setup autenticación JWT
  
Entregables:
  - Proyecto base funcionando
  - Database migrations
  - API documentation
  - Auth system working
```

#### Sprint 2.2: Core Features (Semana 4)
```yaml
Tareas:
  - Computer registration endpoint
  - Activity report ingestion
  - Data aggregation services
  - Background job processing
  - Real-time hub setup
  
Entregables:
  - Complete API surface
  - SignalR integration
  - Hangfire jobs
  - Service layer tests
```

#### Sprint 2.3: Frontend Base (Semana 5)
```yaml
Tareas:
  - MVC views structure
  - Authentication UI
  - Dashboard layout
  - Computer management UI
  - Basic reporting views
  
Entregables:
  - Responsive UI
  - Login system
  - Navigation
  - CRUD operations
```

### Fase 3: Visualizations & Analytics (2 semanas)

#### Sprint 3.1: Charts Integration (Semana 6)
```yaml
Tareas:
  - Chart.js setup
  - Activity timeline charts
  - Quadrant comparison charts
  - Heatmap implementation
  - Export functionality
  
Entregables:
  - Interactive charts
  - Data export (CSV/PDF)
  - Print-friendly views
  - Chart customization
```

#### Sprint 3.2: Real-time & Alerts (Semana 7)
```yaml
Tareas:
  - SignalR real-time updates
  - Alert system implementation
  - Push notifications setup
  - Email notifications
  - Dashboard widgets
  
Entregables:
  - Live data updates
  - Alert management
  - Notification center
  - Performance metrics
```

### Fase 4: PWA & Deployment (1 semana)

#### Sprint 4.1: PWA & Polish (Semana 8)
```yaml
Tareas:
  - Service Worker implementation
  - Offline functionality
  - Mobile optimizations
  - Performance tuning
  - Security hardening
  
Entregables:
  - PWA installable
  - Offline mode
  - Mobile app
  - Load testing results
  - Security audit
```

---

## 🔒 Seguridad y Autenticación

### Modelo de Seguridad Multi-Capa

#### Capa 1: Capturer Client Authentication
```csharp
// Configuration/ApiKeyConfiguration.cs
public class ApiKeyConfiguration
{
    public string ComputerId { get; set; } = GenerateHardwareId();
    public string ApiKey { get; set; } = GenerateApiKey();
    public string DashboardUrl { get; set; }
    
    private static string GenerateHardwareId()
    {
        // Combine multiple hardware identifiers
        var cpu = GetCpuId();
        var motherboard = GetMotherboardId();
        var mac = GetMacAddress();
        
        var combined = $"{cpu}-{motherboard}-{mac}";
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
        return Convert.ToBase64String(hash);
    }
    
    private static string GenerateApiKey()
    {
        // Generate secure API key
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return $"cap_{Convert.ToBase64String(bytes)}";
    }
}
```

#### Capa 2: Dashboard JWT Authentication
```csharp
// Dashboard/Services/AuthenticationService.cs
public class AuthenticationService
{
    private readonly IConfiguration _configuration;
    private readonly UserManager<ApplicationUser> _userManager;
    
    public async Task<AuthResult> AuthenticateAsync(LoginDto login)
    {
        var user = await _userManager.FindByEmailAsync(login.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, login.Password))
        {
            return new AuthResult { Success = false };
        }
        
        // Generate JWT token
        var token = GenerateJwtToken(user);
        
        // Generate refresh token
        var refreshToken = GenerateRefreshToken();
        
        // Store refresh token
        await StoreRefreshTokenAsync(user.Id, refreshToken);
        
        return new AuthResult
        {
            Success = true,
            Token = token,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };
    }
    
    private string GenerateJwtToken(ApplicationUser user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Secret"]);
        
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("org_id", user.OrganizationId.ToString())
        };
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(24),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };
        
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
```

---

## 🚀 Deployment Strategy

### Docker Deployment
```dockerfile
# Dockerfile for Dashboard
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["CapturerDashboard.Web/CapturerDashboard.Web.csproj", "CapturerDashboard.Web/"]
COPY ["CapturerDashboard.Core/CapturerDashboard.Core.csproj", "CapturerDashboard.Core/"]
COPY ["CapturerDashboard.Data/CapturerDashboard.Data.csproj", "CapturerDashboard.Data/"]
RUN dotnet restore "CapturerDashboard.Web/CapturerDashboard.Web.csproj"
COPY . .
WORKDIR "/src/CapturerDashboard.Web"
RUN dotnet build "CapturerDashboard.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CapturerDashboard.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CapturerDashboard.Web.dll"]
```

### Docker Compose
```yaml
version: '3.8'

services:
  dashboard:
    image: capturer-dashboard:latest
    container_name: capturer-dashboard
    ports:
      - "5000:80"
      - "5001:443"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Host=postgres;Database=capturer;Username=capturer;Password=${DB_PASSWORD}
      - Jwt__Secret=${JWT_SECRET}
      - Redis__ConnectionString=redis:6379
    depends_on:
      - postgres
      - redis
    networks:
      - capturer-network

  postgres:
    image: postgres:16-alpine
    container_name: capturer-postgres
    environment:
      - POSTGRES_DB=capturer
      - POSTGRES_USER=capturer
      - POSTGRES_PASSWORD=${DB_PASSWORD}
    volumes:
      - postgres-data:/var/lib/postgresql/data
    ports:
      - "5432:5432"
    networks:
      - capturer-network

  redis:
    image: redis:7-alpine
    container_name: capturer-redis
    ports:
      - "6379:6379"
    volumes:
      - redis-data:/data
    networks:
      - capturer-network

volumes:
  postgres-data:
  redis-data:

networks:
  capturer-network:
    driver: bridge
```

---

## 📈 Monitoreo y Observabilidad

### Application Insights Integration
```csharp
// Program.cs
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
});

builder.Services.Configure<TelemetryConfiguration>(config =>
{
    config.TelemetryProcessorChainBuilder
        .Use(next => new CustomTelemetryProcessor(next))
        .Build();
});

// Custom metrics
public class MetricsService
{
    private readonly TelemetryClient _telemetryClient;
    
    public void TrackActivitySync(string computerId, bool success, double duration)
    {
        var telemetry = new EventTelemetry("ActivitySync");
        telemetry.Properties["ComputerId"] = computerId;
        telemetry.Properties["Success"] = success.ToString();
        telemetry.Metrics["Duration"] = duration;
        
        _telemetryClient.TrackEvent(telemetry);
    }
    
    public void TrackApiCall(string endpoint, int statusCode, double responseTime)
    {
        _telemetryClient.GetMetric("api.calls", "endpoint", "status")
            .TrackValue(1, endpoint, statusCode.ToString());
        
        _telemetryClient.GetMetric("api.response_time", "endpoint")
            .TrackValue(responseTime, endpoint);
    }
}
```

---

## 🎯 Métricas de Éxito

### KPIs Técnicos
- **API Response Time**: < 100ms p95
- **Dashboard Load Time**: < 2s on 3G
- **Data Sync Latency**: < 5 seconds
- **System Uptime**: > 99.9%
- **Error Rate**: < 0.1%

### KPIs de Negocio
- **Active Computers**: Tracked daily
- **Activity Reports Generated**: Per day/week/month
- **User Engagement**: Daily active users
- **Data Retention**: 90+ days
- **Alert Response Time**: < 5 minutes

---

## 📚 Documentación Requerida

1. **API Documentation** (Swagger/OpenAPI)
2. **Deployment Guide** (Step-by-step)
3. **Administrator Manual** (Configuration)
4. **User Guide** (End-user documentation)
5. **Troubleshooting Guide** (Common issues)
6. **Security Audit Report** (Penetration testing)
7. **Performance Baseline** (Load testing results)

---

## ✅ Checklist de Implementación v4.0

### Pre-Development
- [ ] Crear repositorios Git separados
- [ ] Setup CI/CD pipelines
- [ ] Configurar entornos (dev/staging/prod)
- [ ] Definir branching strategy
- [ ] Setup project boards

### Development Phase
- [ ] Capturer v4.0 refactoring complete
- [ ] API implementation tested
- [ ] Dashboard backend functional
- [ ] Frontend responsive design
- [ ] Charts and visualizations
- [ ] PWA configuration
- [ ] Security audit passed
- [ ] Performance testing complete

### Deployment Phase
- [ ] Docker images built
- [ ] Database migrations ready
- [ ] SSL certificates configured
- [ ] Monitoring setup
- [ ] Backup strategy implemented
- [ ] Documentation complete
- [ ] User training materials
- [ ] Go-live checklist validated

### Post-Deployment
- [ ] Monitor system health
- [ ] Gather user feedback
- [ ] Performance optimization
- [ ] Bug fixes and patches
- [ ] Feature roadmap planning