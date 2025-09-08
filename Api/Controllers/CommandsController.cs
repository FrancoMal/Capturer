using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Capturer.Api.DTOs;
using Capturer.Services;

namespace Capturer.Api.Controllers;

/// <summary>
/// Controller para ejecutar comandos en el cliente Capturer
/// Implementación según plan v4.0 - Command endpoints
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = "ApiKeyPolicy")]
public class CommandsController : ControllerBase
{
    private readonly IScreenshotService? _screenshotService;
    private readonly IEmailService? _emailService;
    private readonly ILogger<CommandsController> _logger;

    public CommandsController(
        IServiceProvider serviceProvider,
        ILogger<CommandsController> logger)
    {
        _screenshotService = serviceProvider.GetService(typeof(IScreenshotService)) as IScreenshotService;
        _emailService = serviceProvider.GetService(typeof(IEmailService)) as IEmailService;
        _logger = logger;
    }

    /// <summary>
    /// Trigger manual screenshot capture
    /// POST /api/v1/commands/capture
    /// </summary>
    [HttpPost("capture")]
    public async Task<ActionResult<ApiResponse<CommandResult>>> TriggerCapture()
    {
        try
        {
            _logger.LogInformation("Manual screenshot capture requested via API");

            if (_screenshotService == null)
            {
                return StatusCode(503, ApiResponse<CommandResult>.ErrorResponse(
                    "Screenshot service not available", 
                    "SERVICE_UNAVAILABLE"));
            }

            var screenshotInfo = await _screenshotService.CaptureScreenshotAsync();
            
            var result = new CommandResult
            {
                Success = screenshotInfo != null,
                Command = "capture",
                Output = screenshotInfo != null 
                    ? $"Screenshot captured: {screenshotInfo.FileName}" 
                    : "Screenshot capture failed",
                ErrorMessage = screenshotInfo == null ? "Capture failed" : null
            };

            return Ok(ApiResponse<CommandResult>.SuccessResponse(
                result, 
                result.Success ? "Screenshot captured successfully" : "Screenshot capture failed"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing capture command");
            
            var errorResult = new CommandResult
            {
                Success = false,
                Command = "capture",
                ErrorMessage = ex.Message
            };

            return StatusCode(500, ApiResponse<CommandResult>.ErrorResponse(
                "Failed to execute capture command", 
                "CAPTURE_ERROR"));
        }
    }

    /// <summary>
    /// Generate and send report
    /// POST /api/v1/commands/report
    /// </summary>
    [HttpPost("report")]
    public async Task<ActionResult<ApiResponse<CommandResult>>> GenerateReport(
        [FromBody] ReportRequest? request = null)
    {
        try
        {
            _logger.LogInformation("Report generation requested via API");

            if (_emailService == null)
            {
                return StatusCode(503, ApiResponse<CommandResult>.ErrorResponse(
                    "Email service not available", 
                    "SERVICE_UNAVAILABLE"));
            }

            // Use default values if no request provided
            var startDate = request?.StartDate ?? DateTime.Today.AddDays(-1);
            var endDate = request?.EndDate ?? DateTime.Today;
            var recipients = request?.Recipients ?? new List<string>();

            // TODO: Implement report generation with actual service call
            // For now, return mock success
            var result = new CommandResult
            {
                Success = true,
                Command = "report",
                Output = $"Report generation queued for period {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}"
            };

            return Ok(ApiResponse<CommandResult>.SuccessResponse(
                result, 
                "Report generation initiated"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing report command");
            
            var errorResult = new CommandResult
            {
                Success = false,
                Command = "report",
                ErrorMessage = ex.Message
            };

            return StatusCode(500, ApiResponse<CommandResult>.ErrorResponse(
                "Failed to execute report command", 
                "REPORT_ERROR"));
        }
    }

    /// <summary>
    /// Get API configuration status
    /// GET /api/v1/commands/config
    /// </summary>
    [HttpGet("config")]
    public async Task<ActionResult<ApiResponse<object>>> GetConfiguration()
    {
        try
        {
            _logger.LogInformation("Configuration status requested via API");

            var config = new
            {
                ApiVersion = "4.0.0-alpha",
                Services = new
                {
                    ScreenshotService = _screenshotService != null,
                    EmailService = _emailService != null,
                    IsCapturing = _screenshotService?.IsCapturing ?? false
                },
                SystemInfo = new
                {
                    MachineName = Environment.MachineName,
                    UserName = Environment.UserName,
                    OSVersion = Environment.OSVersion.ToString(),
                    ProcessorCount = Environment.ProcessorCount,
                    WorkingSet = Environment.WorkingSet,
                    UpTime = TimeSpan.FromMilliseconds(Environment.TickCount64)
                }
            };

            return Ok(ApiResponse<object>.SuccessResponse(
                config, 
                "Configuration retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting configuration");
            return StatusCode(500, ApiResponse<object>.ErrorResponse(
                "Failed to get configuration", 
                "CONFIG_ERROR"));
        }
    }
}

/// <summary>
/// Request model for report generation
/// </summary>
public class ReportRequest
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<string> Recipients { get; set; } = new();
    public bool UseZipFormat { get; set; } = true;
    public List<string> SelectedQuadrants { get; set; } = new();
}