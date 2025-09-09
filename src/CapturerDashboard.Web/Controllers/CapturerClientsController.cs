using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using CapturerDashboard.Data.Context;
using CapturerDashboard.Web.Services;

namespace CapturerDashboard.Web.Controllers;

[ApiController]
[Route("api/capturer-clients")]
[Authorize]
public class CapturerClientsController : ControllerBase
{
    private readonly DashboardDbContext _context;
    private readonly ICapturerClientService _capturerClient;
    private readonly ILogger<CapturerClientsController> _logger;

    public CapturerClientsController(
        DashboardDbContext context,
        ICapturerClientService capturerClient,
        ILogger<CapturerClientsController> logger)
    {
        _context = context;
        _capturerClient = capturerClient;
        _logger = logger;
    }

    /// <summary>
    /// Gets real-time status of all computers for current organization
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetAllComputersStatus()
    {
        try
        {
            var organizationId = GetCurrentUserOrganizationId();
            var statuses = await _capturerClient.GetAllComputersStatusAsync(organizationId);

            var result = statuses.Select(status => new
            {
                computer = new
                {
                    status.Computer.Id,
                    status.Computer.ComputerId,
                    status.Computer.Name,
                    status.Computer.Status,
                    status.Computer.LastSeenAt,
                    status.Computer.IsOnline
                },
                connection = new
                {
                    isReachable = status.IsReachable,
                    lastResponseTime = status.LastResponseTime?.TotalMilliseconds,
                    errorMessage = status.ErrorMessage
                },
                systemInfo = status.SystemStatus?.SystemInfo != null ? new
                {
                    status.SystemStatus.SystemInfo.ComputerName,
                    status.SystemStatus.SystemInfo.OperatingSystem,
                    status.SystemStatus.SystemInfo.UserName,
                    status.SystemStatus.SystemInfo.ProcessorCount,
                    status.SystemStatus.SystemInfo.WorkingSetMemory,
                    status.SystemStatus.SystemInfo.Uptime,
                    screenCount = status.SystemStatus.SystemInfo.AvailableScreens.Count
                } : null,
                captureInfo = status.SystemStatus != null ? new
                {
                    status.SystemStatus.IsCapturing,
                    status.SystemStatus.LastCaptureTime,
                    status.SystemStatus.TotalScreenshots
                } : null
            }).ToList();

            return Ok(new
            {
                totalComputers = statuses.Count,
                onlineComputers = statuses.Count(s => s.IsReachable),
                offlineComputers = statuses.Count(s => !s.IsReachable),
                computers = result,
                lastUpdated = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get computers status");
            return StatusCode(500, new { error = "Failed to get computers status" });
        }
    }

    /// <summary>
    /// Gets detailed status for a specific computer
    /// </summary>
    [HttpGet("{computerId}/status")]
    public async Task<IActionResult> GetComputerStatus(Guid computerId)
    {
        try
        {
            var organizationId = GetCurrentUserOrganizationId();
            var computer = await _context.Computers
                .FirstOrDefaultAsync(c => c.Id == computerId && c.OrganizationId == organizationId);

            if (computer == null)
            {
                return NotFound();
            }

            var health = await _capturerClient.CheckCapturerHealthAsync(computer);
            var status = health.IsHealthy ? await _capturerClient.GetCapturerStatusAsync(computer) : null;
            var activity = health.IsHealthy ? await _capturerClient.GetCapturerActivityAsync(computer) : null;

            return Ok(new
            {
                computer = new
                {
                    computer.Id,
                    computer.ComputerId,
                    computer.Name,
                    computer.Status,
                    computer.LastSeenAt,
                    computer.IsOnline
                },
                health = new
                {
                    health.IsHealthy,
                    health.ResponseTime?.TotalMilliseconds,
                    health.Version,
                    health.ErrorMessage
                },
                systemStatus = status?.Data,
                currentActivity = activity?.Data,
                lastChecked = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get computer status for {ComputerId}", computerId);
            return StatusCode(500, new { error = "Failed to get computer status" });
        }
    }

    /// <summary>
    /// Triggers screenshot capture on a specific computer
    /// </summary>
    [HttpPost("{computerId}/capture")]
    public async Task<IActionResult> TriggerCapture(Guid computerId)
    {
        try
        {
            var organizationId = GetCurrentUserOrganizationId();
            var computer = await _context.Computers
                .FirstOrDefaultAsync(c => c.Id == computerId && c.OrganizationId == organizationId);

            if (computer == null)
            {
                return NotFound();
            }

            var success = await _capturerClient.TriggerCapturerCaptureAsync(computer);

            if (success)
            {
                _logger.LogInformation("Capture triggered for computer {ComputerName} by user {UserId}",
                    computer.Name, GetCurrentUserId());

                return Ok(new
                {
                    success = true,
                    message = $"Screenshot capture triggered on {computer.Name}",
                    timestamp = DateTime.UtcNow
                });
            }
            else
            {
                return BadRequest(new { error = "Failed to trigger capture" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering capture for computer {ComputerId}", computerId);
            return StatusCode(500, new { error = "Failed to trigger capture" });
        }
    }

    /// <summary>
    /// Syncs data from all computers in organization
    /// </summary>
    [HttpPost("sync")]
    public async Task<IActionResult> SyncAllComputers()
    {
        try
        {
            var organizationId = GetCurrentUserOrganizationId();
            
            // Start sync in background
            _ = Task.Run(async () =>
            {
                try
                {
                    await _capturerClient.SyncAllComputersDataAsync(organizationId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Background sync failed for organization {OrganizationId}", organizationId);
                }
            });

            return Accepted(new
            {
                message = "Data synchronization started in background",
                organizationId,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start sync for organization");
            return StatusCode(500, new { error = "Failed to start sync" });
        }
    }

    /// <summary>
    /// Gets activity data for a specific computer
    /// </summary>
    [HttpGet("{computerId}/activity")]
    public async Task<IActionResult> GetComputerActivity(
        Guid computerId, 
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var organizationId = GetCurrentUserOrganizationId();
            var computer = await _context.Computers
                .FirstOrDefaultAsync(c => c.Id == computerId && c.OrganizationId == organizationId);

            if (computer == null)
            {
                return NotFound();
            }

            // Get activity from our database (historical)
            var from = fromDate ?? DateTime.Today;
            var to = toDate ?? DateTime.Today.AddDays(1);

            var historicalActivity = await _context.ActivityReports
                .Where(r => r.ComputerId == computer.Id && 
                           r.ReportDate >= DateOnly.FromDateTime(from) && 
                           r.ReportDate < DateOnly.FromDateTime(to))
                .OrderByDescending(r => r.ReportDate)
                .Select(r => new
                {
                    r.Id,
                    r.ReportDate,
                    r.StartTime,
                    r.EndTime,
                    r.TotalQuadrants,
                    r.TotalComparisons,
                    r.TotalActivities,
                    r.AverageActivityRate,
                    r.CreatedAt
                })
                .ToListAsync();

            // Also try to get current activity from Capturer API
            var currentActivity = await _capturerClient.GetCapturerActivityAsync(computer);

            return Ok(new
            {
                computer = new { computer.Name, computer.ComputerId },
                historical = historicalActivity,
                current = currentActivity.Success ? currentActivity.Data : null,
                dateRange = new { from, to },
                lastUpdated = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get activity for computer {ComputerId}", computerId);
            return StatusCode(500, new { error = "Failed to get computer activity" });
        }
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        throw new InvalidOperationException("User ID not found in claims");
    }

    private Guid GetCurrentUserOrganizationId()
    {
        var orgIdClaim = User.FindFirst("OrganizationId")?.Value;
        if (Guid.TryParse(orgIdClaim, out var orgId))
        {
            return orgId;
        }
        throw new InvalidOperationException("Organization ID not found in claims");
    }
}