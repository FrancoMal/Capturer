using Azure.Identity;
using Microsoft.AspNetCore.SignalR;

namespace CapturerDashboard.Web.Extensions;

public static class AzureServiceExtensions
{
    /// <summary>
    /// Configure Azure services based on environment and configuration
    /// </summary>
    public static IServiceCollection AddAzureServices(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        // Azure Key Vault Configuration
        if (environment.IsProduction())
        {
            var keyVaultName = configuration["KeyVaultName"];
            if (!string.IsNullOrEmpty(keyVaultName))
            {
                services.AddSingleton<IAzureKeyVaultService, AzureKeyVaultService>();
            }
        }

        // Azure SignalR Service
        var useAzureSignalR = configuration.GetValue<bool>("Features:UseAzureSignalR", false);
        if (useAzureSignalR)
        {
            var connectionString = configuration["Azure:SignalR:ConnectionString"];
            if (!string.IsNullOrEmpty(connectionString))
            {
                services.AddSignalR(options =>
                {
                    // Configure SignalR options for Azure
                    options.EnableDetailedErrors = environment.IsDevelopment();
                    options.MaximumReceiveMessageSize = 32 * 1024; // 32KB
                    options.StreamBufferCapacity = 10;
                    options.MaximumParallelInvocationsPerClient = 2;
                })
                .AddAzureSignalR(options =>
                {
                    options.ConnectionString = connectionString;
                    options.ServerStickyMode = Microsoft.Azure.SignalR.ServerStickyMode.Required;
                });
            }
        }
        else
        {
            services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = environment.IsDevelopment();
            });
        }

        return services;
    }
}

/// <summary>
/// Interface for Azure Key Vault operations
/// </summary>
public interface IAzureKeyVaultService
{
    Task<string?> GetSecretAsync(string secretName);
    Task SetSecretAsync(string secretName, string secretValue);
}

/// <summary>
/// Azure Key Vault service implementation
/// </summary>
public class AzureKeyVaultService : IAzureKeyVaultService
{
    private readonly Azure.Security.KeyVault.Secrets.SecretClient _secretClient;
    private readonly ILogger<AzureKeyVaultService> _logger;

    public AzureKeyVaultService(IConfiguration configuration, ILogger<AzureKeyVaultService> logger)
    {
        _logger = logger;
        
        var keyVaultName = configuration["KeyVaultName"];
        if (string.IsNullOrEmpty(keyVaultName))
        {
            throw new InvalidOperationException("KeyVaultName is not configured");
        }

        var keyVaultUri = new Uri($"https://{keyVaultName}.vault.azure.net/");
        _secretClient = new Azure.Security.KeyVault.Secrets.SecretClient(keyVaultUri, new DefaultAzureCredential());
    }

    public async Task<string?> GetSecretAsync(string secretName)
    {
        try
        {
            var secret = await _secretClient.GetSecretAsync(secretName);
            return secret.Value.Value;
        }
        catch (Azure.RequestFailedException ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve secret {SecretName} from Key Vault", secretName);
            return null;
        }
    }

    public async Task SetSecretAsync(string secretName, string secretValue)
    {
        try
        {
            await _secretClient.SetSecretAsync(secretName, secretValue);
            _logger.LogInformation("Successfully set secret {SecretName} in Key Vault", secretName);
        }
        catch (Azure.RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to set secret {SecretName} in Key Vault", secretName);
            throw;
        }
    }
}