using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using CapturerDashboard.Data.Context;
using CapturerDashboard.Core.Models;

namespace CapturerDashboard.Web.Services;

/// <summary>
/// Service for communicating with individual Capturer client APIs
/// </summary>
public interface ICapturerClientService
{
    Task<CapturerHealthCheckResult> CheckCapturerHealthAsync(Computer computer);
    Task<CapturerSystemStatusResult> GetCapturerStatusAsync(Computer computer);
    Task<CapturerActivityResult> GetCapturerActivityAsync(Computer computer);
    Task<bool> TriggerCapturerCaptureAsync(Computer computer);
    Task<List<CapturerConnectionStatus>> GetAllComputersStatusAsync(Guid organizationId);
    Task SyncAllComputersDataAsync(Guid organizationId);
}

public class CapturerClientService : ICapturerClientService
{
    private readonly DashboardDbContext _context;
    private readonly ILogger<CapturerClientService> _logger;
    private readonly HttpClient _httpClient;

    public CapturerClientService(
        DashboardDbContext context, 
        ILogger<CapturerClientService> logger,
        HttpClient httpClient)
    {
        _context = context;
        _logger = logger;
        _httpClient = httpClient;
        
        // Configure HttpClient for Capturer API communication
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
    }

    public async Task<CapturerHealthCheckResult> CheckCapturerHealthAsync(Computer computer)
    {
        try
        {
            var capturerApiUrl = GetCapturerApiUrl(computer);
            var response = await _httpClient.GetAsync($"{capturerApiUrl}/api/v1/health");
            
            if (response.IsSuccessStatusCode)
            {
                var healthData = await response.Content.ReadAsStringAsync();
                var health = JsonSerializer.Deserialize<Dictionary<string, object>>(healthData);
                
                return new CapturerHealthCheckResult
                {
                    IsHealthy = true,
                    ResponseTime = TimeSpan.FromMilliseconds(100), // Approximate
                    Version = health?.ContainsKey("details") == true ? 
                        ExtractVersionFromDetails(health["details"]) : "Unknown"
                };
            }
            
            return new CapturerHealthCheckResult
            {
                IsHealthy = false,
                ErrorMessage = $"Health check failed: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Health check failed for computer {ComputerId}", computer.ComputerId);
            return new CapturerHealthCheckResult
            {
                IsHealthy = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<CapturerSystemStatusResult> GetCapturerStatusAsync(Computer computer)
    {
        try
        {
            var capturerApiUrl = GetCapturerApiUrl(computer);
            var request = new HttpRequestMessage(HttpMethod.Get, $"{capturerApiUrl}/api/v1/status");
            request.Headers.Add("X-Api-Key", computer.ApiKey);
            
            var response = await _httpClient.SendAsync(request);
            
            if (response.IsSuccessStatusCode)
            {
                var statusJson = await response.Content.ReadAsStringAsync();
                var statusData = JsonSerializer.Deserialize<CapturerApiStatusResponse>(statusJson);
                
                return new CapturerSystemStatusResult
                {
                    Success = true,
                    Data = statusData
                };
            }
            
            return new CapturerSystemStatusResult
            {
                Success = false,
                ErrorMessage = $"Status request failed: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get status for computer {ComputerId}", computer.ComputerId);
            return new CapturerSystemStatusResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<CapturerActivityResult> GetCapturerActivityAsync(Computer computer)
    {
        try
        {
            var capturerApiUrl = GetCapturerApiUrl(computer);
            var request = new HttpRequestMessage(HttpMethod.Get, $"{capturerApiUrl}/api/v1/activity/current");
            request.Headers.Add("X-Api-Key", computer.ApiKey);
            
            var response = await _httpClient.SendAsync(request);
            
            if (response.IsSuccessStatusCode)
            {
                var activityJson = await response.Content.ReadAsStringAsync();
                var activityData = JsonSerializer.Deserialize<CapturerActivityApiResponse>(activityJson);
                
                // Store activity data in our database
                await StoreActivityDataAsync(computer, activityData);
                
                return new CapturerActivityResult
                {
                    Success = true,
                    Data = activityData
                };
            }
            
            return new CapturerActivityResult
            {
                Success = false,
                ErrorMessage = $"Activity request failed: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get activity for computer {ComputerId}", computer.ComputerId);
            return new CapturerActivityResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<bool> TriggerCapturerCaptureAsync(Computer computer)
    {
        try
        {
            var capturerApiUrl = GetCapturerApiUrl(computer);
            var request = new HttpRequestMessage(HttpMethod.Post, $"{capturerApiUrl}/api/v1/commands/capture");
            request.Headers.Add("X-Api-Key", computer.ApiKey);
            
            var response = await _httpClient.SendAsync(request);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully triggered capture for computer {ComputerId}", computer.ComputerId);
                return true;
            }
            
            _logger.LogWarning("Failed to trigger capture for computer {ComputerId}: {StatusCode}", 
                computer.ComputerId, response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering capture for computer {ComputerId}", computer.ComputerId);
            return false;
        }
    }

    public async Task<List<CapturerConnectionStatus>> GetAllComputersStatusAsync(Guid organizationId)
    {
        var computers = await _context.Computers
            .Where(c => c.OrganizationId == organizationId && c.IsActive)
            .ToListAsync();
        
        var statusTasks = computers.Select(async computer =>
        {
            var health = await CheckCapturerHealthAsync(computer);
            var status = health.IsHealthy ? await GetCapturerStatusAsync(computer) : null;
            
            // Update computer last seen if healthy
            if (health.IsHealthy)
            {
                computer.LastSeenAt = DateTime.UtcNow;
                computer.Status = "Online";
            }
            else
            {
                computer.Status = "Offline";
            }
            
            return new CapturerConnectionStatus
            {
                Computer = computer,
                IsReachable = health.IsHealthy,
                LastResponseTime = health.ResponseTime,
                SystemStatus = status?.Data,
                ErrorMessage = health.ErrorMessage
            };
        });

        var results = await Task.WhenAll(statusTasks);
        
        // Update database with new statuses
        await _context.SaveChangesAsync();
        
        return results.ToList();
    }

    public async Task SyncAllComputersDataAsync(Guid organizationId)
    {
        _logger.LogInformation("Starting data sync for organization {OrganizationId}", organizationId);
        
        var computers = await _context.Computers
            .Where(c => c.OrganizationId == organizationId && c.IsActive)
            .ToListAsync();

        var syncTasks = computers.Select(async computer =>
        {
            try
            {
                var activity = await GetCapturerActivityAsync(computer);
                if (activity.Success && activity.Data != null)
                {
                    _logger.LogInformation("Successfully synced data for computer {ComputerId}", computer.ComputerId);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync data for computer {ComputerId}", computer.ComputerId);
                return false;
            }
        });

        var results = await Task.WhenAll(syncTasks);
        var successCount = results.Count(r => r);
        
        _logger.LogInformation("Data sync completed: {SuccessCount}/{TotalCount} computers synced successfully", 
            successCount, computers.Count);
    }

    private async Task StoreActivityDataAsync(Computer computer, CapturerActivityApiResponse? activityData)
    {
        if (activityData?.Data == null) return;

        try
        {
            var activity = activityData.Data;
            
            // Check if we already have a report for this date
            var existingReport = await _context.ActivityReports
                .FirstOrDefaultAsync(r => r.ComputerId == computer.Id && 
                                        r.ReportDate.Date == DateTime.Parse(activity.ReportDate).Date);

            if (existingReport != null)
            {
                // Update existing report
                existingReport.TotalQuadrants = activity.QuadrantActivities?.Count ?? 0;
                existingReport.TotalComparisons = activity.QuadrantActivities?.Sum(q => q.TotalComparisons) ?? 0;
                existingReport.TotalActivities = activity.QuadrantActivities?.Sum(q => q.ActivityDetectionCount) ?? 0;
                existingReport.AverageActivityRate = (decimal)(activity.QuadrantActivities?.Average(q => q.ActivityRate) ?? 0);
                existingReport.ReportData = JsonSerializer.Serialize(activity);
                existingReport.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Create new report
                var newReport = new ActivityReport
                {
                    ComputerId = computer.Id,
                    ReportDate = DateTime.Parse(activity.ReportDate),
                    StartTime = TimeSpan.Parse(activity.StartTime),
                    EndTime = TimeSpan.Parse(activity.EndTime),
                    TotalQuadrants = activity.QuadrantActivities?.Count ?? 0,
                    TotalComparisons = activity.QuadrantActivities?.Sum(q => q.TotalComparisons) ?? 0,
                    TotalActivities = activity.QuadrantActivities?.Sum(q => q.ActivityDetectionCount) ?? 0,
                    AverageActivityRate = (decimal)(activity.QuadrantActivities?.Average(q => q.ActivityRate) ?? 0),
                    ReportData = JsonSerializer.Serialize(activity),
                    CreatedAt = DateTime.UtcNow
                };

                _context.ActivityReports.Add(newReport);
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store activity data for computer {ComputerId}", computer.ComputerId);
        }
    }

    private string GetCapturerApiUrl(Computer computer)
    {
        // For now, assume Capturer API is running on the same IP as the computer with port 8080
        // This could be made configurable per computer later
        
        // Try to extract IP from last known IP address or use computer ID as hostname
        var baseUrl = $"http://localhost:8080"; // Default for local testing
        
        // TODO: In production, this would need to resolve computer IP addresses
        // from network discovery or configuration
        
        return baseUrl;
    }

    private string? ExtractVersionFromDetails(object details)
    {
        try
        {
            var detailsElement = (JsonElement)details;
            if (detailsElement.TryGetProperty("version", out var versionElement))
            {
                return versionElement.GetString();
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not extract version from health details");
        }
        return null;
    }
}

// Result DTOs
public class CapturerHealthCheckResult
{
    public bool IsHealthy { get; set; }
    public TimeSpan? ResponseTime { get; set; }
    public string? Version { get; set; }
    public string? ErrorMessage { get; set; }
}

public class CapturerSystemStatusResult
{
    public bool Success { get; set; }
    public CapturerApiStatusResponse? Data { get; set; }
    public string? ErrorMessage { get; set; }
}

public class CapturerActivityResult
{
    public bool Success { get; set; }
    public CapturerActivityApiResponse? Data { get; set; }
    public string? ErrorMessage { get; set; }
}

public class CapturerConnectionStatus
{
    public Computer Computer { get; set; } = null!;
    public bool IsReachable { get; set; }
    public TimeSpan? LastResponseTime { get; set; }
    public CapturerApiStatusData? SystemStatus { get; set; }
    public string? ErrorMessage { get; set; }
}

// DTOs for Capturer API responses (matching the existing API structure)
public class CapturerApiStatusResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public CapturerApiStatusData? Data { get; set; }
    public string? ErrorCode { get; set; }
    public string Timestamp { get; set; } = string.Empty;
}

public class CapturerApiStatusData
{
    public bool IsCapturing { get; set; }
    public string? LastCaptureTime { get; set; }
    public int TotalScreenshots { get; set; }
    public CapturerSystemInfo? SystemInfo { get; set; }
    public string Version { get; set; } = string.Empty;
    public string StatusTimestamp { get; set; } = string.Empty;
}

public class CapturerSystemInfo
{
    public string ComputerName { get; set; } = string.Empty;
    public string OperatingSystem { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public int ProcessorCount { get; set; }
    public long WorkingSetMemory { get; set; }
    public string Uptime { get; set; } = string.Empty;
    public List<CapturerScreenInfo> AvailableScreens { get; set; } = new();
}

public class CapturerScreenInfo
{
    public int Index { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public bool IsPrimary { get; set; }
    public string Resolution { get; set; } = string.Empty;
}

public class CapturerActivityApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public CapturerActivityData? Data { get; set; }
    public string Timestamp { get; set; } = string.Empty;
}

public class CapturerActivityData
{
    public string Id { get; set; } = string.Empty;
    public string ComputerId { get; set; } = string.Empty;
    public string ComputerName { get; set; } = string.Empty;
    public string ReportDate { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public List<CapturerQuadrantActivity> QuadrantActivities { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string Version { get; set; } = string.Empty;
}

public class CapturerQuadrantActivity
{
    public string QuadrantName { get; set; } = string.Empty;
    public long TotalComparisons { get; set; }
    public long ActivityDetectionCount { get; set; }
    public double ActivityRate { get; set; }
    public double AverageChangePercentage { get; set; }
    public string ActiveDuration { get; set; } = string.Empty;
    public List<CapturerActivityTimelineEntry> Timeline { get; set; } = new();
}

public class CapturerActivityTimelineEntry
{
    public string Timestamp { get; set; } = string.Empty;
    public double ActivityLevel { get; set; }
    public Dictionary<string, double> QuadrantLevels { get; set; } = new();
}