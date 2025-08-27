using System.Drawing.Imaging;

namespace Capturer.Models;

public class CapturerConfiguration
{
    public ScreenshotSettings Screenshot { get; set; } = new();
    public EmailSettings Email { get; set; } = new();
    public StorageSettings Storage { get; set; } = new();
    public ScheduleSettings Schedule { get; set; } = new();
    public ApplicationSettings Application { get; set; } = new();
    public QuadrantSystemSettings QuadrantSystem { get; set; } = new();
}

public class ScreenshotSettings
{
    public TimeSpan CaptureInterval { get; set; } = TimeSpan.FromMinutes(30);
    public bool AutoStartCapture { get; set; } = true;
    public string ImageFormat { get; set; } = "png";
    public int Quality { get; set; } = 90;
    public bool IncludeCursor { get; set; } = false;
    public ScreenCaptureMode CaptureMode { get; set; } = ScreenCaptureMode.AllScreens;
    public int SelectedScreenIndex { get; set; } = 0; // Index of specific screen when using SingleScreen mode
    public List<ScreenInfo> AvailableScreens { get; set; } = new(); // Runtime populated
    
    // Legacy support - will be migrated to CaptureMode
    public int ScreenIndex
    {
        get => CaptureMode == ScreenCaptureMode.AllScreens ? -1 : SelectedScreenIndex;
        set
        {
            if (value == -1)
            {
                CaptureMode = ScreenCaptureMode.AllScreens;
            }
            else
            {
                CaptureMode = ScreenCaptureMode.SingleScreen;
                SelectedScreenIndex = value;
            }
        }
    }
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

public enum ScreenCaptureMode
{
    AllScreens = 0,     // Capture all monitors as one large image
    SingleScreen = 1,   // Capture specific monitor
    PrimaryScreen = 2   // Capture primary monitor only
}

public class ScreenInfo
{
    public int Index { get; set; }
    public string DeviceName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public int Width { get; set; }
    public int Height { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public bool IsPrimary { get; set; }
    public string Resolution => $"{Width}x{Height}";
    public string Description => $"{DisplayName} ({Resolution})" + (IsPrimary ? " [Principal]" : "");
}

public class QuadrantSystemSettings
{
    public bool IsEnabled { get; set; } = false;
    public int MaxQuadrants { get; set; } = 36; // 6x6 grid max
    public string ActiveConfigurationName { get; set; } = "Default";
    public List<QuadrantConfiguration> Configurations { get; set; } = new();
    public ScheduledProcessing ScheduledProcessing { get; set; } = new();
    public string QuadrantsFolder { get; set; } = "Quadrants";
    public bool ShowPreviewColors { get; set; } = true;
    public bool EnableLogging { get; set; } = true;

    public QuadrantSystemSettings()
    {
        QuadrantsFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), 
            "Capturer", "Quadrants");
    }

    /// <summary>
    /// Gets the active configuration
    /// </summary>
    public QuadrantConfiguration? GetActiveConfiguration()
    {
        return Configurations.FirstOrDefault(c => c.Name == ActiveConfigurationName && c.IsActive);
    }

    /// <summary>
    /// Adds or updates a configuration
    /// </summary>
    public void AddOrUpdateConfiguration(QuadrantConfiguration configuration)
    {
        var existing = Configurations.FirstOrDefault(c => c.Name == configuration.Name);
        if (existing != null)
        {
            existing = configuration;
            existing.LastModified = DateTime.Now;
        }
        else
        {
            Configurations.Add(configuration);
        }
    }
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