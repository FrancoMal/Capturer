namespace Capturer.Models;

public class ScheduledProcessing
{
    public bool IsEnabled { get; set; } = false;
    public ProcessingFrequency Frequency { get; set; } = ProcessingFrequency.Daily;
    public int CustomDays { get; set; } = 1;
    public TimeSpan ProcessingTime { get; set; } = TimeSpan.FromHours(2); // 2:00 AM default
    public DateTime? LastRun { get; set; }
    public DateTime? NextRun { get; set; }
    public string ConfigurationName { get; set; } = "Default";
    public ProcessingDateRange DateRange { get; set; } = ProcessingDateRange.LastWeek;
    public int CustomDaysBack { get; set; } = 7;
    public List<ProcessingLogEntry> ExecutionHistory { get; set; } = new();

    /// <summary>
    /// Calculates the next run time based on current settings
    /// </summary>
    public DateTime CalculateNextRun()
    {
        var baseTime = DateTime.Today.Add(ProcessingTime);
        
        // If today's time has passed, start from tomorrow
        if (DateTime.Now > baseTime)
        {
            baseTime = baseTime.AddDays(1);
        }

        return Frequency switch
        {
            ProcessingFrequency.Daily => baseTime,
            ProcessingFrequency.Weekly => baseTime.AddDays(7 - (int)baseTime.DayOfWeek), // Next Monday
            ProcessingFrequency.Monthly => new DateTime(baseTime.Year, baseTime.Month, 1).AddMonths(1).Add(ProcessingTime),
            ProcessingFrequency.Custom => baseTime.AddDays(CustomDays - 1),
            _ => baseTime
        };
    }

    /// <summary>
    /// Updates the execution history after a run
    /// </summary>
    public void RecordExecution(ProcessingTask task)
    {
        LastRun = DateTime.Now;
        NextRun = CalculateNextRun();
        
        ExecutionHistory.Add(new ProcessingLogEntry
        {
            ExecutionTime = LastRun.Value,
            Success = task.Status == ProcessingStatus.Completed,
            FilesProcessed = task.ProcessedFiles,
            FilesSkipped = task.SkippedFiles,
            FilesErrored = task.FailedFiles,
            Duration = task.ProcessingTime,
            ErrorMessage = task.Status == ProcessingStatus.Failed ? string.Join("; ", task.ErrorMessages) : null
        });

        // Keep only last 50 executions
        if (ExecutionHistory.Count > 50)
        {
            ExecutionHistory.RemoveAt(0);
        }
    }

    /// <summary>
    /// Gets the date range for processing based on settings
    /// </summary>
    public (DateTime start, DateTime end) GetProcessingDateRange()
    {
        var end = DateTime.Now.Date;
        var start = DateRange switch
        {
            ProcessingDateRange.Today => end,
            ProcessingDateRange.LastWeek => end.AddDays(-7),
            ProcessingDateRange.LastMonth => end.AddDays(-30),
            ProcessingDateRange.Custom => end.AddDays(-CustomDaysBack),
            _ => end.AddDays(-7)
        };
        
        return (start, end.AddDays(1).AddTicks(-1)); // End of day
    }
}

public enum ProcessingFrequency
{
    Daily = 1,
    Weekly = 7,
    Monthly = 30,
    Custom = 0
}

public enum ProcessingDateRange
{
    Today,
    LastWeek,
    LastMonth,
    Custom
}

public class ProcessingLogEntry
{
    public DateTime ExecutionTime { get; set; }
    public bool Success { get; set; }
    public int FilesProcessed { get; set; }
    public int FilesSkipped { get; set; }
    public int FilesErrored { get; set; }
    public TimeSpan Duration { get; set; }
    public string? ErrorMessage { get; set; }
}