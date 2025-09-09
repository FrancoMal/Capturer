using Capturer.Models;

namespace Capturer.Services;

public interface IQuadrantSchedulerService : IDisposable
{
    bool IsRunning { get; }
    DateTime? NextScheduledRun { get; }
    
    Task StartAsync();
    Task StopAsync();
    Task<bool> UpdateScheduleAsync(ScheduledProcessing schedule);
    Task<bool> RunScheduledProcessingAsync();
    
    event EventHandler<ScheduledProcessingEventArgs>? ScheduledProcessingStarted;
    event EventHandler<ScheduledProcessingEventArgs>? ScheduledProcessingCompleted;
}

public class QuadrantSchedulerService : IQuadrantSchedulerService, IDisposable
{
    private readonly IQuadrantService _quadrantService;
    private readonly IConfigurationManager _configManager;
    private CapturerConfiguration _config = new();
    private System.Threading.Timer? _schedulerTimer;
    private readonly SemaphoreSlim _processingLock = new(1, 1);

    public bool IsRunning { get; private set; }
    public DateTime? NextScheduledRun => _config.QuadrantSystem.ScheduledProcessing.NextRun;

    public event EventHandler<ScheduledProcessingEventArgs>? ScheduledProcessingStarted;
    public event EventHandler<ScheduledProcessingEventArgs>? ScheduledProcessingCompleted;

    public QuadrantSchedulerService(IQuadrantService quadrantService, IConfigurationManager configManager)
    {
        _quadrantService = quadrantService;
        _configManager = configManager;
        LoadConfigurationAsync().ConfigureAwait(false);
    }

    private async Task LoadConfigurationAsync()
    {
        try
        {
            _config = await _configManager.LoadConfigurationAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading configuration for scheduler: {ex.Message}");
            _config = new CapturerConfiguration();
        }
    }

    public async Task StartAsync()
    {
        if (IsRunning) return;

        await LoadConfigurationAsync();
        
        if (!_config.QuadrantSystem.ScheduledProcessing.IsEnabled)
        {
            Console.WriteLine("Quadrant scheduled processing is disabled");
            return;
        }

        IsRunning = true;
        await ScheduleNextRun();
        
        Console.WriteLine($"Quadrant scheduler started. Next run: {NextScheduledRun:yyyy-MM-dd HH:mm:ss}");
    }

    public async Task StopAsync()
    {
        if (!IsRunning) return;

        IsRunning = false;
        
        if (_schedulerTimer != null)
        {
            await _schedulerTimer.DisposeAsync();
            _schedulerTimer = null;
        }
        
        Console.WriteLine("Quadrant scheduler stopped");
    }

    public async Task<bool> UpdateScheduleAsync(ScheduledProcessing schedule)
    {
        try
        {
            _config.QuadrantSystem.ScheduledProcessing = schedule;
            await _configManager.SaveConfigurationAsync(_config);
            
            // Restart scheduler with new settings if running
            if (IsRunning)
            {
                await StopAsync();
                await StartAsync();
            }
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating schedule: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> RunScheduledProcessingAsync()
    {
        if (!await _processingLock.WaitAsync(TimeSpan.FromMinutes(1)))
        {
            Console.WriteLine("Scheduled processing skipped - another processing is already running");
            return false;
        }

        try
        {
            var schedule = _config.QuadrantSystem.ScheduledProcessing;
            var (startDate, endDate) = schedule.GetProcessingDateRange();

            Console.WriteLine($"Starting scheduled quadrant processing: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
            
            // Fire started event
            ScheduledProcessingStarted?.Invoke(this, new ScheduledProcessingEventArgs
            {
                Schedule = schedule,
                StartDate = startDate,
                EndDate = endDate
            });

            // Run the processing
            var task = await _quadrantService.ProcessImagesAsync(
                startDate,
                endDate,
                schedule.ConfigurationName);

            // Record execution in schedule
            schedule.RecordExecution(task);
            await _configManager.SaveConfigurationAsync(_config);

            // Schedule next run
            await ScheduleNextRun();

            // Fire completed event
            ScheduledProcessingCompleted?.Invoke(this, new ScheduledProcessingEventArgs
            {
                Schedule = schedule,
                Task = task,
                Success = task.Status == ProcessingStatus.Completed,
                StartDate = startDate,
                EndDate = endDate
            });

            Console.WriteLine($"Scheduled processing completed: {task.GetSummary()}");
            return task.Status == ProcessingStatus.Completed;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during scheduled processing: {ex.Message}");
            
            // Fire completed event with error
            ScheduledProcessingCompleted?.Invoke(this, new ScheduledProcessingEventArgs
            {
                Schedule = _config.QuadrantSystem.ScheduledProcessing,
                Success = false,
                ErrorMessage = ex.Message,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now
            });
            
            return false;
        }
        finally
        {
            _processingLock.Release();
        }
    }

    private async Task ScheduleNextRun()
    {
        var schedule = _config.QuadrantSystem.ScheduledProcessing;
        
        if (!schedule.IsEnabled)
        {
            await StopAsync();
            return;
        }

        var nextRun = schedule.CalculateNextRun();
        schedule.NextRun = nextRun;
        await _configManager.SaveConfigurationAsync(_config);

        // Calculate delay until next run
        var delay = nextRun - DateTime.Now;
        if (delay <= TimeSpan.Zero)
        {
            // If the next run time has already passed, run immediately
            delay = TimeSpan.FromMinutes(1);
        }

        // Dispose existing timer
        if (_schedulerTimer != null)
        {
            await _schedulerTimer.DisposeAsync();
        }

        // Create new timer for next run
        _schedulerTimer = new System.Threading.Timer(
            async _ =>
            {
                if (IsRunning)
                {
                    await RunScheduledProcessingAsync();
                }
            },
            null,
            delay,
            Timeout.InfiniteTimeSpan); // Single execution

        Console.WriteLine($"Next quadrant processing scheduled for: {nextRun:yyyy-MM-dd HH:mm:ss} (in {delay.TotalHours:F1} hours)");
    }

    public void Dispose()
    {
        StopAsync().Wait(5000);
        _schedulerTimer?.Dispose();
        _processingLock?.Dispose();
    }
}

public class ScheduledProcessingEventArgs : EventArgs
{
    public ScheduledProcessing Schedule { get; set; } = new();
    public ProcessingTask? Task { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}