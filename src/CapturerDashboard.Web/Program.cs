using Microsoft.EntityFrameworkCore;
using CapturerDashboard.Data.Context;
using CapturerDashboard.Web.Hubs;
using Azure.Identity;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.AspNetCore.Authentication.Cookies;
using CapturerDashboard.Core.Models;

var builder = WebApplication.CreateBuilder(args);

// =============================================================================
// AZURE CONFIGURATION
// =============================================================================

// Configure Azure Key Vault (if in production)
if (builder.Environment.IsProduction())
{
    var keyVaultName = builder.Configuration["KeyVaultName"];
    if (!string.IsNullOrEmpty(keyVaultName))
    {
        var keyVaultUri = new Uri($"https://{keyVaultName}.vault.azure.net/");
        builder.Configuration.AddAzureKeyVault(keyVaultUri, new DefaultAzureCredential());
    }
}

// Add Application Insights
if (builder.Configuration.GetValue<bool>("Features:UseApplicationInsights", false))
{
    builder.Services.AddApplicationInsightsTelemetry(new ApplicationInsightsServiceOptions
    {
        ConnectionString = builder.Configuration["Azure:ApplicationInsights:ConnectionString"]
    });
}

// =============================================================================
// CORE SERVICES
// =============================================================================

// Add services to the container
builder.Services.AddControllersWithViews();

// Register application services
builder.Services.AddScoped<CapturerDashboard.Web.Services.IComputerInvitationService, CapturerDashboard.Web.Services.ComputerInvitationService>();
builder.Services.AddScoped<CapturerDashboard.Web.Services.ICapturerClientService, CapturerDashboard.Web.Services.CapturerClientService>();

// Register HttpClient for Capturer API communication
builder.Services.AddHttpClient<CapturerDashboard.Web.Services.ICapturerClientService, CapturerDashboard.Web.Services.CapturerClientService>();

// Configure Authentication
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/auth/login";
        options.LogoutPath = "/auth/logout";
        options.AccessDeniedPath = "/auth/access-denied";
        options.Cookie.Name = "CapturerDashboard.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = builder.Environment.IsProduction() 
            ? CookieSecurePolicy.Always 
            : CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

// Configure SignalR (Azure SignalR Service or local)
var useAzureSignalR = builder.Configuration.GetValue<bool>("Features:UseAzureSignalR", false);
var signalRBuilder = builder.Services.AddSignalR();

if (useAzureSignalR && !string.IsNullOrEmpty(builder.Configuration["Azure:SignalR:ConnectionString"]))
{
    signalRBuilder.AddAzureSignalR(builder.Configuration["Azure:SignalR:ConnectionString"]);
}

// Configure Entity Framework (SQL Server for production, SQLite for development)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var useSqlServer = !string.IsNullOrEmpty(connectionString) && 
                   (connectionString.Contains("database.windows.net") || 
                    connectionString.Contains("Server=") || 
                    connectionString.Contains("localdb"));

if (useSqlServer)
{
    // SQL Server (Azure SQL or LocalDB)
    builder.Services.AddDbContext<DashboardDbContext>(options =>
        options.UseSqlServer(connectionString,
            sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
            }));
}
else
{
    // SQLite for development
    builder.Services.AddDbContext<DashboardDbContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") 
            ?? "Data Source=capturer-dashboard.db"));
}

// Configure CORS
var enableCors = builder.Configuration.GetValue<bool>("Security:EnableCors", true);
if (enableCors)
{
    var allowedOrigins = builder.Configuration.GetSection("Security:AllowedOrigins").Get<string[]>() ?? [];
    
    builder.Services.AddCors(options =>
    {
        if (builder.Environment.IsDevelopment())
        {
            options.AddPolicy("AllowAll", policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        }
        else
        {
            options.AddPolicy("Production", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });
        }
    });
}

// Add Swagger for API documentation (development only)
if (builder.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("Features:EnableSwagger", false))
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "Capturer Dashboard API", Version = "v1" });
        c.AddSecurityDefinition("ApiKey", new()
        {
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Name = "X-API-Key",
            Description = "API Key for Capturer clients"
        });
    });
}

// =============================================================================
// HEALTH CHECKS & MONITORING
// =============================================================================

// Add health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<DashboardDbContext>("database")
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());

var app = builder.Build();

// =============================================================================
// MIDDLEWARE PIPELINE
// =============================================================================

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    
    // Enable Swagger in development
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Capturer Dashboard API V1");
        c.RoutePrefix = "api/docs";
    });
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    
    // Enable Swagger in production if configured
    if (builder.Configuration.GetValue<bool>("Features:EnableSwagger", false))
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Capturer Dashboard API V1");
            c.RoutePrefix = "api/docs";
        });
    }
}

// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    
    if (app.Environment.IsProduction())
    {
        context.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
    }
    
    await next();
});

// Database initialization
await InitializeDatabaseAsync(app);

// Configure HTTPS redirection
if (builder.Configuration.GetValue<bool>("Security:RequireHttps", true))
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

// Configure CORS
if (enableCors)
{
    var corsPolicy = app.Environment.IsDevelopment() ? "AllowAll" : "Production";
    app.UseCors(corsPolicy);
}

app.UseRouting();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Health checks endpoint
app.MapHealthChecks("/health");

app.MapControllers();
app.MapHub<DashboardHub>("/dashboardHub");

// Default route for SPA
app.MapFallbackToFile("index.html");

// =============================================================================
// APPLICATION STARTUP
// =============================================================================

// Log startup information
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("🚀 Capturer Dashboard starting up...");
logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);
logger.LogInformation("Using SQL Server: {UseSqlServer}", useSqlServer);
logger.LogInformation("Using Azure SignalR: {UseAzureSignalR}", useAzureSignalR);

app.Run();

// =============================================================================
// HELPER METHODS
// =============================================================================

static async Task InitializeDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<DashboardDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("🗄️ Initializing database...");
        
        if (app.Environment.IsDevelopment())
        {
            // En desarrollo, crear la base de datos si no existe
            await context.Database.EnsureCreatedAsync();
        }
        else
        {
            // En producción, aplicar migraciones pendientes
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                logger.LogInformation("Applying {Count} pending migrations", pendingMigrations.Count());
                await context.Database.MigrateAsync();
            }
        }
        
        // Ensure admin user exists
        await EnsureAdminUserExistsAsync(context, logger);
        
        logger.LogInformation("✅ Database initialization completed");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Database initialization failed");
        throw;
    }
}

static async Task EnsureAdminUserExistsAsync(DashboardDbContext context, ILogger logger)
{
    // Check if default organization exists
    var defaultOrg = await context.Organizations.FirstOrDefaultAsync(o => o.Slug == "default");
    if (defaultOrg == null)
    {
        defaultOrg = new CapturerDashboard.Core.Models.Organization
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            Name = "Default Organization",
            Slug = "default",
            MaxComputers = 10,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        context.Organizations.Add(defaultOrg);
        await context.SaveChangesAsync();
        logger.LogInformation("✅ Default organization created");
    }

    // Check if admin user exists
    var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "admin@capturer.local");
    if (adminUser == null)
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("admin123");
        adminUser = new CapturerDashboard.Core.Models.User
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
            Email = "admin@capturer.local",
            FirstName = "System",
            LastName = "Administrator",
            PasswordHash = passwordHash,
            Role = "Admin",
            OrganizationId = defaultOrg.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(adminUser);
        await context.SaveChangesAsync();
        logger.LogInformation("✅ Admin user created with email: {Email}", adminUser.Email);
    }
    else
    {
        logger.LogInformation("ℹ️ Admin user already exists: {Email}", adminUser.Email);
    }
}
