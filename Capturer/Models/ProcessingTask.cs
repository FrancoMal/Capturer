namespace Capturer.Models;

public class ProcessingTask
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public ProcessingStatus Status { get; set; } = ProcessingStatus.Pending;
    public int TotalFiles { get; set; }
    public int ProcessedFiles { get; set; }
    public int SkippedFiles { get; set; }
    public int FailedFiles { get; set; }
    public List<string> ErrorMessages { get; set; } = new();
    public List<string> SkippedFileNames { get; set; } = new();
    public DateTime DateRangeStart { get; set; }
    public DateTime DateRangeEnd { get; set; }
    public string ConfigurationUsed { get; set; } = "";
    public bool WasCancelled { get; set; } = false;

    public ProcessingTask()
    {
        StartTime = DateTime.Now;
    }

    public ProcessingTask(DateTime dateStart, DateTime dateEnd, string configName)
    {
        StartTime = DateTime.Now;
        DateRangeStart = dateStart;
        DateRangeEnd = dateEnd;
        ConfigurationUsed = configName;
    }

    /// <summary>
    /// Gets the processing progress as percentage (0-100)
    /// </summary>
    public int ProgressPercentage
    {
        get
        {
            if (TotalFiles <= 0) return 0;
            return (int)((double)(ProcessedFiles + SkippedFiles + FailedFiles) / TotalFiles * 100);
        }
    }

    /// <summary>
    /// Gets the total processing time
    /// </summary>
    public TimeSpan ProcessingTime
    {
        get
        {
            var endTime = EndTime ?? DateTime.Now;
            return endTime - StartTime;
        }
    }

    /// <summary>
    /// Marks the task as completed
    /// </summary>
    public void Complete()
    {
        EndTime = DateTime.Now;
        Status = WasCancelled ? ProcessingStatus.Cancelled : ProcessingStatus.Completed;
    }

    /// <summary>
    /// Marks the task as failed
    /// </summary>
    public void Fail(string errorMessage)
    {
        EndTime = DateTime.Now;
        Status = ProcessingStatus.Failed;
        ErrorMessages.Add($"{DateTime.Now:HH:mm:ss}: {errorMessage}");
    }

    /// <summary>
    /// Adds an error without failing the entire task
    /// </summary>
    public void AddError(string fileName, string errorMessage)
    {
        FailedFiles++;
        ErrorMessages.Add($"{DateTime.Now:HH:mm:ss}: {fileName} - {errorMessage}");
    }

    /// <summary>
    /// Marks a file as skipped
    /// </summary>
    public void SkipFile(string fileName, string reason)
    {
        SkippedFiles++;
        SkippedFileNames.Add($"{fileName} - {reason}");
    }

    /// <summary>
    /// Increments processed files counter
    /// </summary>
    public void ProcessFile()
    {
        ProcessedFiles++;
    }

    /// <summary>
    /// Gets a summary string of the processing results
    /// </summary>
    public string GetSummary()
    {
        var duration = ProcessingTime;
        return $"Procesados: {ProcessedFiles}, Omitidos: {SkippedFiles}, Errores: {FailedFiles} " +
               $"(Total: {TotalFiles}) en {duration.TotalSeconds:F1}s";
    }
}

public enum ProcessingStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled
}

public class ProcessingProgress
{
    public int CurrentFile { get; set; }
    public int TotalFiles { get; set; }
    public string CurrentFileName { get; set; } = "";
    public string CurrentOperation { get; set; } = "";
    public int ProgressPercentage => TotalFiles > 0 ? (int)((double)CurrentFile / TotalFiles * 100) : 0;
}