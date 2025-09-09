using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using CapturerDashboard.Data.Context;
using CapturerDashboard.Core.Models;
using CapturerDashboard.Web.Hubs;
using System.Text.Json;

namespace CapturerDashboard.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsApiController : ControllerBase
{
    private readonly DashboardDbContext _context;
    private readonly IHubContext<DashboardHub> _hubContext;
    private readonly ILogger<ReportsApiController> _logger;

    public ReportsApiController(
        DashboardDbContext context,
        IHubContext<DashboardHub> hubContext,
        ILogger<ReportsApiController> logger)
    {
        _context = context;
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Receives activity reports from Capturer Desktop clients
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ReceiveReport([FromBody] ActivityReportDto reportDto)
    {
        try
        {
            // Validate API key from header
            var apiKey = Request.Headers["X-API-Key"].FirstOrDefault();
            if (string.IsNullOrEmpty(apiKey))
            {
                return Unauthorized(new { error = "API Key is required" });
            }

            // Find computer by API key
            var computer = await _context.Computers
                .Include(c => c.Organization)
                .FirstOrDefaultAsync(c => c.ApiKey == apiKey && c.IsActive);

            if (computer == null)
            {
                return Unauthorized(new { error = "Invalid API key" });
            }

            // Update computer last seen
            computer.LastSeenAt = DateTime.UtcNow;
            computer.Status = "Online";

            // Parse report data
            var reportDate = DateOnly.Parse(reportDto.Report.Date);
            var startTime = TimeOnly.Parse(reportDto.Report.StartTime);
            var endTime = TimeOnly.Parse(reportDto.Report.EndTime);

            // Create activity report
            var activityReport = new ActivityReport
            {
                ComputerId = computer.Id,
                ReportDate = reportDate,
                StartTime = startTime,
                EndTime = endTime,
                ReportData = JsonSerializer.Serialize(reportDto),
                TotalQuadrants = reportDto.Report.Quadrants?.Count ?? 0,
                TotalComparisons = reportDto.Report.Quadrants?.Sum(q => q.Comparisons) ?? 0,
                TotalActivities = reportDto.Report.Quadrants?.Sum(q => q.Activities) ?? 0,
                AverageActivityRate = (decimal)(reportDto.Report.Quadrants?.Average(q => q.ActivityRate) ?? 0)
            };

            _context.ActivityReports.Add(activityReport);

            // Create quadrant activities
            if (reportDto.Report.Quadrants != null)
            {
                foreach (var quadrant in reportDto.Report.Quadrants)
                {
                    var quadrantActivity = new QuadrantActivity
                    {
                        ReportId = activityReport.Id,
                        QuadrantName = quadrant.Name,
                        TotalComparisons = quadrant.Comparisons,
                        ActivityDetectionCount = quadrant.Activities,
                        ActivityRate = (decimal)quadrant.ActivityRate,
                        Timeline = JsonSerializer.Serialize(quadrant.Timeline)
                    };

                    _context.QuadrantActivities.Add(quadrantActivity);
                }
            }

            // Check for alerts
            await CheckAndCreateAlerts(computer, activityReport);

            // Save changes
            await _context.SaveChangesAsync();

            // Notify connected clients via SignalR
            await _hubContext.Clients.Group($"org-{computer.OrganizationId}")
                .SendAsync("ReportReceived", new
                {
                    ComputerId = computer.Id,
                    ComputerName = computer.Name,
                    ReportId = activityReport.Id,
                    ActivityRate = activityReport.AverageActivityRate,
                    Timestamp = DateTime.UtcNow
                });

            _logger.LogInformation("Report received from {ComputerName} ({ComputerId})", 
                computer.Name, computer.ComputerId);

            return CreatedAtAction(
                nameof(GetReport), 
                new { id = activityReport.Id }, 
                new { 
                    reportId = activityReport.Id,
                    processed = true,
                    nextSyncTime = DateTime.UtcNow.AddMinutes(5) // Suggest next sync in 5 minutes
                });
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON in report data");
            return BadRequest(new { error = "Invalid report format" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing report");
            return StatusCode(500, new { error = "Failed to process report" });
        }
    }

    /// <summary>
    /// Gets a specific activity report by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetReport(Guid id)
    {
        var report = await _context.ActivityReports
            .Include(r => r.Computer)
            .Include(r => r.Quadrants)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (report == null)
        {
            return NotFound();
        }

        return Ok(new
        {
            report.Id,
            report.ReportDate,
            report.StartTime,
            report.EndTime,
            Computer = new { report.Computer.Name, report.Computer.ComputerId },
            report.TotalQuadrants,
            report.TotalComparisons,
            report.TotalActivities,
            report.AverageActivityRate,
            Quadrants = report.Quadrants.Select(q => new
            {
                q.QuadrantName,
                q.TotalComparisons,
                q.ActivityDetectionCount,
                q.ActivityRate
            }),
            report.CreatedAt
        });
    }

    private Task CheckAndCreateAlerts(Computer computer, ActivityReport report)
    {
        var alerts = new List<Alert>();

        // Low activity alert
        if (report.AverageActivityRate < 20)
        {
            alerts.Add(new Alert
            {
                Type = "LowActivity",
                Severity = "Info",
                Title = "Low Activity Detected",
                Description = $"Computer {computer.Name} shows low activity ({report.AverageActivityRate:F1}%)",
                ComputerId = computer.Id,
                OrganizationId = computer.OrganizationId,
                Metadata = JsonSerializer.Serialize(new { ActivityRate = report.AverageActivityRate })
            });
        }

        // High activity alert
        if (report.AverageActivityRate > 95)
        {
            alerts.Add(new Alert
            {
                Type = "HighActivity",
                Severity = "Info",
                Title = "High Activity Detected",
                Description = $"Computer {computer.Name} shows sustained high activity ({report.AverageActivityRate:F1}%)",
                ComputerId = computer.Id,
                OrganizationId = computer.OrganizationId,
                Metadata = JsonSerializer.Serialize(new { ActivityRate = report.AverageActivityRate })
            });
        }

        if (alerts.Any())
        {
            _context.Alerts.AddRange(alerts);
        }

        return Task.CompletedTask;
    }
}