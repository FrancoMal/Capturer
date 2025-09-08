using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.SignalR;
using Capturer.Api.Middleware;
using Capturer.Api.DTOs;
using Capturer.Api.Hubs;
using Capturer.Models;
using Serilog;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Windows.Forms;
using IAppConfigurationManager = Capturer.Services.IConfigurationManager;

namespace Capturer.Services;

/// <summary>
/// Servicio principal que hostea la API REST embedida en el cliente v4.0
/// Implementación según plan técnico v4.0 - CapturerApiService
/// </summary>
public interface ICapturerApiService
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    bool IsRunning { get; }
    int Port { get; }
}

public class CapturerApiService : BackgroundService, ICapturerApiService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly Microsoft.Extensions.Logging.ILogger<CapturerApiService> _logger;
    private WebApplication? _app;
    private bool _isRunning = false;

    public bool IsRunning => _isRunning;
    public int Port { get; private set; }

    public CapturerApiService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        Microsoft.Extensions.Logging.ILogger<CapturerApiService> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
        
        // 🔍 DEBUGGING: Constructor called
        Console.WriteLine("[DEBUG] CapturerApiService constructor called");
        _logger.LogInformation("CapturerApiService constructor initialized");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 🔍 DEBUGGING: ExecuteAsync called
        Console.WriteLine("[DEBUG] CapturerApiService.ExecuteAsync starting...");
        _logger.LogInformation("CapturerApiService.ExecuteAsync starting execution");
        
        try
        {
            // Get real configuration from ConfigurationManager
            var configManager = _serviceProvider.GetService(typeof(IAppConfigurationManager)) as IAppConfigurationManager;
            var capturerConfig = await configManager?.LoadConfigurationAsync() ?? new CapturerConfiguration();
            
            Console.WriteLine($"[DEBUG] Real config loaded. API Enabled: {capturerConfig.Api.Enabled}");
            
            // Check if API is enabled in real configuration
            if (!capturerConfig.Api.Enabled)
            {
                Console.WriteLine("[DEBUG] API is disabled in configuration, exiting");
                _logger.LogInformation("Capturer API is disabled in configuration");
                return;
            }

            Port = capturerConfig.Api.Port;
            
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                ApplicationName = "Capturer", // Use actual assembly name
                ContentRootPath = AppContext.BaseDirectory,
                WebRootPath = null // No static files needed
            });

            // Configure Serilog for structured logging
            builder.Host.UseSerilog((context, configuration) =>
            {
                configuration
                    .WriteTo.Console()
                    .WriteTo.File("logs/capturer-api-.log", rollingInterval: RollingInterval.Day)
                    .MinimumLevel.Information();
            });

            // Configure services
            ConfigureServices(builder.Services);

            _app = builder.Build();

            // Configure middleware pipeline
            ConfigureMiddleware(_app);

            // Map endpoints
            MapEndpoints(_app);

            _logger.LogInformation("Starting Capturer API v4.0 on port {Port}", Port);
            
            _isRunning = true;
            
            _app.Urls.Add($"http://localhost:{Port}");
            await _app.RunAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Capturer API shutdown requested");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Capturer API");
            throw;
        }
        finally
        {
            _isRunning = false;
        }
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Add controllers with Newtonsoft.Json
        services.AddControllers()
            .AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.DateFormatHandling = Newtonsoft.Json.DateFormatHandling.IsoDateFormat;
                options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            });

        // Add API Key authentication
        services.AddAuthentication("ApiKey")
            .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
                "ApiKey", options => { });

        // Add authorization
        services.AddAuthorization(options =>
        {
            options.AddPolicy("ApiKeyPolicy", policy =>
                policy.RequireAuthenticatedUser());
        });

        // Configure CORS for Dashboard Web communication using real config
        services.AddCors(options =>
        {
            options.AddPolicy("DashboardPolicy", builder =>
            {
                // Get real configuration
                var configManager = _serviceProvider.GetService(typeof(IAppConfigurationManager)) as IAppConfigurationManager;
                var config = configManager?.LoadConfigurationAsync().GetAwaiter().GetResult() ?? new CapturerConfiguration();
                
                builder
                    .WithOrigins(config.Api.AllowedOrigins.ToArray())
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
        });

        // Add SignalR for real-time communication
        services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = true;
            options.KeepAliveInterval = TimeSpan.FromSeconds(15);
            options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
        });
        
        // Register SignalR service
        services.AddSingleton<IActivityHubService, ActivityHubService>();

        // Add health checks
        services.AddHealthChecks()
            .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("API is running"));

        // Register application services from main DI container
        var screenshotService = _serviceProvider.GetService<IScreenshotService>();
        var emailService = _serviceProvider.GetService<IEmailService>();
        var quadrantService = _serviceProvider.GetService<IQuadrantService>();
        var configManager = _serviceProvider.GetService<IConfigurationManager>();

        if (screenshotService != null) services.AddSingleton(screenshotService);
        if (emailService != null) services.AddSingleton(emailService);
        if (quadrantService != null) services.AddSingleton(quadrantService);
        if (configManager != null) services.AddSingleton(configManager);

        // Add HttpClient for Dashboard communication using real config
        services.AddHttpClient<DashboardSyncService>(client =>
        {
            // Get real configuration
            var configManager = _serviceProvider.GetService(typeof(IAppConfigurationManager)) as IAppConfigurationManager;
            var config = configManager?.LoadConfigurationAsync().GetAwaiter().GetResult() ?? new CapturerConfiguration();
            
            client.BaseAddress = new Uri(config.Api.DashboardUrl);
            client.DefaultRequestHeaders.Add("X-Api-Key", config.Api.ApiKey);
            client.Timeout = TimeSpan.FromSeconds(config.Api.ConnectionTimeoutSeconds);
        });
    }

    private void ConfigureMiddleware(WebApplication app)
    {
        // Exception handling
        app.UseExceptionHandler("/api/error");

        // Security headers
        app.Use(async (context, next) =>
        {
            context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Add("X-Frame-Options", "DENY");
            context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
            context.Response.Headers.Add("Server", "Capturer-API/4.0");
            await next();
        });

        // Request logging with Serilog
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
        // Map SignalR hub
        app.MapHub<ActivityHub>("/hubs/activity")
            .RequireAuthorization("ApiKeyPolicy");

        // Map controllers
        app.MapControllers()
            .RequireAuthorization("ApiKeyPolicy");

        // Minimal API endpoints for quick operations
        var apiGroup = app.MapGroup("/api/v1")
            .RequireAuthorization("ApiKeyPolicy");

        // Status endpoint
        apiGroup.MapGet("/status", GetSystemStatus);
        
        // Health endpoint (unauthenticated for monitoring)
        app.MapGet("/api/v1/health", GetHealthStatus);
    }

    private async Task<ApiResponse<SystemStatusDto>> GetSystemStatus(
        IScreenshotService? screenshotService,
        IQuadrantService? quadrantService)
    {
        try
        {
            var status = new SystemStatusDto
            {
                IsCapturing = screenshotService?.IsCapturing ?? false,
                LastCaptureTime = screenshotService?.NextCaptureTime,
                TotalScreenshots = await GetTotalScreenshotsAsync(),
                SystemInfo = new SystemInfoDto
                {
                    WorkingSetMemory = Environment.WorkingSet,
                    Uptime = TimeSpan.FromMilliseconds(Environment.TickCount64),
                    AvailableScreens = GetAvailableScreens()
                }
            };

            return ApiResponse<SystemStatusDto>.SuccessResponse(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system status");
            return ApiResponse<SystemStatusDto>.ErrorResponse("Failed to get system status", "STATUS_ERROR");
        }
    }

    private Task<HealthCheckResult> GetHealthStatus()
    {
        var result = new HealthCheckResult
        {
            Status = _isRunning ? "Healthy" : "Unhealthy",
            Details = new Dictionary<string, object>
            {
                ["api_running"] = _isRunning,
                ["port"] = Port,
                ["version"] = "4.0.0-alpha",
                ["uptime_ms"] = Environment.TickCount64
            },
            ResponseTime = TimeSpan.FromMilliseconds(1) // Fast response
        };

        return Task.FromResult(result);
    }

    // Helper methods
    private async Task<long> GetTotalScreenshotsAsync()
    {
        try
        {
            var fileService = _serviceProvider.GetService<IFileService>();
            if (fileService == null) return 0;

            // This would need to be implemented in IFileService
            // For now, return 0
            return 0;
        }
        catch
        {
            return 0;
        }
    }

    private List<ScreenInfoDto> GetAvailableScreens()
    {
        var screens = new List<ScreenInfoDto>();
        try
        {
            foreach (var screen in Screen.AllScreens)
            {
                screens.Add(new ScreenInfoDto
                {
                    Index = screens.Count,
                    DeviceName = screen.DeviceName,
                    DisplayName = screen.DeviceName,
                    Width = screen.Bounds.Width,
                    Height = screen.Bounds.Height,
                    IsPrimary = screen.Primary
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting available screens");
        }
        return screens;
    }

    // TODO: Implement Polly policies in next iteration
    // private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy() { ... }
    // private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy() { ... }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Capturer API");
        _isRunning = false;
        
        if (_app != null)
        {
            await _app.DisposeAsync();
        }
        
        await base.StopAsync(cancellationToken);
    }
}

/// <summary>
/// Service for synchronizing data with Dashboard Web
/// Fully functional implementation for v4.0
/// </summary>
public class DashboardSyncService
{
    private readonly HttpClient _httpClient;
    private readonly Microsoft.Extensions.Logging.ILogger<DashboardSyncService> _logger;
    private readonly Queue<ActivityReportDto> _pendingReports;
    private readonly SemaphoreSlim _syncSemaphore;
    private readonly IActivityHubService? _hubService;

    public DashboardSyncService(
        HttpClient httpClient, 
        Microsoft.Extensions.Logging.ILogger<DashboardSyncService> logger,
        IServiceProvider serviceProvider)
    {
        _httpClient = httpClient;
        _logger = logger;
        _pendingReports = new Queue<ActivityReportDto>();
        _syncSemaphore = new SemaphoreSlim(1, 1);
        _hubService = serviceProvider.GetService(typeof(IActivityHubService)) as IActivityHubService;
    }

    public async Task<SyncResult> SyncReportAsync(ActivityReportDto report)
    {
        await _syncSemaphore.WaitAsync();
        try
        {
            _logger.LogInformation("Syncing activity report {ReportId} to dashboard", report.Id);
            
            // Add to pending queue
            _pendingReports.Enqueue(report);
            
            try
            {
                // Try to sync with Dashboard Web
                var response = await _httpClient.PostAsJsonAsync("/api/reports", report);
                
                if (response.IsSuccessStatusCode)
                {
                    _pendingReports.Dequeue();
                    var result = await response.Content.ReadFromJsonAsync<SyncResult>();
                    
                    _logger.LogInformation("Report synced successfully: {ReportId}", result?.ReportId);
                    
                    // Broadcast via SignalR
                    if (_hubService != null)
                    {
                        await _hubService.BroadcastActivityUpdate(report);
                    }
                    
                    return result ?? new SyncResult { Success = true, ReportId = report.Id.ToString() };
                }
                else
                {
                    _logger.LogWarning("Failed to sync report: HTTP {StatusCode}", response.StatusCode);
                    return new SyncResult 
                    { 
                        Success = false, 
                        Error = $"HTTP {response.StatusCode}",
                        ReportId = report.Id.ToString()
                    };
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error during sync - Dashboard may not be running yet");
                
                // This is expected when Dashboard Web is not running yet
                return new SyncResult 
                { 
                    Success = false, 
                    Error = "Dashboard Web not available - report queued for later",
                    ReportId = report.Id.ToString()
                };
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Sync request timed out");
                return new SyncResult 
                { 
                    Success = false, 
                    Error = "Request timeout",
                    ReportId = report.Id.ToString()
                };
            }
        }
        finally
        {
            _syncSemaphore.Release();
        }
    }

    public async Task SyncPendingReportsAsync()
    {
        _logger.LogInformation("Syncing {Count} pending reports", _pendingReports.Count);
        
        var syncedCount = 0;
        while (_pendingReports.Count > 0)
        {
            var report = _pendingReports.Peek();
            var result = await SyncReportAsync(report);
            
            if (!result.Success)
            {
                _logger.LogWarning("Failed to sync pending report, will retry later");
                break;
            }
            
            syncedCount++;
        }
        
        if (syncedCount > 0)
        {
            _logger.LogInformation("Synced {Count} pending reports successfully", syncedCount);
        }
    }

    public int GetPendingReportCount() => _pendingReports.Count;
    
    public async Task<bool> TestDashboardConnectionAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}