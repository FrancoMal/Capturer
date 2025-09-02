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
    
    // Privacy settings
    public bool EnablePrivacyBlur { get; set; } = false;
    public int BlurIntensity { get; set; } = 3; // 1-10 scale, 3 = light blur, 5 = medium, 8+ = heavy
    public BlurMode BlurMode { get; set; } = BlurMode.Gaussian;
    
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
    public SmtpSecurityMode SecurityMode { get; set; } = SmtpSecurityMode.StartTls;
    public string SenderName { get; set; } = "Capturer Screenshot App";
    public int ConnectionTimeout { get; set; } = 30;
    public FtpSettings FtpSettings { get; set; } = new();
    
    // Legacy compatibility
    public bool UseSSL
    {
        get => SecurityMode == SmtpSecurityMode.Ssl;
        set => SecurityMode = value ? SmtpSecurityMode.Ssl : SmtpSecurityMode.StartTls;
    }
    
    // Enhanced email options (shared between manual and auto)
    public bool UseZipFormat { get; set; } = true;
    public bool SendSeparateEmails { get; set; } = false;
    public SharedQuadrantSettings QuadrantSettings { get; set; } = new();
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
    
    // Configuración de filtros de horario
    public TimeSpan StartTime { get; set; } = TimeSpan.FromHours(8); // 8:00 AM
    public TimeSpan EndTime { get; set; } = TimeSpan.FromHours(23); // 11:00 PM
    public bool UseTimeFilter { get; set; } = false; // Por defecto desactivado
    public bool IncludeWeekends { get; set; } = true; // Incluir fines de semana
    
    // Configuración para días específicos de la semana
    public List<DayOfWeek> ActiveWeekDays { get; set; } = new()
    {
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, 
        DayOfWeek.Thursday, DayOfWeek.Friday
    };
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

public enum BlurMode
{
    Gaussian = 0,       // Gaussian blur (most common)
    Box = 1,           // Box blur (faster)
    Motion = 2         // Motion blur (for privacy)
}

public enum SmtpSecurityMode
{
    None = 0,          // Sin seguridad (puerto 25)
    StartTls = 1,      // STARTTLS (puertos 587, 25)
    Ssl = 2,           // SSL/TLS implícito (puerto 465)
    Auto = 3           // Detección automática
}

public enum FileDeliveryMode
{
    EmailOnly = 0,     // Solo por email
    FtpOnly = 1,       // Solo por FTP
    EmailAndFtp = 2    // Email + backup FTP
}

public class FtpSettings
{
    public bool Enabled { get; set; } = false;
    public string Server { get; set; } = "";
    public int Port { get; set; } = 21;
    public string Username { get; set; } = "";
    public string Password { get; set; } = ""; // Will be encrypted
    public string RemoteDirectory { get; set; } = "/uploads/capturer/";
    public bool UsePassiveMode { get; set; } = true;
    public bool UseSftp { get; set; } = false;
    public int TimeoutSeconds { get; set; } = 30;
    public FileDeliveryMode DeliveryMode { get; set; } = FileDeliveryMode.EmailOnly;
    public bool CompressBeforeUpload { get; set; } = true;
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
    
    /// <summary>
    /// Configuración para recordar preferencias de resolución
    /// </summary>
    public bool RememberResolutionChoice { get; set; } = false;
    public string ResolutionHandling { get; set; } = "ask"; // "ask", "auto-adjust", "keep-current"

    public QuadrantSystemSettings()
    {
        QuadrantsFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), 
            "Capturer", "Quadrants");
    }

    /// <summary>
    /// Gets the active configuration, automatically selecting a default if none is active
    /// </summary>
    public QuadrantConfiguration? GetActiveConfiguration()
    {
        // Buscar la configuración activa por nombre
        var activeConfig = Configurations.FirstOrDefault(c => c.Name == ActiveConfigurationName && c.IsActive);
        
        if (activeConfig != null)
            return activeConfig;
        
        // Si no hay configuración activa, buscar cualquier configuración activa
        activeConfig = Configurations.FirstOrDefault(c => c.IsActive);
        if (activeConfig != null)
        {
            ActiveConfigurationName = activeConfig.Name;
            return activeConfig;
        }
        
        // Si no hay ninguna configuración activa, activar la primera disponible
        var firstConfig = Configurations.FirstOrDefault();
        if (firstConfig != null)
        {
            firstConfig.IsActive = true;
            ActiveConfigurationName = firstConfig.Name;
            return firstConfig;
        }
        
        return null;
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

public class SharedQuadrantSettings
{
    // Shared settings for both manual and auto reports
    public bool UseQuadrantsInEmails { get; set; } = false;
    public List<string> SelectedQuadrants { get; set; } = new();
    public bool ProcessQuadrantsFirst { get; set; } = false;
    public string ProcessingProfile { get; set; } = "Default";
    public bool SendSeparateEmailPerQuadrant { get; set; } = false;
    
    // Legacy compatibility for routine emails
    public bool UseQuadrantsInRoutineEmails 
    { 
        get => UseQuadrantsInEmails; 
        set => UseQuadrantsInEmails = value; 
    }
}

/// <summary>
/// Representa un período de reporte con filtros específicos
/// </summary>
public class ReportPeriod
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public List<DayOfWeek> ActiveWeekDays { get; set; } = new();
    
    public static ReportPeriod GetDailyPeriod(ScheduleSettings settings)
    {
        var yesterday = DateTime.Now.Date.AddDays(-1);
        return new ReportPeriod
        {
            StartDate = yesterday,
            EndDate = yesterday.AddDays(1).AddSeconds(-1),
            StartTime = settings.UseTimeFilter ? settings.StartTime : null,
            EndTime = settings.UseTimeFilter ? settings.EndTime : null,
            ActiveWeekDays = settings.ActiveWeekDays
        };
    }
    
    public static ReportPeriod GetWeeklyPeriod(ScheduleSettings settings)
    {
        var today = DateTime.Now.Date;
        var startOfWeek = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
        var endOfWeek = startOfWeek.AddDays(6).AddDays(1).AddSeconds(-1);
        
        // Si hoy es lunes, tomar la semana anterior
        if (today.DayOfWeek == DayOfWeek.Monday)
        {
            startOfWeek = startOfWeek.AddDays(-7);
            endOfWeek = endOfWeek.AddDays(-7);
        }
        
        return new ReportPeriod
        {
            StartDate = startOfWeek,
            EndDate = endOfWeek,
            StartTime = settings.UseTimeFilter ? settings.StartTime : null,
            EndTime = settings.UseTimeFilter ? settings.EndTime : null,
            ActiveWeekDays = settings.ActiveWeekDays
        };
    }
    
    public static ReportPeriod GetMonthlyPeriod(ScheduleSettings settings)
    {
        var today = DateTime.Now.Date;
        var startOfMonth = new DateTime(today.Year, today.Month, 1).AddMonths(-1);
        var endOfMonth = startOfMonth.AddMonths(1).AddSeconds(-1);
        
        return new ReportPeriod
        {
            StartDate = startOfMonth,
            EndDate = endOfMonth,
            StartTime = settings.UseTimeFilter ? settings.StartTime : null,
            EndTime = settings.UseTimeFilter ? settings.EndTime : null,
            ActiveWeekDays = settings.ActiveWeekDays
        };
    }
    
    public static ReportPeriod GetCustomPeriod(ScheduleSettings settings)
    {
        var endDate = DateTime.Now.Date;
        var startDate = endDate.AddDays(-settings.CustomDays);
        
        return new ReportPeriod
        {
            StartDate = startDate,
            EndDate = endDate.AddDays(1).AddSeconds(-1),
            StartTime = settings.UseTimeFilter ? settings.StartTime : null,
            EndTime = settings.UseTimeFilter ? settings.EndTime : null,
            ActiveWeekDays = settings.ActiveWeekDays
        };
    }
}