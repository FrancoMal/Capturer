using Capturer.Models;

namespace Capturer.Services;

public interface ISchedulerService : IDisposable
{
    Task StartAsync();
    Task StopAsync();
    Task ScheduleScreenshotsAsync(TimeSpan interval);
    Task ScheduleAutomaticReportsAsync(ScheduleSettings scheduleSettings);
    Task ScheduleCleanupAsync(TimeSpan interval);
    Task CancelAllSchedulesAsync();
    List<ScheduledTask> GetActiveSchedules();
    bool IsRunning { get; }
}

public class SchedulerService : ISchedulerService
{
    private readonly IScreenshotService _screenshotService;
    private readonly IEmailService _emailService;
    private readonly IFileService _fileService;
    private readonly IConfigurationManager _configManager;
    private readonly IReportPeriodService _reportPeriodService;
    
    private System.Threading.Timer? _automaticReportTimer;
    private System.Threading.Timer? _cleanupTimer;
    private readonly List<ScheduledTask> _scheduledTasks = new();
    private bool _isRunning = false;
    private CapturerConfiguration _config = new();

    public bool IsRunning => _isRunning;

    public SchedulerService(
        IScreenshotService screenshotService, 
        IEmailService emailService,
        IFileService fileService,
        IConfigurationManager configManager,
        IReportPeriodService reportPeriodService)
    {
        _screenshotService = screenshotService;
        _emailService = emailService;
        _fileService = fileService;
        _configManager = configManager;
        _reportPeriodService = reportPeriodService;
    }

    public async Task StartAsync()
    {
        if (_isRunning) return;

        _config = await _configManager.LoadConfigurationAsync();
        _isRunning = true;

        // Start screenshot capturing if auto-start is enabled
        if (_config.Screenshot.AutoStartCapture)
        {
            await ScheduleScreenshotsAsync(_config.Screenshot.CaptureInterval);
        }

        // Schedule automatic reports if enabled
        if (_config.Schedule.EnableAutomaticReports)
        {
            await ScheduleAutomaticReportsAsync(_config.Schedule);
        }

        // Schedule cleanup if auto cleanup is enabled
        if (_config.Storage.AutoCleanup)
        {
            await ScheduleCleanupAsync(TimeSpan.FromHours(24)); // Daily cleanup check
        }

        Console.WriteLine("Scheduler service started successfully");
    }

    public async Task StopAsync()
    {
        if (!_isRunning) return;

        await CancelAllSchedulesAsync();
        await _screenshotService.StopAutomaticCaptureAsync();
        
        _isRunning = false;
        Console.WriteLine("Scheduler service stopped");
    }

    public async Task ScheduleScreenshotsAsync(TimeSpan interval)
    {
        // Remove existing screenshot schedule
        RemoveTaskByType("Screenshot");

        // Start screenshot service
        await _screenshotService.StartAutomaticCaptureAsync(interval);

        // Add to scheduled tasks for tracking
        var task = new ScheduledTask
        {
            Name = "Automatic Screenshot Capture",
            Type = "Screenshot",
            NextExecution = DateTime.Now.Add(interval),
            Interval = interval,
            Parameters = { ["interval"] = interval.TotalMinutes }
        };

        _scheduledTasks.Add(task);
        Console.WriteLine($"Screenshot capture scheduled every {interval.TotalMinutes} minutes");
    }

    public async Task ScheduleAutomaticReportsAsync(ScheduleSettings scheduleSettings)
    {
        // Remove existing automatic report schedule
        RemoveTaskByType("AutomaticReport");

        var nextExecution = _reportPeriodService.GetNextReportTime(scheduleSettings);
        var interval = GetReportInterval(scheduleSettings.Frequency, scheduleSettings.CustomDays);

        _automaticReportTimer = new System.Threading.Timer(async _ =>
        {
            if (_isRunning)
            {
                await SendAutomaticReportAsync();
                
                // Update next execution time
                var task = _scheduledTasks.FirstOrDefault(t => t.Type == "AutomaticReport");
                if (task != null)
                {
                    task.NextExecution = DateTime.Now.Add(interval);
                }
            }
        }, null, nextExecution.Subtract(DateTime.Now), interval);

        // Add to scheduled tasks for tracking
        var scheduledTask = new ScheduledTask
        {
            Name = $"Automatic Report - {scheduleSettings.Frequency} at {scheduleSettings.ReportTime:hh\\:mm}",
            Type = "AutomaticReport",
            NextExecution = nextExecution,
            Interval = interval,
            Parameters = 
            { 
                ["frequency"] = scheduleSettings.Frequency.ToString(), 
                ["time"] = scheduleSettings.ReportTime.ToString(),
                ["customDays"] = scheduleSettings.CustomDays.ToString()
            }
        };

        _scheduledTasks.Add(scheduledTask);
        Console.WriteLine($"Automatic reports scheduled for {scheduleSettings.Frequency} at {scheduleSettings.ReportTime:hh\\:mm}");
    }

    public async Task ScheduleCleanupAsync(TimeSpan interval)
    {
        // Remove existing cleanup schedule
        RemoveTaskByType("Cleanup");

        var nextExecution = DateTime.Now.Add(interval);

        _cleanupTimer = new System.Threading.Timer(async _ =>
        {
            if (_isRunning)
            {
                await PerformCleanupAsync();
                
                // Update next execution time
                var task = _scheduledTasks.FirstOrDefault(t => t.Type == "Cleanup");
                if (task != null)
                {
                    task.NextExecution = DateTime.Now.Add(interval);
                }
            }
        }, null, interval, interval);

        // Add to scheduled tasks for tracking
        var task = new ScheduledTask
        {
            Name = "Automatic File Cleanup",
            Type = "Cleanup",
            NextExecution = nextExecution,
            Interval = interval,
            Parameters = { ["intervalHours"] = interval.TotalHours }
        };

        _scheduledTasks.Add(task);
        Console.WriteLine($"File cleanup scheduled every {interval.TotalHours} hours");
    }

    public async Task CancelAllSchedulesAsync()
    {
        // Stop screenshot service
        await _screenshotService.StopAutomaticCaptureAsync();

        // Cancel timers
        if (_automaticReportTimer != null)
        {
            await _automaticReportTimer.DisposeAsync();
            _automaticReportTimer = null;
        }

        if (_cleanupTimer != null)
        {
            await _cleanupTimer.DisposeAsync();
            _cleanupTimer = null;
        }

        // Clear scheduled tasks
        _scheduledTasks.Clear();
        Console.WriteLine("All schedules cancelled");
    }

    public List<ScheduledTask> GetActiveSchedules()
    {
        return new List<ScheduledTask>(_scheduledTasks.Where(t => t.IsActive));
    }

    private void RemoveTaskByType(string taskType)
    {
        _scheduledTasks.RemoveAll(t => t.Type == taskType);
    }

    private DateTime GetNextWeeklyExecution(DayOfWeek targetDay, TimeSpan targetTime)
    {
        var now = DateTime.Now;
        var today = now.DayOfWeek;
        
        // Calculate days until target day
        var daysUntilTarget = ((int)targetDay - (int)today + 7) % 7;
        
        var targetDate = now.Date.AddDays(daysUntilTarget).Add(targetTime);
        
        // If target time has already passed today and it's the target day, schedule for next week
        if (daysUntilTarget == 0 && now.TimeOfDay > targetTime)
        {
            targetDate = targetDate.AddDays(7);
        }
        
        return targetDate;
    }

    private async Task SendAutomaticReportAsync()
    {
        const int MaxRetries = 3;
        const int RetryDelayMs = 5000;
        
        try
        {
            Console.WriteLine("[SCHEDULER] Starting automatic report generation...");
            var config = await _configManager.LoadConfigurationAsync();
            
            if (!config.Email.Recipients.Any())
            {
                Console.WriteLine("[SCHEDULER] ERROR: No recipients configured for automatic report");
                return;
            }

            // Calculate the appropriate period based on configuration
            var period = _reportPeriodService.GetCurrentReportPeriod(config.Schedule);
            
            Console.WriteLine($"[SCHEDULER] Generating {config.Schedule.Frequency} report for {period.StartDate:yyyy-MM-dd} to {period.EndDate:yyyy-MM-dd}");
            if (period.StartTime.HasValue && period.EndTime.HasValue)
            {
                Console.WriteLine($"[SCHEDULER] Time filter: {period.StartTime:hh\\:mm} to {period.EndTime:hh\\:mm}");
            }
            
            // Get filtered screenshots based on the period
            var screenshots = await _fileService.GetScreenshotsByReportPeriodAsync(period);
            Console.WriteLine($"[SCHEDULER] Found {screenshots.Count} screenshots matching the period criteria");
            
            if (!screenshots.Any())
            {
                Console.WriteLine($"[SCHEDULER] WARNING: No screenshots found for period, sending empty report");
            }
            
            bool success = false;
            Exception? lastException = null;
            
            // Retry mechanism for email sending
            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    Console.WriteLine($"[SCHEDULER] Attempt {attempt}/{MaxRetries} - Sending unified report...");
                    
                    var reportType = GetReportTypeString(config.Schedule.Frequency);
                    success = await _emailService.SendUnifiedReportAsync(
                        config.Email.Recipients,
                        period,
                        screenshots,
                        reportType,
                        config.Email.UseZipFormat);
                        
                    if (success)
                    {
                        Console.WriteLine($"[SCHEDULER] SUCCESS: {config.Schedule.Frequency} report sent successfully on attempt {attempt}");
                        Console.WriteLine($"[SCHEDULER] Report details: {screenshots.Count} files, {config.Email.Recipients.Count} recipients, quadrants: {config.Email.QuadrantSettings.UseQuadrantsInRoutineEmails}");
                        break;
                    }
                    else
                    {
                        Console.WriteLine($"[SCHEDULER] FAILED: Attempt {attempt} failed - no exception thrown but result was false");
                    }
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    Console.WriteLine($"[SCHEDULER] ERROR: Attempt {attempt} failed with exception: {ex.Message}");
                    
                    if (attempt < MaxRetries)
                    {
                        Console.WriteLine($"[SCHEDULER] Waiting {RetryDelayMs}ms before retry...");
                        await Task.Delay(RetryDelayMs);
                    }
                }
            }
            
            if (!success)
            {
                var errorMsg = lastException?.Message ?? "Unknown error - no exception details";
                Console.WriteLine($"[SCHEDULER] CRITICAL: All {MaxRetries} attempts failed. Last error: {errorMsg}");
                Console.WriteLine($"[SCHEDULER] Report details for debugging: Recipients={config.Email.Recipients.Count}, Screenshots={screenshots.Count}, SMTP={config.Email.SmtpServer}:{config.Email.SmtpPort}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SCHEDULER] FATAL ERROR in automatic report generation: {ex.Message}");
            Console.WriteLine($"[SCHEDULER] Stack trace: {ex.StackTrace}");
        }
    }

    private async Task PerformCleanupAsync()
    {
        try
        {
            Console.WriteLine("Starting scheduled cleanup...");
            await _fileService.CleanupOldFilesAsync();
            Console.WriteLine("Scheduled cleanup completed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during scheduled cleanup: {ex.Message}");
        }
    }

    private TimeSpan GetReportInterval(ReportFrequency frequency, int customDays)
    {
        return frequency switch
        {
            ReportFrequency.Daily => TimeSpan.FromDays(1),
            ReportFrequency.Weekly => TimeSpan.FromDays(7),
            ReportFrequency.Monthly => TimeSpan.FromDays(30),
            ReportFrequency.Custom => TimeSpan.FromDays(customDays),
            _ => TimeSpan.FromDays(7)
        };
    }

    private string GetReportTypeString(ReportFrequency frequency)
    {
        return frequency switch
        {
            ReportFrequency.Daily => "Reporte Diario Capturer",
            ReportFrequency.Weekly => "Reporte Semanal Capturer",
            ReportFrequency.Monthly => "Reporte Mensual Capturer",
            ReportFrequency.Custom => "Reporte Personalizado Capturer",
            _ => "Reporte Automático Capturer"
        };
    }

    public void Dispose()
    {
        StopAsync().Wait();
        _automaticReportTimer?.Dispose();
        _cleanupTimer?.Dispose();
    }
}