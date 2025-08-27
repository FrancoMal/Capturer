namespace Capturer.Models;

public class ScreenshotInfo
{
    public string FileName { get; set; } = "";
    public string FullPath { get; set; } = "";
    public DateTime CaptureTime { get; set; }
    public long FileSize { get; set; }
    public bool EmailSent { get; set; } = false;
    public string ComputerName { get; set; } = Environment.MachineName;
    public List<string> EmailedTo { get; set; } = new();
    public DateTime? LastEmailDate { get; set; }
}

public class ScreenshotCapturedEventArgs : EventArgs
{
    public ScreenshotInfo Screenshot { get; set; } = null!;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

public class EmailSentEventArgs : EventArgs
{
    public List<string> Recipients { get; set; } = new();
    public int AttachmentCount { get; set; }
    public int FileCount { get; set; }
    public DateTime SentDate { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

public class ScheduledTask
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    public string Type { get; set; } = ""; // "Screenshot", "Email", "Cleanup"
    public DateTime NextExecution { get; set; }
    public TimeSpan Interval { get; set; }
    public bool IsActive { get; set; } = true;
    public Dictionary<string, object> Parameters { get; set; } = new();
}