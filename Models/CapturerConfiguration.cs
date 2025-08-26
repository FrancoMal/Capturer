using System.Drawing.Imaging;

namespace Capturer.Models;

public class CapturerConfiguration
{
    public ScreenshotSettings Screenshot { get; set; } = new();
    public EmailSettings Email { get; set; } = new();
    public StorageSettings Storage { get; set; } = new();
    public ScheduleSettings Schedule { get; set; } = new();
    public ApplicationSettings Application { get; set; } = new();
}

public class ScreenshotSettings
{
    public TimeSpan CaptureInterval { get; set; } = TimeSpan.FromMinutes(30);
    public bool AutoStartCapture { get; set; } = true;
    public string ImageFormat { get; set; } = "png";
    public int Quality { get; set; } = 90;
    public bool IncludeCursor { get; set; } = false;
    public int ScreenIndex { get; set; } = -1; // -1 = all screens
}

public class EmailSettings
{
    public string SmtpServer { get; set; } = "smtp.gmail.com";
    public int SmtpPort { get; set; } = 587;
    public string Username { get; set; } = "";
    public string Password { get; set; } = ""; // Will be encrypted
    public List<string> Recipients { get; set; } = new();
    public bool EnableWeeklyReports { get; set; } = true;
    public bool UseSSL { get; set; } = true;
    public string SenderName { get; set; } = "Capturer Screenshot App";
}

public class StorageSettings
{
    public string ScreenshotFolder { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Capturer", "Screenshots");
    public TimeSpan RetentionPeriod { get; set; } = TimeSpan.FromDays(90);
    public long MaxFolderSizeBytes { get; set; } = 5_000_000_000; // 5GB
    public bool AutoCleanup { get; set; } = true;
    public bool CreateDateFolders { get; set; } = false;
}

public class ScheduleSettings
{
    public ReportFrequency Frequency { get; set; } = ReportFrequency.Weekly;
    public int CustomDays { get; set; } = 7; // For custom frequency
    public DayOfWeek WeeklyReportDay { get; set; } = DayOfWeek.Monday;
    public TimeSpan ReportTime { get; set; } = TimeSpan.FromHours(9); // 9:00 AM
    public bool EnableAutomaticReports { get; set; } = true;
}

public enum ReportFrequency
{
    Daily = 1,
    Weekly = 7,
    Monthly = 30,
    Custom = 0
}

public class ApplicationSettings
{
    public bool StartWithWindows { get; set; } = false;
    public bool StartMinimized { get; set; } = true;
    public bool MinimizeToTray { get; set; } = true;
    public bool ShowNotifications { get; set; } = true;
    public string Language { get; set; } = "es-ES";
    public bool AutoCheckUpdates { get; set; } = true;
}