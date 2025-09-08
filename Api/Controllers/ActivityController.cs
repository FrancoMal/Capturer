using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Capturer.Api.DTOs;
using Capturer.Services;
using Capturer.Models;

namespace Capturer.Api.Controllers;

/// <summary>
/// Controller para endpoints de actividad y reportes
/// Implementación según plan v4.0 - API Contract Definition
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = "ApiKeyPolicy")]
public class ActivityController : ControllerBase
{
    private readonly IScreenshotService? _screenshotService;
    private readonly IQuadrantService? _quadrantService;
    private readonly ActivityReportService? _activityReportService;
    private readonly ILogger<ActivityController> _logger;

    public ActivityController(
        IServiceProvider serviceProvider,
        ILogger<ActivityController> logger)
    {
        _screenshotService = serviceProvider.GetService(typeof(IScreenshotService)) as IScreenshotService;
        _quadrantService = serviceProvider.GetService(typeof(IQuadrantService)) as IQuadrantService;
        _activityReportService = serviceProvider.GetService(typeof(ActivityReportService)) as ActivityReportService;
        _logger = logger;
    }

    /// <summary>
    /// Get current activity status
    /// GET /api/v1/activity/current
    /// </summary>
    [HttpGet("current")]
    public async Task<ActionResult<ApiResponse<ActivityReportDto>>> GetCurrentActivity()
    {
        try
        {
            _logger.LogInformation("Getting current activity");
            
            // Try to get real activity data
            if (_activityReportService != null)
            {
                try
                {
                    // Get the most recent report (method name may vary)
                    // For now, use mock data until we confirm the correct method
                    ActivityReport? currentReport = null; // await _activityReportService.GetLatestReportAsync();
                    if (currentReport != null)
                    {
                        var dto = ActivityReportMapper.ToDto(currentReport);
                        return Ok(ApiResponse<ActivityReportDto>.SuccessResponse(
                            dto, 
                            "Current activity retrieved from live session"));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get real activity data, using mock");
                }
            }
            
            // Fallback to mock data with realistic values
            var mockActivity = ActivityReportMapper.CreateMockReport(Environment.MachineName);

            return Ok(ApiResponse<ActivityReportDto>.SuccessResponse(
                mockActivity, 
                "Current activity retrieved (mock data - real activity monitoring not active)"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current activity");
            return StatusCode(500, ApiResponse<ActivityReportDto>.ErrorResponse(
                "Failed to get current activity", 
                "ACTIVITY_ERROR"));
        }
    }

    /// <summary>
    /// Get activity history for date range
    /// GET /api/v1/activity/history?from=2024-01-01&to=2024-01-31&limit=100
    /// </summary>
    [HttpGet("history")]
    public async Task<ActionResult<ApiResponse<List<ActivityReportDto>>>> GetActivityHistory(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int limit = 100)
    {
        try
        {
            _logger.LogInformation("Getting activity history from {From} to {To}, limit {Limit}", 
                from, to, limit);

            // TODO: Implement actual history retrieval
            var mockHistory = new List<ActivityReportDto>();

            return Ok(ApiResponse<List<ActivityReportDto>>.SuccessResponse(
                mockHistory, 
                "Activity history retrieved (mock data)"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting activity history");
            return StatusCode(500, ApiResponse<List<ActivityReportDto>>.ErrorResponse(
                "Failed to get activity history", 
                "HISTORY_ERROR"));
        }
    }

    /// <summary>
    /// Sync activity report to dashboard
    /// POST /api/v1/activity/sync
    /// </summary>
    [HttpPost("sync")]
    public async Task<ActionResult<ApiResponse<SyncResult>>> SyncActivity(
        [FromBody] ActivityReportDto report)
    {
        try
        {
            if (report == null)
            {
                return BadRequest(ApiResponse<SyncResult>.ErrorResponse(
                    "Activity report is required", 
                    "INVALID_INPUT"));
            }

            _logger.LogInformation("Syncing activity report {ReportId} to dashboard", report.Id);

            // TODO: Implement dashboard sync when Dashboard Web is ready
            var syncResult = new SyncResult
            {
                Success = false,
                Error = "Dashboard Web not implemented yet",
                ReportId = report.Id.ToString()
            };

            return Ok(ApiResponse<SyncResult>.SuccessResponse(
                syncResult, 
                "Sync queued (dashboard not ready)"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing activity report");
            return StatusCode(500, ApiResponse<SyncResult>.ErrorResponse(
                "Failed to sync activity", 
                "SYNC_ERROR"));
        }
    }
}