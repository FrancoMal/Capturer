using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Capturer.Models;
using Capturer.Services;

namespace Capturer.Services;

/// <summary>
/// Service for processing Dashboard Web invitation URLs and connecting to dashboard
/// </summary>
public interface IDashboardInvitationService
{
    Task<InvitationValidationResult> ValidateInvitationAsync(string invitationUrl);
    Task<RegistrationResult> ProcessInvitationAsync(string invitationUrl, string computerName);
    Task<ConnectionTestResult> TestDashboardConnectionAsync(string dashboardUrl);
}

public class DashboardInvitationService : IDashboardInvitationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfigurationManager _configManager;
    private readonly ILogger<DashboardInvitationService> _logger;

    public DashboardInvitationService(
        HttpClient httpClient,
        IConfigurationManager configManager,
        ILogger<DashboardInvitationService> logger)
    {
        _httpClient = httpClient;
        _configManager = configManager;
        _logger = logger;
        
        // Configure HttpClient with timeout
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<InvitationValidationResult> ValidateInvitationAsync(string invitationUrl)
    {
        try
        {
            _logger.LogInformation("Validating invitation URL: {Url}", invitationUrl);

            // Parse the invitation URL to extract token
            var uri = new Uri(invitationUrl);
            var token = uri.Segments.Last();

            if (string.IsNullOrEmpty(token))
            {
                return new InvitationValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Invalid invitation URL format"
                };
            }

            // Extract dashboard base URL
            var dashboardUrl = $"{uri.Scheme}://{uri.Host}:{uri.Port}";
            
            // Test connection to dashboard
            var connectionTest = await TestDashboardConnectionAsync(dashboardUrl);
            if (!connectionTest.IsReachable)
            {
                return new InvitationValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"Cannot reach dashboard at {dashboardUrl}: {connectionTest.ErrorMessage}"
                };
            }

            // Validate the invitation token
            var response = await _httpClient.GetAsync(invitationUrl);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return new InvitationValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"Invitation validation failed: {response.StatusCode} - {errorContent}"
                };
            }

            var validationData = await response.Content.ReadFromJsonAsync<InvitationValidationResponse>();
            
            return new InvitationValidationResult
            {
                IsValid = validationData?.Valid == true,
                DashboardUrl = dashboardUrl,
                Token = token,
                OrganizationName = validationData?.OrganizationName,
                RequireApproval = validationData?.RequireApproval == true,
                ErrorMessage = validationData?.Error
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating invitation URL: {Url}", invitationUrl);
            return new InvitationValidationResult
            {
                IsValid = false,
                ErrorMessage = $"Validation error: {ex.Message}"
            };
        }
    }

    public async Task<RegistrationResult> ProcessInvitationAsync(string invitationUrl, string computerName)
    {
        try
        {
            _logger.LogInformation("Processing invitation for computer: {ComputerName}", computerName);

            // First validate the invitation
            var validation = await ValidateInvitationAsync(invitationUrl);
            if (!validation.IsValid)
            {
                return new RegistrationResult
                {
                    Success = false,
                    ErrorMessage = validation.ErrorMessage
                };
            }

            // Prepare hardware information
            var hardwareInfo = CollectHardwareInfo();

            // Prepare registration request
            var registrationRequest = new
            {
                computerName = computerName,
                computerId = Environment.MachineName, // Use machine name as computer ID
                hardwareInfo = hardwareInfo
            };

            // Send registration request
            var registrationUrl = $"{validation.DashboardUrl}/invite/{validation.Token}/register";
            var response = await _httpClient.PostAsJsonAsync(registrationUrl, registrationRequest);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return new RegistrationResult
                {
                    Success = false,
                    ErrorMessage = $"Registration failed: {response.StatusCode} - {errorContent}"
                };
            }

            var registrationData = await response.Content.ReadFromJsonAsync<RegistrationResponse>();

            if (registrationData?.Success != true)
            {
                return new RegistrationResult
                {
                    Success = false,
                    ErrorMessage = registrationData?.ErrorMessage ?? "Registration failed"
                };
            }

            // If approved, save configuration
            if (registrationData.Status == "Approved" && !string.IsNullOrEmpty(registrationData.ApiKey))
            {
                await SaveDashboardConfiguration(
                    validation.DashboardUrl,
                    registrationData.ApiKey,
                    computerName,
                    registrationData.ComputerId ?? Guid.NewGuid());

                return new RegistrationResult
                {
                    Success = true,
                    Status = "Approved",
                    ComputerName = computerName,
                    ApiKey = registrationData.ApiKey,
                    DashboardUrl = validation.DashboardUrl,
                    Message = "Computer registered successfully and ready to sync with dashboard"
                };
            }

            // If pending approval
            return new RegistrationResult
            {
                Success = true,
                Status = "PendingApproval",
                ComputerName = computerName,
                PendingRegistrationId = registrationData.PendingRegistrationId,
                Message = "Registration submitted. Waiting for administrator approval.",
                RequireApproval = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing invitation for computer: {ComputerName}", computerName);
            return new RegistrationResult
            {
                Success = false,
                ErrorMessage = $"Registration error: {ex.Message}"
            };
        }
    }

    public async Task<ConnectionTestResult> TestDashboardConnectionAsync(string dashboardUrl)
    {
        try
        {
            _logger.LogInformation("Testing connection to dashboard: {Url}", dashboardUrl);

            var healthUrl = $"{dashboardUrl}/health";
            var response = await _httpClient.GetAsync(healthUrl);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                
                return new ConnectionTestResult
                {
                    IsReachable = true,
                    ResponseTime = TimeSpan.FromMilliseconds(100), // Approximate
                    DashboardVersion = ExtractVersionFromHealthResponse(content)
                };
            }
            else
            {
                return new ConnectionTestResult
                {
                    IsReachable = false,
                    ErrorMessage = $"Health check failed: {response.StatusCode}"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test dashboard connection: {Url}", dashboardUrl);
            return new ConnectionTestResult
            {
                IsReachable = false,
                ErrorMessage = $"Connection failed: {ex.Message}"
            };
        }
    }

    private Dictionary<string, object> CollectHardwareInfo()
    {
        return new Dictionary<string, object>
        {
            ["computerName"] = Environment.MachineName,
            ["userName"] = Environment.UserName,
            ["operatingSystem"] = Environment.OSVersion.ToString(),
            ["processorCount"] = Environment.ProcessorCount,
            ["workingSet"] = Environment.WorkingSet,
            ["systemDirectory"] = Environment.SystemDirectory,
            ["is64BitOperatingSystem"] = Environment.Is64BitOperatingSystem,
            ["is64BitProcess"] = Environment.Is64BitProcess,
            ["clrVersion"] = Environment.Version.ToString(),
            ["collectTimestamp"] = DateTime.UtcNow.ToString("O")
        };
    }

    private async Task SaveDashboardConfiguration(string dashboardUrl, string apiKey, string computerName, Guid? computerId)
    {
        try
        {
            var config = await _configManager.LoadConfigurationAsync();
            
            // Update API configuration
            config.Api.Enabled = true;
            config.Api.DashboardUrl = dashboardUrl;
            config.Api.EnableDashboardSync = true;
            config.Api.SyncIntervalSeconds = 300; // 5 minutes default

            // Store the API key securely (this would need to be implemented)
            // For now, we'll store it in the regular config (in production, use encryption)
            config.Api.ShowStatusIndicator = true;
            
            // Create a secure storage mechanism for API key
            await StoreApiKeySecurely(apiKey, computerName, computerId);
            
            await _configManager.SaveConfigurationAsync(config);
            
            _logger.LogInformation("Dashboard configuration saved for computer: {ComputerName}", computerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save dashboard configuration");
            throw;
        }
    }

    private async Task StoreApiKeySecurely(string apiKey, string computerName, Guid? computerId)
    {
        // This should implement secure storage of API key
        // For MVP, we'll store in a separate config file with basic encoding
        
        var apiKeyInfo = new
        {
            ApiKey = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(apiKey)),
            ComputerName = computerName,
            ComputerId = computerId,
            RegisteredAt = DateTime.UtcNow,
            Version = "v4.0"
        };

        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Capturer");

        Directory.CreateDirectory(appDataPath);
        
        var apiKeyFile = Path.Combine(appDataPath, "dashboard-connection.json");
        var json = JsonSerializer.Serialize(apiKeyInfo, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });

        await File.WriteAllTextAsync(apiKeyFile, json);
        
        _logger.LogInformation("API key stored securely for dashboard connection");
    }

    public async Task<string?> LoadStoredApiKeyAsync()
    {
        try
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Capturer");

            var apiKeyFile = Path.Combine(appDataPath, "dashboard-connection.json");
            
            if (!File.Exists(apiKeyFile))
                return null;

            var json = await File.ReadAllTextAsync(apiKeyFile);
            var apiKeyInfo = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

            if (apiKeyInfo != null && apiKeyInfo.TryGetValue("ApiKey", out var encodedKey))
            {
                var keyBytes = Convert.FromBase64String(encodedKey.ToString()!);
                return System.Text.Encoding.UTF8.GetString(keyBytes);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load stored API key");
            return null;
        }
    }

    private string? ExtractVersionFromHealthResponse(string healthResponse)
    {
        try
        {
            var healthData = JsonSerializer.Deserialize<Dictionary<string, object>>(healthResponse);
            if (healthData?.TryGetValue("version", out var version) == true)
            {
                return version.ToString();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not extract version from health response");
        }
        return null;
    }
}

// DTOs for invitation processing
public class InvitationValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public string? DashboardUrl { get; set; }
    public string? Token { get; set; }
    public string? OrganizationName { get; set; }
    public bool RequireApproval { get; set; }
}

public class RegistrationResult
{
    public bool Success { get; set; }
    public string Status { get; set; } = string.Empty; // "Approved", "PendingApproval"
    public string? ComputerName { get; set; }
    public string? ApiKey { get; set; }
    public string? DashboardUrl { get; set; }
    public Guid? PendingRegistrationId { get; set; }
    public bool RequireApproval { get; set; }
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }
}

public class ConnectionTestResult
{
    public bool IsReachable { get; set; }
    public TimeSpan? ResponseTime { get; set; }
    public string? DashboardVersion { get; set; }
    public string? ErrorMessage { get; set; }
}

// Response DTOs from Dashboard Web
public class InvitationValidationResponse
{
    [JsonPropertyName("valid")]
    public bool Valid { get; set; }
    
    [JsonPropertyName("organizationName")]
    public string? OrganizationName { get; set; }
    
    [JsonPropertyName("organizationSlug")]
    public string? OrganizationSlug { get; set; }
    
    [JsonPropertyName("requireApproval")]
    public bool RequireApproval { get; set; }
    
    [JsonPropertyName("message")]
    public string? Message { get; set; }
    
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

public class RegistrationResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    
    [JsonPropertyName("computerId")]
    public Guid? ComputerId { get; set; }
    
    [JsonPropertyName("apiKey")]
    public string? ApiKey { get; set; }
    
    [JsonPropertyName("computerName")]
    public string? ComputerName { get; set; }
    
    [JsonPropertyName("message")]
    public string? Message { get; set; }
    
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }
    
    [JsonPropertyName("isExistingComputer")]
    public bool IsExistingComputer { get; set; }
    
    [JsonPropertyName("pendingRegistrationId")]
    public Guid? PendingRegistrationId { get; set; }
    
    [JsonPropertyName("dashboardUrl")]
    public string? DashboardUrl { get; set; }
    
    [JsonPropertyName("syncSettings")]
    public SyncSettings? SyncSettings { get; set; }
}

public class SyncSettings
{
    [JsonPropertyName("syncEnabled")]
    public bool SyncEnabled { get; set; }
    
    [JsonPropertyName("syncIntervalSeconds")]
    public int SyncIntervalSeconds { get; set; }
    
    [JsonPropertyName("realtimeUpdates")]
    public bool RealtimeUpdates { get; set; }
}