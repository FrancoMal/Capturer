using System.ComponentModel;

namespace Capturer.Models;

/// <summary>
/// Modelo de reporte de actividad para persistencia y exportación
/// </summary>
public class ActivityReport
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime ReportStartTime { get; set; }
    public DateTime ReportEndTime { get; set; }
    public DateTime SessionStartTime { get; set; } = DateTime.Now; // When monitoring session started
    public string SessionName { get; set; } = ""; // Human-readable session name
    public string QuadrantConfigurationName { get; set; } = "";
    public List<ActivityReportEntry> Entries { get; set; } = new();
    public ActivityReportSummary Summary { get; set; } = new();
    public string ReportType { get; set; } = "Manual"; // Manual, Scheduled, Export, LiveSession
    public string Comments { get; set; } = "";
    public bool IsActive { get; set; } = true; // Whether session is currently active

    /// <summary>
    /// Duración total del reporte
    /// </summary>
    public TimeSpan ReportDuration => ReportEndTime - ReportStartTime;

    /// <summary>
    /// Genera un resumen automático basado en las entradas
    /// </summary>
    public void GenerateSummary()
    {
        Summary = new ActivityReportSummary
        {
            TotalQuadrants = Entries.Select(e => e.QuadrantName).Distinct().Count(),
            TotalComparisons = Entries.Sum(e => e.TotalComparisons),
            TotalActivities = Entries.Sum(e => e.ActivityDetectionCount),
            AverageActivityRate = Entries.Count > 0 ? Entries.Average(e => e.ActivityRate) : 0,
            HighestActivityQuadrant = Entries.OrderByDescending(e => e.ActivityRate).FirstOrDefault()?.QuadrantName ?? "",
            ReportDurationHours = ReportDuration.TotalHours
        };
    }
}

/// <summary>
/// Entrada individual de actividad por cuadrante en el reporte
/// </summary>
public class ActivityReportEntry
{
    public string QuadrantName { get; set; } = "";
    public int TotalComparisons { get; set; }
    public int ActivityDetectionCount { get; set; }
    public double ActivityRate { get; set; }
    public double AverageChangePercentage { get; set; }
    public DateTime FirstActivityTime { get; set; }
    public DateTime LastActivityTime { get; set; }
    public TimeSpan ActiveDuration { get; set; }
    public List<ActivityDetectionPoint> DetectionPoints { get; set; } = new();

    /// <summary>
    /// Tiempo sin actividad
    /// </summary>
    public TimeSpan InactiveDuration => TimeSpan.FromHours(24) - ActiveDuration; // Simplified

    /// <summary>
    /// Pico máximo de actividad
    /// </summary>
    public double PeakActivity => DetectionPoints.Count > 0 ? DetectionPoints.Max(d => d.ChangePercentage) : 0;
}

/// <summary>
/// Punto específico de detección de actividad
/// </summary>
public class ActivityDetectionPoint
{
    public DateTime Timestamp { get; set; }
    public double ChangePercentage { get; set; }
    public bool HasActivity { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Resumen estadístico del reporte
/// </summary>
public class ActivityReportSummary
{
    public int TotalQuadrants { get; set; }
    public int TotalComparisons { get; set; }
    public int TotalActivities { get; set; }
    public double AverageActivityRate { get; set; }
    public string HighestActivityQuadrant { get; set; } = "";
    public double ReportDurationHours { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Nivel general de actividad del período
    /// </summary>
    public string ActivityLevel
    {
        get
        {
            return AverageActivityRate switch
            {
                >= 50 => "Muy Alta",
                >= 25 => "Alta", 
                >= 10 => "Moderada",
                >= 5 => "Baja",
                _ => "Muy Baja"
            };
        }
    }

    /// <summary>
    /// Eficiencia de monitoreo (comparaciones que resultaron en actividad)
    /// </summary>
    public double MonitoringEfficiency => TotalComparisons > 0 ? (double)TotalActivities / TotalComparisons * 100.0 : 0;
}

/// <summary>
/// Configuración para generación de reportes de actividad
/// </summary>
public class ActivityReportConfiguration
{
    public bool AutoSaveReports { get; set; } = true;
    public string ReportsFolder { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), 
        "Capturer", "ActivityReports");
    public bool IncludeDetectionPoints { get; set; } = true;
    public int MaxDetectionPointsPerEntry { get; set; } = 10000; // Increased for longer monitoring
    public List<string> ExportFormats { get; set; } = new() { "JSON", "CSV", "HTML" };
    public bool CompressOldReports { get; set; } = true;
    public int ReportRetentionDays { get; set; } = 90;
    
    /// <summary>
    /// Configuración para integración con email
    /// </summary>
    public ActivityEmailIntegration EmailIntegration { get; set; } = new();
}

/// <summary>
/// Configuración de integración con sistema de email
/// </summary>
public class ActivityEmailIntegration
{
    public bool IncludeInManualReports { get; set; } = false;
    public bool IncludeInScheduledReports { get; set; } = false;
    public bool AttachDetailedReport { get; set; } = true;
    public bool EmbedSummaryInEmail { get; set; } = true;
    public string ReportFileNamePattern { get; set; } = "ActivityReport_{StartDate:yyyyMMdd}_{EndDate:yyyyMMdd}";
    public List<string> PreferredFormats { get; set; } = new() { "HTML", "CSV" };
}