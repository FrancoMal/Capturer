using Capturer.Models;

namespace Capturer.Services;

public interface ISchedulerService : IDisposable
{
    Task StartAsync();
    Task StopAsync();
    Task ScheduleScreenshotsAsync(TimeSpan interval);
    Task ScheduleWeeklyEmailsAsync(DayOfWeek day, TimeSpan time);
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
    
    private System.Threading.Timer? _weeklyEmailTimer;
    private System.Threading.Timer? _cleanupTimer;
    private readonly List<ScheduledTask> _scheduledTasks = new();
    private bool _isRunning = false;
    private CapturerConfiguration _config = new();

    public bool IsRunning => _isRunning;

    public SchedulerService(
        IScreenshotService screenshotService, 
        IEmailService emailService,
        IFileService fileService,
        IConfigurationManager configManager)
    {
        _screenshotService = screenshotService;
        _emailService = emailService;
        _fileService = fileService;
        _configManager = configManager;
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
            await ScheduleWeeklyEmailsAsync(_config.Schedule.WeeklyReportDay, _config.Schedule.ReportTime);
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

    public async Task ScheduleWeeklyEmailsAsync(DayOfWeek day, TimeSpan time)
    {
        // Remove existing weekly email schedule
        RemoveTaskByType("WeeklyEmail");

        var nextExecution = GetNextWeeklyExecution(day, time);
        var interval = TimeSpan.FromDays(7);

        _weeklyEmailTimer = new System.Threading.Timer(async _ =>
        {
            if (_isRunning)
            {
                await SendWeeklyReportAsync();
                
                // Update next execution time
                var task = _scheduledTasks.FirstOrDefault(t => t.Type == "WeeklyEmail");
                if (task != null)
                {
                    task.NextExecution = DateTime.Now.Add(interval);
                }
            }
        }, null, nextExecution.Subtract(DateTime.Now), interval);

        // Add to scheduled tasks for tracking
        var scheduledTask = new ScheduledTask
        {
            Name = $"Weekly Email Report - {day} {time:hh\\:mm}",
            Type = "WeeklyEmail",
            NextExecution = nextExecution,
            Interval = interval,
            Parameters = 
            { 
                ["day"] = day.ToString(), 
                ["time"] = time.ToString() 
            }
        };

        _scheduledTasks.Add(scheduledTask);
        Console.WriteLine($"Weekly email reports scheduled for {day} at {time:hh\\:mm}");
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
        if (_weeklyEmailTimer != null)
        {
            await _weeklyEmailTimer.DisposeAsync();
            _weeklyEmailTimer = null;
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

    private async Task SendWeeklyReportAsync()
    {
        try
        {
            var config = await _configManager.LoadConfigurationAsync();
            
            if (!config.Email.Recipients.Any())
            {
                Console.WriteLine("No recipients configured for weekly email");
                return;
            }

            // Calculate date range for the past week
            var endDate = DateTime.Now.Date;
            var startDate = endDate.AddDays(-7);

            Console.WriteLine($"Sending weekly report for {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
            
            var success = await _emailService.SendWeeklyReportAsync(config.Email.Recipients, startDate, endDate);
            
            if (success)
            {
                Console.WriteLine("Weekly report sent successfully");
            }
            else
            {
                Console.WriteLine("Failed to send weekly report");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending weekly report: {ex.Message}");
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

    public void Dispose()
    {
        StopAsync().Wait();
        _weeklyEmailTimer?.Dispose();
        _cleanupTimer?.Dispose();
    }
}