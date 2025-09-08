namespace Capturer.Api.DTOs;

/// <summary>
/// DTO para estado del sistema - usado en endpoint /api/v1/status
/// </summary>
public class SystemStatusDto
{
    public bool IsCapturing { get; set; }
    public DateTime? LastCaptureTime { get; set; }
    public long TotalScreenshots { get; set; }
    public ActivityReportDto? CurrentActivity { get; set; }
    public SystemInfoDto SystemInfo { get; set; } = new();
    public string Version { get; set; } = "4.0.0";
    public DateTime StatusTimestamp { get; set; } = DateTime.UtcNow;
}

public class SystemInfoDto
{
    public string ComputerName { get; set; } = Environment.MachineName;
    public string OperatingSystem { get; set; } = Environment.OSVersion.ToString();
    public string UserName { get; set; } = Environment.UserName;
    public int ProcessorCount { get; set; } = Environment.ProcessorCount;
    public long WorkingSetMemory { get; set; }
    public TimeSpan Uptime { get; set; }
    public List<ScreenInfoDto> AvailableScreens { get; set; } = new();
}

public class ScreenInfoDto
{
    public int Index { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public bool IsPrimary { get; set; }
    public string Resolution => $"{Width}x{Height}";
}