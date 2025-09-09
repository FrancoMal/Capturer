using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Capturer.Api.DTOs;

namespace Capturer.Api.Hubs;

/// <summary>
/// SignalR Hub para comunicación real-time entre Cliente y Dashboard Web
/// Implementación según plan v4.0 - Real-time Streaming
/// </summary>
[Authorize(Policy = "ApiKeyPolicy")]
public class ActivityHub : Hub
{
    private readonly ILogger<ActivityHub> _logger;

    public ActivityHub(ILogger<ActivityHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        try
        {
            var connectionId = Context.ConnectionId;
            var computerName = Context.User?.Claims?.FirstOrDefault(c => c.Type == "ComputerId")?.Value ?? "Unknown";
            
            _logger.LogInformation("Dashboard connected: {ConnectionId} from {ComputerName}", connectionId, computerName);
            
            // Join Dashboard group for broadcast messages
            await Groups.AddToGroupAsync(connectionId, "DashboardClients");
            
            // Send initial connection confirmation
            await Clients.Caller.SendAsync("ConnectionConfirmed", new
            {
                ConnectionId = connectionId,
                ServerTime = DateTime.UtcNow,
                Message = "Connected to Capturer v4.0 API"
            });

            await base.OnConnectedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during client connection");
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            var connectionId = Context.ConnectionId;
            _logger.LogInformation("Dashboard disconnected: {ConnectionId}", connectionId);
            
            await Groups.RemoveFromGroupAsync(connectionId, "DashboardClients");
            await base.OnDisconnectedAsync(exception);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during client disconnection");
        }
    }

    /// <summary>
    /// Subscribe to activity updates from specific computer
    /// </summary>
    public async Task SubscribeToActivityUpdates(string computerId)
    {
        try
        {
            var groupName = $"Activity_{computerId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            
            _logger.LogInformation("Client {ConnectionId} subscribed to activity updates for {ComputerId}", 
                Context.ConnectionId, computerId);
                
            await Clients.Caller.SendAsync("SubscriptionConfirmed", new
            {
                ComputerId = computerId,
                GroupName = groupName,
                Message = $"Subscribed to activity updates for {computerId}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing to activity updates");
            await Clients.Caller.SendAsync("SubscriptionError", new
            {
                Error = "Failed to subscribe to activity updates",
                Details = ex.Message
            });
        }
    }

    /// <summary>
    /// Unsubscribe from activity updates
    /// </summary>
    public async Task UnsubscribeFromActivityUpdates(string computerId)
    {
        try
        {
            var groupName = $"Activity_{computerId}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            
            _logger.LogInformation("Client {ConnectionId} unsubscribed from activity updates for {ComputerId}", 
                Context.ConnectionId, computerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unsubscribing from activity updates");
        }
    }

    /// <summary>
    /// Request immediate system status update
    /// </summary>
    public async Task RequestStatusUpdate()
    {
        try
        {
            _logger.LogInformation("Status update requested by {ConnectionId}", Context.ConnectionId);
            
            // This would trigger an immediate status broadcast
            // Implementation will be completed when Dashboard is ready
            await Clients.Caller.SendAsync("StatusUpdateRequested", new
            {
                RequestTime = DateTime.UtcNow,
                Message = "Status update request received - implementation pending"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing status update request");
        }
    }
}

/// <summary>
/// Service para enviar updates via SignalR Hub
/// </summary>
public interface IActivityHubService
{
    Task BroadcastActivityUpdate(ActivityReportDto activityReport);
    Task BroadcastSystemStatus(SystemStatusDto systemStatus);
    Task BroadcastScreenshotCaptured(string fileName, DateTime captureTime);
    Task NotifyDashboardError(string error, string details);
}

public class ActivityHubService : IActivityHubService
{
    private readonly IHubContext<ActivityHub> _hubContext;
    private readonly ILogger<ActivityHubService> _logger;

    public ActivityHubService(IHubContext<ActivityHub> hubContext, ILogger<ActivityHubService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task BroadcastActivityUpdate(ActivityReportDto activityReport)
    {
        try
        {
            var groupName = $"Activity_{activityReport.ComputerId}";
            
            await _hubContext.Clients.Group(groupName).SendAsync("ActivityUpdate", new
            {
                Type = "ActivityReport",
                Data = activityReport,
                Timestamp = DateTime.UtcNow
            });
            
            _logger.LogDebug("Activity update broadcasted for {ComputerId}", activityReport.ComputerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting activity update");
        }
    }

    public async Task BroadcastSystemStatus(SystemStatusDto systemStatus)
    {
        try
        {
            await _hubContext.Clients.Group("DashboardClients").SendAsync("SystemStatusUpdate", new
            {
                Type = "SystemStatus",
                Data = systemStatus,
                Timestamp = DateTime.UtcNow
            });
            
            _logger.LogDebug("System status broadcasted");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting system status");
        }
    }

    public async Task BroadcastScreenshotCaptured(string fileName, DateTime captureTime)
    {
        try
        {
            await _hubContext.Clients.Group("DashboardClients").SendAsync("ScreenshotCaptured", new
            {
                Type = "ScreenshotEvent",
                Data = new
                {
                    FileName = fileName,
                    CaptureTime = captureTime,
                    ComputerName = Environment.MachineName
                },
                Timestamp = DateTime.UtcNow
            });
            
            _logger.LogDebug("Screenshot capture event broadcasted: {FileName}", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting screenshot event");
        }
    }

    public async Task NotifyDashboardError(string error, string details)
    {
        try
        {
            await _hubContext.Clients.Group("DashboardClients").SendAsync("ErrorNotification", new
            {
                Type = "Error",
                Data = new
                {
                    Error = error,
                    Details = details,
                    ComputerName = Environment.MachineName
                },
                Timestamp = DateTime.UtcNow
            });
            
            _logger.LogWarning("Error notification broadcasted: {Error}", error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting error notification");
        }
    }
}