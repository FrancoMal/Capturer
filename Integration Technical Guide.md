# 🔗 Guía Técnica de Integración - Capturer ↔ Dashboard Web

## 📌 Resumen de Integración

Esta guía técnica detalla la implementación paso a paso de la integración entre **Capturer Desktop v4.0** y **Dashboard Web**, dos aplicaciones independientes que se comunican vía API REST.

---

## 🎯 Quick Start - Integración en 10 Pasos

### Paso 1: Habilitar API en Capturer
```json
// appsettings.json en Capturer
{
  "CapturerApi": {
    "Enabled": true,
    "Port": 8080,
    "DashboardUrl": "https://dashboard.local",
    "SyncInterval": 300,
    "ApiKey": "" // Se genera automáticamente
  }
}
```

### Paso 2: Registrar Computadora en Dashboard
```bash
# Primera vez - Registro manual
curl -X POST https://dashboard.local/api/computers/register \
  -H "Content-Type: application/json" \
  -d '{
    "computerId": "HARDWARE-UUID-12345",
    "computerName": "PC-OFFICE-01",
    "organizationCode": "ACME2024"
  }'

# Respuesta
{
  "apiKey": "cap_xKj3n4m5p6q7r8s9",
  "dashboardUrl": "https://dashboard.local",
  "syncEnabled": true
}
```

### Paso 3: Configurar API Key en Capturer
```csharp
// Se guarda automáticamente en configuración encriptada
var config = ConfigurationManager.GetConfiguration();
config.CapturerApi.ApiKey = responseApiKey;
ConfigurationManager.SaveConfiguration(config);
```

### Paso 4: Iniciar Servicio API
```csharp
// Program.cs - Se auto-inicia con la aplicación
services.AddHostedService<CapturerApiService>();
```

### Paso 5-10: Flujo Automático
- ✅ Capturer inicia API en puerto 8080
- ✅ Dashboard detecta nueva computadora online
- ✅ Activity Reports se sincronizan automáticamente
- ✅ Real-time updates vía SignalR
- ✅ Alertas se generan según configuración
- ✅ Métricas disponibles en Dashboard

---

## 🏗️ Arquitectura de Comunicación Detallada

### Flujo de Datos Completo
```
┌──────────────────────────────────────────────────────────────────┐
│                    FLUJO DE DATOS COMPLETO                       │
├──────────────────────────────────────────────────────────────────┤
│                                                                   │
│  CAPTURER (Windows Desktop)          DASHBOARD (Web Server)      │
│  ┌─────────────────────┐            ┌─────────────────────┐     │
│  │                     │            │                     │     │
│  │  1. Screenshot      │            │                     │     │
│  │     Service         │            │   7. Process &     │     │
│  │        ↓            │            │      Store         │     │
│  │  2. Activity        │            │        ↑            │     │
│  │     Detection       │            │        │            │     │
│  │        ↓            │  HTTP/S    │   6. Validate      │     │
│  │  3. Report          │◄──────────►│      API Key       │     │
│  │     Generation      │            │        ↑            │     │
│  │        ↓            │            │        │            │     │
│  │  4. Queue for       │            │   5. Receive       │     │
│  │     Sync            │            │      Report        │     │
│  │        ↓            │            │        ↑            │     │
│  │  5. POST to         │────────────────────┘            │     │
│  │     Dashboard       │            │                     │     │
│  │                     │            │   8. Update UI     │     │
│  │                     │◄───────────│   9. Send Alerts   │     │
│  │                     │ WebSocket  │  10. Analytics     │     │
│  └─────────────────────┘            └─────────────────────┘     │
│                                                                   │
└──────────────────────────────────────────────────────────────────┘
```

---

## 💻 Implementación en Capturer v4.0

### 1. Estructura de Proyecto Actualizada
```
Capturer/
├── Capturer.Desktop/           # Windows Forms App
│   ├── Forms/
│   ├── Program.cs
│   └── Capturer.Desktop.csproj
│
├── Capturer.Core/              # Business Logic (NEW)
│   ├── Services/
│   │   ├── ScreenshotService.cs
│   │   ├── ActivityReportService.cs
│   │   └── QuadrantService.cs
│   └── Capturer.Core.csproj
│
├── Capturer.Api/               # API Layer (NEW)
│   ├── Services/
│   │   ├── CapturerApiService.cs
│   │   └── DashboardSyncService.cs
│   ├── Controllers/
│   │   └── ActivityController.cs
│   ├── Hubs/
│   │   └── ActivityHub.cs
│   └── Capturer.Api.csproj
│
├── Capturer.Shared/            # Shared DTOs (NEW)
│   ├── DTOs/
│   │   ├── ActivityReportDto.cs
│   │   └── QuadrantActivityDto.cs
│   └── Capturer.Shared.csproj
│
└── Capturer.sln
```

### 2. Modificaciones en Program.cs
```csharp
// Program.cs - Capturer Desktop
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Capturer.Api.Services;

namespace Capturer;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        
        var host = CreateHostBuilder().Build();
        
        // Start background services
        _ = host.RunAsync();
        
        // Run Windows Forms app
        var mainForm = host.Services.GetRequiredService<Form1>();
        Application.Run(mainForm);
    }
    
    static IHostBuilder CreateHostBuilder() =>
        Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Windows Forms
                services.AddSingleton<Form1>();
                
                // Core Services
                services.AddSingleton<ScreenshotService>();
                services.AddSingleton<ActivityReportService>();
                services.AddSingleton<QuadrantService>();
                services.AddSingleton<EmailService>();
                
                // API Services (NEW)
                services.AddHostedService<CapturerApiService>();
                services.AddSingleton<DashboardSyncService>();
                
                // Configuration
                services.AddSingleton<IConfiguration>(context.Configuration);
                
                // HTTP Client for Dashboard communication
                services.AddHttpClient<DashboardSyncService>(client =>
                {
                    var dashboardUrl = context.Configuration["CapturerApi:DashboardUrl"];
                    client.BaseAddress = new Uri(dashboardUrl);
                    client.Timeout = TimeSpan.FromSeconds(30);
                });
                
                // Logging
                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.AddDebug();
                    builder.AddFile("Logs/capturer-{Date}.log");
                });
            });
}
```

### 3. API Service Implementation
```csharp
// Capturer.Api/Services/CapturerApiService.cs
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Capturer.Api.Services;

public class CapturerApiService : BackgroundService
{
    private readonly ILogger<CapturerApiService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private WebApplication? _app;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var apiEnabled = _configuration.GetValue<bool>("CapturerApi:Enabled");
        if (!apiEnabled)
        {
            _logger.LogInformation("Capturer API is disabled");
            return;
        }

        var port = _configuration.GetValue<int>("CapturerApi:Port", 8080);
        
        try
        {
            var builder = WebApplication.CreateBuilder();
            
            // Configure Kestrel
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenLocalhost(port);
            });
            
            // Add services
            builder.Services.AddControllers();
            builder.Services.AddSignalR();
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("DashboardPolicy", policy =>
                {
                    policy.WithOrigins(_configuration["CapturerApi:DashboardUrl"])
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                });
            });
            
            // Add authentication
            builder.Services.AddAuthentication("ApiKey")
                .AddScheme<ApiKeyAuthOptions, ApiKeyAuthHandler>("ApiKey", null);
            
            // Register application services
            builder.Services.AddSingleton(_serviceProvider);
            
            _app = builder.Build();
            
            // Configure pipeline
            _app.UseCors("DashboardPolicy");
            _app.UseAuthentication();
            _app.UseAuthorization();
            
            // Map endpoints
            _app.MapControllers();
            _app.MapHub<ActivityHub>("/hubs/activity");
            _app.MapHealthChecks("/health");
            
            _logger.LogInformation("Starting Capturer API on port {Port}", port);
            
            await _app.RunAsync($"http://localhost:{port}", stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Capturer API");
        }
    }
    
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_app != null)
        {
            await _app.StopAsync(cancellationToken);
            await _app.DisposeAsync();
        }
        await base.StopAsync(cancellationToken);
    }
}
```

### 4. Activity Controller
```csharp
// Capturer.Api/Controllers/ActivityController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Capturer.Core.Services;
using Capturer.Shared.DTOs;

namespace Capturer.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(AuthenticationSchemes = "ApiKey")]
public class ActivityController : ControllerBase
{
    private readonly ActivityReportService _reportService;
    private readonly ScreenshotService _screenshotService;
    private readonly ILogger<ActivityController> _logger;
    
    [HttpGet("current")]
    public async Task<IActionResult> GetCurrentActivity()
    {
        try
        {
            var currentActivity = await _reportService.GetCurrentActivityAsync();
            return Ok(currentActivity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current activity");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
    
    [HttpGet("history")]
    public async Task<IActionResult> GetActivityHistory(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int limit = 100)
    {
        try
        {
            var history = await _reportService.GetHistoryAsync(from, to, limit);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting activity history");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
    
    [HttpPost("capture")]
    public async Task<IActionResult> TriggerCapture()
    {
        try
        {
            await _screenshotService.CaptureScreenshotAsync();
            return Accepted(new 
            { 
                message = "Capture triggered",
                timestamp = DateTime.UtcNow 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering capture");
            return StatusCode(500, new { error = "Capture failed" });
        }
    }
}
```

### 5. Real-time Hub
```csharp
// Capturer.Api/Hubs/ActivityHub.cs
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace Capturer.Api.Hubs;

[Authorize(AuthenticationSchemes = "ApiKey")]
public class ActivityHub : Hub
{
    private readonly ILogger<ActivityHub> _logger;
    
    public override async Task OnConnectedAsync()
    {
        var computerId = Context.UserIdentifier;
        _logger.LogInformation("Dashboard connected for computer {ComputerId}", computerId);
        
        await Groups.AddToGroupAsync(Context.ConnectionId, $"computer-{computerId}");
        await base.OnConnectedAsync();
    }
    
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var computerId = Context.UserIdentifier;
        _logger.LogInformation("Dashboard disconnected for computer {ComputerId}", computerId);
        
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"computer-{computerId}");
        await base.OnDisconnectedAsync(exception);
    }
    
    public async Task SendActivityUpdate(ActivityUpdateDto update)
    {
        await Clients.Group($"computer-{update.ComputerId}")
            .SendAsync("ActivityUpdated", update);
    }
}
```

---

## 🌐 Implementación en Dashboard Web

### 1. Estructura del Proyecto Dashboard
```
CapturerDashboard/
├── src/
│   ├── CapturerDashboard.Web/
│   │   ├── Controllers/
│   │   │   ├── HomeController.cs
│   │   │   ├── ComputersController.cs
│   │   │   └── ReportsController.cs
│   │   ├── Views/
│   │   ├── wwwroot/
│   │   │   ├── js/
│   │   │   │   ├── dashboard.js
│   │   │   │   └── capturer-client.js
│   │   │   └── css/
│   │   └── Program.cs
│   │
│   ├── CapturerDashboard.Api/
│   │   ├── Controllers/
│   │   │   ├── ComputersApiController.cs
│   │   │   └── ReportsApiController.cs
│   │   └── Middleware/
│   │       └── ApiKeyMiddleware.cs
│   │
│   └── CapturerDashboard.Data/
│       ├── Context/
│       │   └── DashboardDbContext.cs
│       └── Repositories/
│           ├── ComputerRepository.cs
│           └── ReportRepository.cs
│
└── docker-compose.yml
```

### 2. API Endpoint para Recibir Reports
```csharp
// CapturerDashboard.Api/Controllers/ReportsApiController.cs
using Microsoft.AspNetCore.Mvc;
using CapturerDashboard.Data.Repositories;
using Capturer.Shared.DTOs;

namespace CapturerDashboard.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsApiController : ControllerBase
{
    private readonly IReportRepository _reportRepository;
    private readonly IComputerRepository _computerRepository;
    private readonly IHubContext<DashboardHub> _hubContext;
    private readonly ILogger<ReportsApiController> _logger;
    
    [HttpPost]
    [ServiceFilter(typeof(ApiKeyAuthFilter))]
    public async Task<IActionResult> ReceiveReport([FromBody] ActivityReportDto report)
    {
        try
        {
            // Validate API key and get computer
            var apiKey = Request.Headers["X-API-Key"].FirstOrDefault();
            var computer = await _computerRepository.GetByApiKeyAsync(apiKey);
            
            if (computer == null)
            {
                return Unauthorized(new { error = "Invalid API key" });
            }
            
            // Update last seen
            computer.LastSeenAt = DateTime.UtcNow;
            await _computerRepository.UpdateAsync(computer);
            
            // Store report
            var storedReport = await _reportRepository.CreateAsync(new ActivityReport
            {
                ComputerId = computer.Id,
                ReportDate = report.ReportDate,
                StartTime = report.StartTime,
                EndTime = report.EndTime,
                ReportData = JsonSerializer.Serialize(report),
                TotalQuadrants = report.Quadrants?.Count ?? 0,
                TotalComparisons = report.Quadrants?.Sum(q => q.TotalComparisons) ?? 0,
                TotalActivities = report.Quadrants?.Sum(q => q.ActivityDetectionCount) ?? 0,
                AverageActivityRate = report.Quadrants?.Average(q => q.ActivityRate) ?? 0
            });
            
            // Notify connected clients via SignalR
            await _hubContext.Clients.Group($"org-{computer.OrganizationId}")
                .SendAsync("ReportReceived", new
                {
                    ComputerId = computer.Id,
                    ComputerName = computer.Name,
                    ReportId = storedReport.Id,
                    Timestamp = DateTime.UtcNow
                });
            
            _logger.LogInformation("Report received from {ComputerName}", computer.Name);
            
            return CreatedAtAction(nameof(GetReport), 
                new { id = storedReport.Id }, 
                new { reportId = storedReport.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing report");
            return StatusCode(500, new { error = "Failed to process report" });
        }
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetReport(Guid id)
    {
        var report = await _reportRepository.GetByIdAsync(id);
        if (report == null)
        {
            return NotFound();
        }
        
        return Ok(report);
    }
}
```

### 3. Dashboard JavaScript Client
```javascript
// wwwroot/js/capturer-client.js
class CapturerDashboardClient {
    constructor() {
        this.connection = null;
        this.charts = {};
        this.computers = new Map();
        this.init();
    }
    
    async init() {
        // Initialize SignalR connection
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl("/hubs/dashboard")
            .withAutomaticReconnect()
            .build();
        
        // Setup event handlers
        this.setupEventHandlers();
        
        // Start connection
        await this.startConnection();
        
        // Load initial data
        await this.loadDashboardData();
        
        // Initialize charts
        this.initializeCharts();
        
        // Start periodic refresh
        this.startPeriodicRefresh();
    }
    
    setupEventHandlers() {
        // Real-time report received
        this.connection.on("ReportReceived", (data) => {
            console.log("New report received:", data);
            this.updateComputerStatus(data.ComputerId, 'online');
            this.incrementCounter('todayReports');
            this.showNotification(`New report from ${data.ComputerName}`);
        });
        
        // Computer status changed
        this.connection.on("ComputerStatusChanged", (data) => {
            this.updateComputerStatus(data.ComputerId, data.Status);
        });
        
        // Alert triggered
        this.connection.on("AlertTriggered", (alert) => {
            this.showAlert(alert);
        });
    }
    
    async startConnection() {
        try {
            await this.connection.start();
            console.log("SignalR connected");
            
            // Join organization group
            const orgId = document.querySelector('#orgId').value;
            await this.connection.invoke("JoinOrganizationGroup", orgId);
        } catch (err) {
            console.error("SignalR connection error:", err);
            setTimeout(() => this.startConnection(), 5000);
        }
    }
    
    async loadDashboardData() {
        try {
            // Fetch dashboard overview
            const response = await fetch('/api/dashboard/overview');
            const data = await response.json();
            
            // Update UI
            this.updateDashboardMetrics(data);
            
            // Load computers
            const computersResponse = await fetch('/api/computers');
            const computers = await computersResponse.json();
            
            computers.forEach(computer => {
                this.computers.set(computer.id, computer);
                this.addComputerToTable(computer);
            });
        } catch (error) {
            console.error("Failed to load dashboard data:", error);
        }
    }
    
    initializeCharts() {
        // Activity Timeline Chart
        const timelineCtx = document.getElementById('activityTimelineChart').getContext('2d');
        this.charts.timeline = new Chart(timelineCtx, {
            type: 'line',
            data: {
                labels: [],
                datasets: [{
                    label: 'Average Activity %',
                    data: [],
                    borderColor: '#007bff',
                    backgroundColor: 'rgba(0, 123, 255, 0.1)',
                    tension: 0.4
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                scales: {
                    y: {
                        beginAtZero: true,
                        max: 100
                    }
                },
                plugins: {
                    streaming: {
                        duration: 20000,
                        refresh: 1000,
                        delay: 2000,
                        onRefresh: (chart) => {
                            // Add new data point
                            this.fetchLatestActivity().then(activity => {
                                chart.data.datasets[0].data.push({
                                    x: Date.now(),
                                    y: activity
                                });
                            });
                        }
                    }
                }
            }
        });
        
        // Computer Status Donut Chart
        const statusCtx = document.getElementById('computerStatusChart').getContext('2d');
        this.charts.status = new Chart(statusCtx, {
            type: 'doughnut',
            data: {
                labels: ['Online', 'Offline', 'Warning'],
                datasets: [{
                    data: [0, 0, 0],
                    backgroundColor: ['#28a745', '#6c757d', '#ffc107']
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false
            }
        });
    }
    
    updateComputerStatus(computerId, status) {
        const computer = this.computers.get(computerId);
        if (computer) {
            computer.status = status;
            
            // Update table row
            const row = document.querySelector(`tr[data-computer-id="${computerId}"]`);
            if (row) {
                const statusCell = row.querySelector('.status-cell');
                statusCell.innerHTML = this.getStatusBadge(status);
            }
            
            // Update status chart
            this.updateStatusChart();
        }
    }
    
    updateStatusChart() {
        const online = Array.from(this.computers.values())
            .filter(c => c.status === 'online').length;
        const offline = Array.from(this.computers.values())
            .filter(c => c.status === 'offline').length;
        const warning = Array.from(this.computers.values())
            .filter(c => c.status === 'warning').length;
        
        this.charts.status.data.datasets[0].data = [online, offline, warning];
        this.charts.status.update();
    }
    
    getStatusBadge(status) {
        const badges = {
            'online': '<span class="badge bg-success">🟢 Online</span>',
            'offline': '<span class="badge bg-secondary">🔴 Offline</span>',
            'warning': '<span class="badge bg-warning">⚠️ Warning</span>'
        };
        return badges[status] || badges['offline'];
    }
    
    showNotification(message) {
        // Check if browser supports notifications
        if ("Notification" in window && Notification.permission === "granted") {
            new Notification("Capturer Dashboard", {
                body: message,
                icon: "/images/icon-192.png"
            });
        }
        
        // Also show in-app notification
        const toast = document.createElement('div');
        toast.className = 'toast show';
        toast.innerHTML = `
            <div class="toast-body">
                ${message}
                <button type="button" class="btn-close" data-bs-dismiss="toast"></button>
            </div>
        `;
        document.getElementById('toastContainer').appendChild(toast);
        
        setTimeout(() => toast.remove(), 5000);
    }
    
    async fetchLatestActivity() {
        try {
            const response = await fetch('/api/dashboard/current-activity');
            const data = await response.json();
            return data.averageActivity;
        } catch (error) {
            console.error("Failed to fetch activity:", error);
            return 0;
        }
    }
    
    startPeriodicRefresh() {
        // Refresh dashboard every 30 seconds
        setInterval(() => {
            this.loadDashboardData();
        }, 30000);
        
        // Update relative times every minute
        setInterval(() => {
            this.updateRelativeTimes();
        }, 60000);
    }
}

// Initialize dashboard when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    window.dashboardClient = new CapturerDashboardClient();
});
```

---

## 🔒 Seguridad de la Integración

### 1. Autenticación por API Key
```csharp
// Dashboard: ApiKeyAuthFilter.cs
public class ApiKeyAuthFilter : IAuthorizationFilter
{
    private readonly IComputerRepository _computerRepository;
    
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var apiKey = context.HttpContext.Request.Headers["X-API-Key"].FirstOrDefault();
        
        if (string.IsNullOrEmpty(apiKey))
        {
            context.Result = new UnauthorizedResult();
            return;
        }
        
        var computer = _computerRepository.GetByApiKeyAsync(apiKey).Result;
        
        if (computer == null || !computer.IsActive)
        {
            context.Result = new UnauthorizedResult();
            return;
        }
        
        // Add computer info to context
        context.HttpContext.Items["Computer"] = computer;
    }
}
```

### 2. Encriptación de Comunicación
```yaml
# nginx.conf para Dashboard
server {
    listen 443 ssl http2;
    server_name dashboard.empresa.com;
    
    ssl_certificate /etc/ssl/certs/dashboard.crt;
    ssl_certificate_key /etc/ssl/private/dashboard.key;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;
    
    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
    
    location /hubs/ {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
    }
}
```

### 3. Rate Limiting
```csharp
// RateLimitMiddleware.cs
public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    
    public async Task InvokeAsync(HttpContext context)
    {
        var apiKey = context.Request.Headers["X-API-Key"].FirstOrDefault();
        if (string.IsNullOrEmpty(apiKey))
        {
            await _next(context);
            return;
        }
        
        var key = $"rate_limit_{apiKey}";
        var requestCount = _cache.Get<int>(key);
        
        if (requestCount >= 100) // 100 requests per minute
        {
            context.Response.StatusCode = 429; // Too Many Requests
            await context.Response.WriteAsync("Rate limit exceeded");
            return;
        }
        
        _cache.Set(key, requestCount + 1, TimeSpan.FromMinutes(1));
        await _next(context);
    }
}
```

---

## 🧪 Testing de Integración

### 1. Test de Registro de Computadora
```csharp
[TestClass]
public class ComputerRegistrationTests
{
    private TestServer _server;
    private HttpClient _client;
    
    [TestInitialize]
    public void Setup()
    {
        var builder = new WebHostBuilder()
            .UseStartup<TestStartup>();
        _server = new TestServer(builder);
        _client = _server.CreateClient();
    }
    
    [TestMethod]
    public async Task Should_Register_New_Computer_Successfully()
    {
        // Arrange
        var request = new
        {
            computerId = "TEST-ID-123",
            computerName = "TEST-PC",
            organizationCode = "TEST-ORG"
        };
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/computers/register", request);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<RegisterResponse>(content);
        
        // Assert
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        Assert.IsNotNull(result.ApiKey);
        Assert.IsTrue(result.ApiKey.StartsWith("cap_"));
    }
}
```

### 2. Test de Sincronización de Reports
```csharp
[TestMethod]
public async Task Should_Sync_Activity_Report_Successfully()
{
    // Arrange
    var apiKey = "cap_test_key_123";
    var report = new ActivityReportDto
    {
        ComputerId = "TEST-ID",
        ComputerName = "TEST-PC",
        ReportDate = DateTime.Today,
        StartTime = TimeSpan.FromHours(8),
        EndTime = TimeSpan.FromHours(18),
        Quadrants = new List<QuadrantActivityDto>
        {
            new QuadrantActivityDto
            {
                QuadrantName = "Work",
                TotalComparisons = 1000,
                ActivityDetectionCount = 800,
                ActivityRate = 80.0
            }
        }
    };
    
    _client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
    
    // Act
    var response = await _client.PostAsJsonAsync("/api/reports", report);
    
    // Assert
    Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
    
    // Verify report was stored
    var location = response.Headers.Location;
    var getResponse = await _client.GetAsync(location);
    Assert.AreEqual(HttpStatusCode.OK, getResponse.StatusCode);
}
```

### 3. Test End-to-End
```javascript
// Playwright E2E Test
const { test, expect } = require('@playwright/test');

test('Complete integration flow', async ({ page }) => {
    // 1. Login to dashboard
    await page.goto('https://dashboard.local');
    await page.fill('#email', 'admin@test.com');
    await page.fill('#password', 'TestPassword123');
    await page.click('button[type="submit"]');
    
    // 2. Verify dashboard loads
    await expect(page.locator('h1')).toContainText('Dashboard');
    
    // 3. Simulate Capturer sending report
    const response = await page.request.post('/api/reports', {
        headers: {
            'X-API-Key': 'cap_test_key_123',
            'Content-Type': 'application/json'
        },
        data: {
            computerId: 'TEST-PC-01',
            computerName: 'Test Computer',
            reportDate: new Date().toISOString(),
            // ... report data
        }
    });
    
    expect(response.status()).toBe(201);
    
    // 4. Verify report appears in dashboard
    await page.waitForTimeout(2000); // Wait for SignalR update
    await expect(page.locator('text=Test Computer')).toBeVisible();
    
    // 5. Check activity updated
    const activityElement = await page.locator('#todayReports');
    const count = await activityElement.textContent();
    expect(parseInt(count)).toBeGreaterThan(0);
});
```

---

## 🚀 Deployment de la Integración

### 1. Docker Compose para Desarrollo
```yaml
version: '3.8'

services:
  # Dashboard Web
  dashboard:
    build: ./CapturerDashboard
    container_name: capturer-dashboard
    ports:
      - "5000:80"
      - "5001:443"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Host=postgres;Database=capturer;Username=capturer;Password=dev123
    depends_on:
      - postgres
      - redis
    networks:
      - capturer-net

  # Database
  postgres:
    image: postgres:16-alpine
    container_name: capturer-postgres
    environment:
      - POSTGRES_DB=capturer
      - POSTGRES_USER=capturer
      - POSTGRES_PASSWORD=dev123
    ports:
      - "5432:5432"
    volumes:
      - postgres-data:/var/lib/postgresql/data
    networks:
      - capturer-net

  # Cache
  redis:
    image: redis:7-alpine
    container_name: capturer-redis
    ports:
      - "6379:6379"
    networks:
      - capturer-net

  # Mock Capturer for testing
  capturer-mock:
    build: ./CapturerMock
    container_name: capturer-mock
    environment:
      - DASHBOARD_URL=http://dashboard
      - API_KEY=cap_mock_key_123
    depends_on:
      - dashboard
    networks:
      - capturer-net

volumes:
  postgres-data:

networks:
  capturer-net:
    driver: bridge
```

### 2. Script de Configuración Inicial
```bash
#!/bin/bash
# setup-integration.sh

echo "🚀 Setting up Capturer-Dashboard Integration"

# 1. Check prerequisites
command -v docker >/dev/null 2>&1 || { echo "Docker required"; exit 1; }
command -v dotnet >/dev/null 2>&1 || { echo ".NET SDK required"; exit 1; }

# 2. Build Dashboard
echo "📦 Building Dashboard..."
cd CapturerDashboard
dotnet build -c Release
docker build -t capturer-dashboard:latest .

# 3. Start services
echo "🐳 Starting Docker services..."
docker-compose up -d

# 4. Wait for services
echo "⏳ Waiting for services to start..."
sleep 10

# 5. Run migrations
echo "🗄️ Running database migrations..."
dotnet ef database update --project src/CapturerDashboard.Data

# 6. Create test organization
echo "🏢 Creating test organization..."
curl -X POST http://localhost:5000/api/organizations \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test Organization",
    "slug": "test-org"
  }'

# 7. Register test computer
echo "💻 Registering test computer..."
RESPONSE=$(curl -X POST http://localhost:5000/api/computers/register \
  -H "Content-Type: application/json" \
  -d '{
    "computerId": "TEST-HARDWARE-ID",
    "computerName": "TEST-PC-01",
    "organizationCode": "TEST-ORG"
  }')

API_KEY=$(echo $RESPONSE | jq -r '.apiKey')
echo "✅ API Key generated: $API_KEY"

# 8. Update Capturer config
echo "📝 Updating Capturer configuration..."
cat > ../Capturer/appsettings.Development.json <<EOF
{
  "CapturerApi": {
    "Enabled": true,
    "Port": 8080,
    "DashboardUrl": "http://localhost:5000",
    "ApiKey": "$API_KEY"
  }
}
EOF

echo "✨ Integration setup complete!"
echo "Dashboard: http://localhost:5000"
echo "API Key: $API_KEY"
```

---

## 📊 Monitoreo de la Integración

### 1. Health Checks
```csharp
// HealthCheckService.cs
public class IntegrationHealthCheck : IHealthCheck
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var dashboardUrl = _configuration["CapturerApi:DashboardUrl"];
            var response = await _httpClient.GetAsync($"{dashboardUrl}/health", cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy("Dashboard is reachable");
            }
            
            return HealthCheckResult.Unhealthy($"Dashboard returned {response.StatusCode}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Dashboard is unreachable", ex);
        }
    }
}
```

### 2. Métricas de Integración
```csharp
// MetricsService.cs
public class IntegrationMetrics
{
    private readonly IMetrics _metrics;
    
    public void RecordSyncAttempt(bool success, double duration)
    {
        _metrics.Measure.Counter.Increment(
            "capturer.sync.attempts",
            success ? "success" : "failure");
        
        _metrics.Measure.Timer.Time(
            "capturer.sync.duration",
            TimeUnit.Milliseconds,
            (long)duration);
    }
    
    public void RecordApiCall(string endpoint, int statusCode)
    {
        _metrics.Measure.Counter.Increment(
            "capturer.api.calls",
            new MetricTags("endpoint", endpoint, "status", statusCode.ToString()));
    }
}
```

---

## 🆘 Troubleshooting

### Problemas Comunes

#### 1. Capturer no puede conectar con Dashboard
```bash
# Verificar conectividad
curl -X GET http://dashboard.local/health

# Verificar API Key
curl -X GET http://dashboard.local/api/status \
  -H "X-API-Key: cap_your_key_here"

# Verificar logs de Capturer
tail -f Logs/capturer-*.log | grep "API"
```

#### 2. Reports no se sincronizan
```csharp
// Enable debug logging
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Capturer.Api": "Trace"
    }
  }
}
```

#### 3. SignalR no conecta
```javascript
// Enable SignalR debug logging
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/dashboard")
    .configureLogging(signalR.LogLevel.Debug)
    .build();
```

---

## ✅ Checklist de Integración

### Capturer Side
- [ ] API Service configurado y corriendo
- [ ] API Key almacenada en configuración
- [ ] Dashboard URL correcto
- [ ] Sync service funcionando
- [ ] Health checks pasando
- [ ] Logs sin errores

### Dashboard Side
- [ ] Computer registrado
- [ ] API Key validada
- [ ] Reports almacenándose
- [ ] SignalR conectado
- [ ] UI actualizándose
- [ ] Alertas funcionando

### Network
- [ ] Conectividad entre sistemas
- [ ] Firewall rules configuradas
- [ ] SSL/TLS si es producción
- [ ] DNS resolviendo correctamente

---

## 📚 Referencias Adicionales

1. [ASP.NET Core Web API](https://docs.microsoft.com/aspnet/core/web-api)
2. [SignalR Documentation](https://docs.microsoft.com/aspnet/core/signalr)
3. [Entity Framework Core](https://docs.microsoft.com/ef/core)
4. [Docker Compose](https://docs.docker.com/compose)
5. [API Security Best Practices](https://owasp.org/www-project-api-security)

---

**Versión**: 1.0.0  
**Última Actualización**: 2024-01-20  
**Autor**: Technical Documentation Team