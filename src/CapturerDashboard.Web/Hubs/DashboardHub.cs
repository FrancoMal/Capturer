using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace CapturerDashboard.Web.Hubs;

public class DashboardHub : Hub
{
    private readonly ILogger<DashboardHub> _logger;
    private static readonly ConcurrentDictionary<string, UserConnectionInfo> _connections = new();

    public DashboardHub(ILogger<DashboardHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Join an organization group for real-time updates
    /// </summary>
    public async Task JoinOrganizationGroup(string organizationId)
    {
        var groupName = $"org-{organizationId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        
        _logger.LogInformation("User {ConnectionId} joined organization group {GroupName}", 
            Context.ConnectionId, groupName);
        
        // Update user connection info
        if (_connections.TryGetValue(Context.ConnectionId, out var info))
        {
            info.OrganizationGroups.Add(groupName);
        }
        
        // Notify group about new member
        await Clients.OthersInGroup(groupName).SendAsync("UserJoinedGroup", new
        {
            ConnectionId = Context.ConnectionId,
            JoinedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Leave an organization group
    /// </summary>
    public async Task LeaveOrganizationGroup(string organizationId)
    {
        var groupName = $"org-{organizationId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        
        _logger.LogInformation("User {ConnectionId} left organization group {GroupName}", 
            Context.ConnectionId, groupName);
        
        // Update user connection info
        if (_connections.TryGetValue(Context.ConnectionId, out var info))
        {
            info.OrganizationGroups.Remove(groupName);
        }
    }

    /// <summary>
    /// Subscribe to computer-specific updates
    /// </summary>
    public async Task SubscribeToComputer(string computerId)
    {
        var groupName = $"computer-{computerId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        
        _logger.LogDebug("User {ConnectionId} subscribed to computer {ComputerId}", 
            Context.ConnectionId, computerId);
        
        // Send current computer status
        await Clients.Caller.SendAsync("ComputerSubscribed", new
        {
            ComputerId = computerId,
            SubscribedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Unsubscribe from computer-specific updates
    /// </summary>
    public async Task UnsubscribeFromComputer(string computerId)
    {
        var groupName = $"computer-{computerId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        
        _logger.LogDebug("User {ConnectionId} unsubscribed from computer {ComputerId}", 
            Context.ConnectionId, computerId);
    }

    /// <summary>
    /// Request current dashboard data
    /// </summary>
    public async Task RequestDashboardUpdate()
    {
        _logger.LogDebug("Dashboard update requested by {ConnectionId}", Context.ConnectionId);
        
        // This would typically fetch current data and send it back
        // For now, we'll just acknowledge the request
        await Clients.Caller.SendAsync("DashboardUpdateReceived", new
        {
            RequestedAt = DateTime.UtcNow,
            ConnectionId = Context.ConnectionId
        });
    }

    /// <summary>
    /// Ping for connection health check
    /// </summary>
    public async Task Ping()
    {
        await Clients.Caller.SendAsync("Pong", DateTime.UtcNow);
    }

    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        var userAgent = httpContext?.Request.Headers["User-Agent"].ToString() ?? "Unknown";
        var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        
        // Store connection information
        var connectionInfo = new UserConnectionInfo
        {
            ConnectionId = Context.ConnectionId,
            ConnectedAt = DateTime.UtcNow,
            UserAgent = userAgent,
            IpAddress = ipAddress,
            UserId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
        };
        
        _connections.TryAdd(Context.ConnectionId, connectionInfo);
        
        _logger.LogInformation("SignalR client connected: {ConnectionId} from {IpAddress} using {UserAgent}", 
            Context.ConnectionId, ipAddress, userAgent);
        
        // Send welcome message with connection info
        await Clients.Caller.SendAsync("Connected", new
        {
            ConnectionId = Context.ConnectionId,
            ConnectedAt = connectionInfo.ConnectedAt,
            ServerTimeZone = TimeZoneInfo.Local.Id
        });
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Remove connection info
        _connections.TryRemove(Context.ConnectionId, out var connectionInfo);
        
        if (exception != null)
        {
            _logger.LogWarning("SignalR client disconnected with error: {ConnectionId}, Error: {Error}", 
                Context.ConnectionId, exception.Message);
        }
        else
        {
            _logger.LogInformation("SignalR client disconnected: {ConnectionId}", Context.ConnectionId);
        }
        
        // Calculate session duration
        var sessionDuration = connectionInfo != null 
            ? DateTime.UtcNow - connectionInfo.ConnectedAt 
            : TimeSpan.Zero;
        
        _logger.LogDebug("Session duration for {ConnectionId}: {Duration}", 
            Context.ConnectionId, sessionDuration);
        
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Get active connections count
    /// </summary>
    public static int GetActiveConnectionsCount() => _connections.Count;
    
    /// <summary>
    /// Get connections by organization
    /// </summary>
    public static IEnumerable<UserConnectionInfo> GetConnectionsByOrganization(string organizationId)
    {
        var groupName = $"org-{organizationId}";
        return _connections.Values.Where(c => c.OrganizationGroups.Contains(groupName));
    }
}

/// <summary>
/// Information about a SignalR user connection
/// </summary>
public class UserConnectionInfo
{
    public string ConnectionId { get; set; } = string.Empty;
    public DateTime ConnectedAt { get; set; }
    public string UserAgent { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public HashSet<string> OrganizationGroups { get; set; } = new();
}