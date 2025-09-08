namespace Capturer.Api.DTOs;

/// <summary>
/// DTO para reportes de actividad compartido entre Cliente y Dashboard Web
/// Basado en el plan v4.0 - Modelo de Datos Compartido
/// </summary>
public class ActivityReportDto
{
    public Guid Id { get; set; }
    public string ComputerId { get; set; } = string.Empty;
    public string ComputerName { get; set; } = string.Empty;
    public DateTime ReportDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public List<QuadrantActivityDto> QuadrantActivities { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string Version { get; set; } = "4.0.0";
}

public class QuadrantActivityDto
{
    public string QuadrantName { get; set; } = string.Empty;
    public long TotalComparisons { get; set; }
    public long ActivityDetectionCount { get; set; }
    public double ActivityRate { get; set; }
    public double AverageChangePercentage { get; set; }
    public TimeSpan ActiveDuration { get; set; }
    public List<ActivityTimelineEntry> Timeline { get; set; } = new();
}

public class ActivityTimelineEntry
{
    public DateTime Timestamp { get; set; }
    public double ActivityLevel { get; set; }
    public Dictionary<string, double> QuadrantLevels { get; set; } = new();
}