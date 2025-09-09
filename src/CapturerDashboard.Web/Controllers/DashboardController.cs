using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CapturerDashboard.Data.Context;

namespace CapturerDashboard.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly DashboardDbContext _context;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(DashboardDbContext context, ILogger<DashboardController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Gets dashboard overview statistics
    /// </summary>
    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview([FromQuery] string? organizationId = null)
    {
        try
        {
            // Get default organization if none specified
            Guid orgId;
            if (string.IsNullOrEmpty(organizationId) || !Guid.TryParse(organizationId, out orgId))
            {
                var defaultOrg = await _context.Organizations.FirstAsync();
                orgId = defaultOrg.Id;
            }

            // Get basic counts
            var totalComputers = await _context.Computers
                .CountAsync(c => c.OrganizationId == orgId && c.IsActive);

            var onlineComputers = await _context.Computers
                .CountAsync(c => c.OrganizationId == orgId && c.IsActive && 
                            c.LastSeenAt > DateTime.UtcNow.AddMinutes(-5));

            var todayReports = await _context.ActivityReports
                .Join(_context.Computers, r => r.ComputerId, c => c.Id, (r, c) => new { r, c })
                .CountAsync(x => x.c.OrganizationId == orgId && x.r.ReportDate == DateOnly.FromDateTime(DateTime.Today));

            // Calculate average activity for today
            var avgActivity = await _context.ActivityReports
                .Join(_context.Computers, r => r.ComputerId, c => c.Id, (r, c) => new { r, c })
                .Where(x => x.c.OrganizationId == orgId && x.r.ReportDate == DateOnly.FromDateTime(DateTime.Today))
                .AverageAsync(x => (double?)x.r.AverageActivityRate) ?? 0;

            // Get unacknowledged alerts count
            var alertsCount = await _context.Alerts
                .CountAsync(a => a.OrganizationId == orgId && !a.IsAcknowledged);

            return Ok(new
            {
                summary = new
                {
                    totalComputers,
                    onlineComputers,
                    offlineComputers = totalComputers - onlineComputers,
                    todayReports,
                    averageActivity = Math.Round(avgActivity, 1),
                    alertsCount
                },
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard overview");
            return StatusCode(500, new { error = "Failed to get overview" });
        }
    }

    /// <summary>
    /// Gets activity timeline for charts
    /// </summary>
    [HttpGet("activity-timeline")]
    public async Task<IActionResult> GetActivityTimeline([FromQuery] string? organizationId = null, [FromQuery] int hours = 24)
    {
        try
        {
            Guid orgId;
            if (string.IsNullOrEmpty(organizationId) || !Guid.TryParse(organizationId, out orgId))
            {
                var defaultOrg = await _context.Organizations.FirstAsync();
                orgId = defaultOrg.Id;
            }

            var fromTime = DateTime.UtcNow.AddHours(-hours);

            var timeline = await _context.ActivityReports
                .Join(_context.Computers, r => r.ComputerId, c => c.Id, (r, c) => new { r, c })
                .Where(x => x.c.OrganizationId == orgId && x.r.CreatedAt >= fromTime)
                .GroupBy(x => new 
                { 
                    Hour = x.r.CreatedAt.Hour,
                    Date = x.r.CreatedAt.Date
                })
                .Select(g => new
                {
                    timestamp = g.Key.Date.AddHours(g.Key.Hour),
                    averageActivity = g.Average(x => (double)x.r.AverageActivityRate),
                    reportCount = g.Count()
                })
                .OrderBy(x => x.timestamp)
                .ToListAsync();

            return Ok(new
            {
                timeline,
                fromTime,
                toTime = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting activity timeline");
            return StatusCode(500, new { error = "Failed to get timeline" });
        }
    }

    /// <summary>
    /// Gets current activity level
    /// </summary>
    [HttpGet("current-activity")]
    public async Task<IActionResult> GetCurrentActivity([FromQuery] string? organizationId = null)
    {
        try
        {
            Guid orgId;
            if (string.IsNullOrEmpty(organizationId) || !Guid.TryParse(organizationId, out orgId))
            {
                var defaultOrg = await _context.Organizations.FirstAsync();
                orgId = defaultOrg.Id;
            }

            // Get average activity from reports in the last hour
            var lastHour = DateTime.UtcNow.AddHours(-1);

            var currentActivity = await _context.ActivityReports
                .Join(_context.Computers, r => r.ComputerId, c => c.Id, (r, c) => new { r, c })
                .Where(x => x.c.OrganizationId == orgId && x.r.CreatedAt >= lastHour)
                .AverageAsync(x => (double?)x.r.AverageActivityRate) ?? 0;

            return Ok(new
            {
                averageActivity = Math.Round(currentActivity, 1),
                timestamp = DateTime.UtcNow,
                timeRange = "Last hour"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current activity");
            return StatusCode(500, new { error = "Failed to get current activity" });
        }
    }

    /// <summary>
    /// Gets recent alerts
    /// </summary>
    [HttpGet("alerts")]
    public async Task<IActionResult> GetRecentAlerts([FromQuery] string? organizationId = null, [FromQuery] int limit = 10)
    {
        try
        {
            Guid orgId;
            if (string.IsNullOrEmpty(organizationId) || !Guid.TryParse(organizationId, out orgId))
            {
                var defaultOrg = await _context.Organizations.FirstAsync();
                orgId = defaultOrg.Id;
            }

            var alerts = await _context.Alerts
                .Include(a => a.Computer)
                .Where(a => a.OrganizationId == orgId)
                .OrderByDescending(a => a.CreatedAt)
                .Take(limit)
                .Select(a => new
                {
                    a.Id,
                    a.Type,
                    a.Severity,
                    a.Title,
                    a.Description,
                    a.IsAcknowledged,
                    a.CreatedAt,
                    Computer = new { a.Computer.Name, a.Computer.ComputerId },
                    a.SeverityIcon
                })
                .ToListAsync();

            return Ok(alerts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting alerts");
            return StatusCode(500, new { error = "Failed to get alerts" });
        }
    }

    /// <summary>
    /// Acknowledges an alert
    /// </summary>
    [HttpPut("alerts/{alertId}/acknowledge")]
    public async Task<IActionResult> AcknowledgeAlert(Guid alertId)
    {
        try
        {
            var alert = await _context.Alerts.FindAsync(alertId);
            
            if (alert == null)
            {
                return NotFound();
            }

            alert.IsAcknowledged = true;
            alert.AcknowledgedAt = DateTime.UtcNow;
            // TODO: Set AcknowledgedBy when authentication is implemented

            await _context.SaveChangesAsync();

            return Ok(new { success = true, acknowledgedAt = alert.AcknowledgedAt });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acknowledging alert {AlertId}", alertId);
            return StatusCode(500, new { error = "Failed to acknowledge alert" });
        }
    }
}