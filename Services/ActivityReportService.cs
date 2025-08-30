using System.Text.Json;
using System.Text;
using Capturer.Models;

namespace Capturer.Services;

/// <summary>
/// Servicio para gestionar reportes de actividad - persistencia, exportación e integración
/// </summary>
public class ActivityReportService : IDisposable
{
    private readonly QuadrantActivityService _activityService;
    private readonly ActivityReportConfiguration _config;
    private readonly object _lockObject = new object();
    
    // Current session report
    private ActivityReport? _currentSessionReport;
    private readonly System.Threading.Timer _autoSaveTimer;

    public event EventHandler<ActivityReportGeneratedEventArgs>? ReportGenerated;

    public ActivityReportService(QuadrantActivityService activityService, ActivityReportConfiguration? config = null)
    {
        _activityService = activityService ?? throw new ArgumentNullException(nameof(activityService));
        _config = config ?? new ActivityReportConfiguration();
        
        EnsureReportsFolder();
        StartCurrentSession();
        
        // Auto-save current session every 5 minutes
        _autoSaveTimer = new System.Threading.Timer(AutoSaveCurrentSession, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        
        // Subscribe to activity changes to build real-time report
        _activityService.ActivityChanged += OnActivityChanged;
        
        Console.WriteLine($"[ActivityReportService] Servicio iniciado. Carpeta de reportes: {_config.ReportsFolder}");
    }

    /// <summary>
    /// Inicia una nueva sesión de reporte
    /// </summary>
    private void StartCurrentSession()
    {
        lock (_lockObject)
        {
            var sessionStartTime = DateTime.Now;
            _currentSessionReport = new ActivityReport
            {
                ReportStartTime = sessionStartTime,
                ReportEndTime = sessionStartTime, // Will update continuously
                SessionStartTime = sessionStartTime,
                SessionName = GenerateSessionName(sessionStartTime),
                ReportType = "LiveSession",
                Comments = "Sesión en tiempo real del dashboard de actividad",
                IsActive = true
            };
        }
    }

    /// <summary>
    /// Genera un nombre de sesión legible basado en la fecha y hora de inicio
    /// </summary>
    private string GenerateSessionName(DateTime startTime)
    {
        return $"Sesión_{startTime:yyyy-MM-dd_HH-mm-ss}";
    }

    /// <summary>
    /// Maneja cambios de actividad para construir el reporte en tiempo real
    /// </summary>
    private void OnActivityChanged(object? sender, QuadrantActivityChangedEventArgs e)
    {
        if (_currentSessionReport == null) return;

        lock (_lockObject)
        {
            // Update report end time
            _currentSessionReport.ReportEndTime = DateTime.Now;
            
            // Find or create entry for this quadrant
            var entry = _currentSessionReport.Entries.FirstOrDefault(entry => entry.QuadrantName == e.Result.QuadrantName);
            if (entry == null)
            {
                entry = new ActivityReportEntry
                {
                    QuadrantName = e.Result.QuadrantName,
                    FirstActivityTime = e.Result.Timestamp
                };
                _currentSessionReport.Entries.Add(entry);
            }

            // Update entry with latest data
            entry.TotalComparisons = e.Stats.TotalComparisons;
            entry.ActivityDetectionCount = e.Stats.ActivityDetectionCount;
            entry.ActivityRate = e.Stats.ActivityRate;
            entry.AverageChangePercentage = e.Stats.AverageChangePercentage;
            entry.LastActivityTime = e.Stats.LastActivityTime;
            entry.ActiveDuration = e.Stats.SessionDuration;

            // Add detection point if we should track them
            if (_config.IncludeDetectionPoints && entry.DetectionPoints.Count < _config.MaxDetectionPointsPerEntry)
            {
                entry.DetectionPoints.Add(new ActivityDetectionPoint
                {
                    Timestamp = e.Result.Timestamp,
                    ChangePercentage = e.Result.ChangePercentage,
                    HasActivity = e.Result.HasActivity
                });
            }

            // Update quadrant configuration name if available
            if (string.IsNullOrEmpty(_currentSessionReport.QuadrantConfigurationName))
            {
                _currentSessionReport.QuadrantConfigurationName = "Live-Session";
            }
        }
    }

    /// <summary>
    /// Auto-guarda la sesión actual
    /// </summary>
    private void AutoSaveCurrentSession(object? state)
    {
        if (!_config.AutoSaveReports || _currentSessionReport == null) return;

        try
        {
            SaveCurrentSessionReport();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ActivityReportService] Error en auto-guardado: {ex.Message}");
        }
    }

    /// <summary>
    /// Guarda el reporte de la sesión actual
    /// </summary>
    private void SaveCurrentSessionReport()
    {
        lock (_lockObject)
        {
            if (_currentSessionReport == null) return;

            // Generate updated summary
            _currentSessionReport.GenerateSummary();

            // Create session file name with session start time and current end time
            var sessionName = _currentSessionReport.SessionName;
            var endTime = DateTime.Now;
            var fileName = $"{sessionName}_to_{endTime:yyyy-MM-dd_HH-mm-ss}.json";
            var filePath = Path.Combine(_config.ReportsFolder, "Sessions", fileName);
            
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            
            // Update end time before saving
            _currentSessionReport.ReportEndTime = endTime;
            
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var jsonContent = JsonSerializer.Serialize(_currentSessionReport, jsonOptions);
            File.WriteAllText(filePath, jsonContent, Encoding.UTF8);
            
            // Save individual quadrant data
            SaveQuadrantData(_currentSessionReport);
            
            Console.WriteLine($"[ActivityReportService] Sesión auto-guardada: {fileName}");
        }
    }

    /// <summary>
    /// Guarda datos individuales por cuadrante
    /// </summary>
    private void SaveQuadrantData(ActivityReport report)
    {
        try
        {
            var quadrantsFolder = Path.Combine(_config.ReportsFolder, "Quadrants");
            Directory.CreateDirectory(quadrantsFolder);

            foreach (var entry in report.Entries)
            {
                var quadrantFolder = Path.Combine(quadrantsFolder, SanitizeFileName(entry.QuadrantName));
                Directory.CreateDirectory(quadrantFolder);

                var quadrantFileName = $"{report.SessionName}_to_{report.ReportEndTime:yyyy-MM-dd_HH-mm-ss}_{entry.QuadrantName}.json";
                var quadrantFilePath = Path.Combine(quadrantFolder, quadrantFileName);

                var quadrantReport = new ActivityReport
                {
                    Id = $"{report.Id}_{entry.QuadrantName}",
                    CreatedAt = report.CreatedAt,
                    ReportStartTime = report.ReportStartTime,
                    ReportEndTime = report.ReportEndTime,
                    SessionStartTime = report.SessionStartTime,
                    SessionName = report.SessionName,
                    QuadrantConfigurationName = report.QuadrantConfigurationName,
                    ReportType = $"{report.ReportType}_Quadrant",
                    Comments = $"Datos específicos del cuadrante '{entry.QuadrantName}' para la {report.SessionName}",
                    IsActive = report.IsActive,
                    Entries = new List<ActivityReportEntry> { entry }
                };

                quadrantReport.GenerateSummary();

                var jsonOptions = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var jsonContent = JsonSerializer.Serialize(quadrantReport, jsonOptions);
                File.WriteAllText(quadrantFilePath, jsonContent, Encoding.UTF8);
            }

            Console.WriteLine($"[ActivityReportService] Datos guardados por cuadrante: {report.Entries.Count} cuadrantes");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ActivityReportService] Error guardando datos por cuadrante: {ex.Message}");
        }
    }

    /// <summary>
    /// Sanitiza nombres de archivo
    /// </summary>
    private string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Join("_", fileName.Split(invalidChars));
    }

    /// <summary>
    /// Genera un reporte para un período específico
    /// </summary>
    public async Task<ActivityReport> GenerateReportAsync(DateTime startDate, DateTime endDate, string? quadrantConfigName = null)
    {
        var report = new ActivityReport
        {
            ReportStartTime = startDate,
            ReportEndTime = endDate,
            QuadrantConfigurationName = quadrantConfigName ?? "Manual-Report",
            ReportType = "Manual",
            Comments = $"Reporte generado manualmente para el período {startDate:yyyy-MM-dd} a {endDate:yyyy-MM-dd}"
        };

        // Get current stats from activity service
        var allStats = _activityService.GetAllActivityStats();
        
        foreach (var (quadrantName, stats) in allStats)
        {
            var entry = new ActivityReportEntry
            {
                QuadrantName = quadrantName,
                TotalComparisons = stats.TotalComparisons,
                ActivityDetectionCount = stats.ActivityDetectionCount,
                ActivityRate = stats.ActivityRate,
                AverageChangePercentage = stats.AverageChangePercentage,
                FirstActivityTime = stats.SessionStartTime,
                LastActivityTime = stats.LastActivityTime,
                ActiveDuration = stats.SessionDuration
            };

            report.Entries.Add(entry);
        }

        // Generate summary
        report.GenerateSummary();

        // Save if configured
        if (_config.AutoSaveReports)
        {
            await SaveReportAsync(report);
        }

        // Notify listeners
        ReportGenerated?.Invoke(this, new ActivityReportGeneratedEventArgs { Report = report });

        Console.WriteLine($"[ActivityReportService] Reporte generado: {report.Entries.Count} cuadrantes, {report.Summary.TotalActivities} actividades");
        
        return report;
    }

    /// <summary>
    /// Guarda un reporte en el sistema de archivos
    /// </summary>
    public async Task SaveReportAsync(ActivityReport report)
    {
        var fileName = $"Report_{report.Id}_{report.ReportStartTime:yyyyMMdd}_{report.ReportEndTime:yyyyMMdd}.json";
        var filePath = Path.Combine(_config.ReportsFolder, fileName);
        
        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var jsonContent = JsonSerializer.Serialize(report, jsonOptions);
        await File.WriteAllTextAsync(filePath, jsonContent, Encoding.UTF8);
        
        Console.WriteLine($"[ActivityReportService] Reporte guardado: {fileName}");
    }

    /// <summary>
    /// Exporta un reporte a diferentes formatos
    /// </summary>
    public async Task<string> ExportReportAsync(ActivityReport report, string format = "HTML")
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var fileName = format.ToUpper() switch
        {
            "JSON" => $"ActivityReport_{timestamp}.json",
            "CSV" => $"ActivityReport_{timestamp}.csv", 
            "HTML" => $"ActivityReport_{timestamp}.html",
            _ => $"ActivityReport_{timestamp}.txt"
        };
        
        var filePath = Path.Combine(_config.ReportsFolder, "Exports", fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        var content = format.ToUpper() switch
        {
            "JSON" => await ExportToJsonAsync(report),
            "CSV" => await ExportToCsvAsync(report),
            "HTML" => await ExportToHtmlAsync(report),
            _ => await ExportToTextAsync(report)
        };

        await File.WriteAllTextAsync(filePath, content, Encoding.UTF8);
        
        Console.WriteLine($"[ActivityReportService] Reporte exportado: {fileName}");
        return filePath;
    }

    /// <summary>
    /// Exporta a formato JSON
    /// </summary>
    private async Task<string> ExportToJsonAsync(ActivityReport report)
    {
        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        return JsonSerializer.Serialize(report, jsonOptions);
    }

    /// <summary>
    /// Exporta a formato CSV
    /// </summary>
    private async Task<string> ExportToCsvAsync(ActivityReport report)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Cuadrante,Comparaciones,Actividades,Tasa Actividad (%),Cambio Promedio (%),Primera Actividad,Última Actividad,Duración Activa");
        
        foreach (var entry in report.Entries)
        {
            csv.AppendLine($"{entry.QuadrantName},{entry.TotalComparisons},{entry.ActivityDetectionCount}," +
                          $"{entry.ActivityRate:F2},{entry.AverageChangePercentage:F2}," +
                          $"{entry.FirstActivityTime:yyyy-MM-dd HH:mm:ss},{entry.LastActivityTime:yyyy-MM-dd HH:mm:ss}," +
                          $"{entry.ActiveDuration:hh\\:mm\\:ss}");
        }
        
        return csv.ToString();
    }

    /// <summary>
    /// Exporta a formato HTML
    /// </summary>
    private async Task<string> ExportToHtmlAsync(ActivityReport report)
    {
        var html = new StringBuilder();
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html><head><title>Reporte de Actividad</title>");
        html.AppendLine("<style>");
        html.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
        html.AppendLine("table { border-collapse: collapse; width: 100%; }");
        html.AppendLine("th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
        html.AppendLine("th { background-color: #f2f2f2; }");
        html.AppendLine(".summary { background-color: #e7f3ff; padding: 15px; margin-bottom: 20px; border-radius: 5px; }");
        html.AppendLine("</style></head><body>");
        
        html.AppendLine($"<h1>Reporte de Actividad - {report.ReportStartTime:dd/MM/yyyy} a {report.ReportEndTime:dd/MM/yyyy}</h1>");
        
        // Summary section
        html.AppendLine("<div class='summary'>");
        html.AppendLine("<h2>Resumen</h2>");
        html.AppendLine($"<p><strong>Período:</strong> {report.ReportDuration.TotalHours:F1} horas</p>");
        html.AppendLine($"<p><strong>Cuadrantes:</strong> {report.Summary.TotalQuadrants}</p>");
        html.AppendLine($"<p><strong>Comparaciones totales:</strong> {report.Summary.TotalComparisons:N0}</p>");
        html.AppendLine($"<p><strong>Actividades detectadas:</strong> {report.Summary.TotalActivities:N0}</p>");
        html.AppendLine($"<p><strong>Tasa promedio de actividad:</strong> {report.Summary.AverageActivityRate:F1}% ({report.Summary.ActivityLevel})</p>");
        html.AppendLine($"<p><strong>Cuadrante más activo:</strong> {report.Summary.HighestActivityQuadrant}</p>");
        html.AppendLine($"<p><strong>Eficiencia de monitoreo:</strong> {report.Summary.MonitoringEfficiency:F1}%</p>");
        html.AppendLine("</div>");
        
        // Details table
        html.AppendLine("<h2>Detalles por Cuadrante</h2>");
        html.AppendLine("<table>");
        html.AppendLine("<tr><th>Cuadrante</th><th>Comparaciones</th><th>Actividades</th><th>Tasa (%)</th><th>Cambio Prom. (%)</th><th>Primera Actividad</th><th>Última Actividad</th><th>Duración</th></tr>");
        
        foreach (var entry in report.Entries.OrderByDescending(e => e.ActivityRate))
        {
            html.AppendLine("<tr>");
            html.AppendLine($"<td>{entry.QuadrantName}</td>");
            html.AppendLine($"<td>{entry.TotalComparisons:N0}</td>");
            html.AppendLine($"<td>{entry.ActivityDetectionCount:N0}</td>");
            html.AppendLine($"<td>{entry.ActivityRate:F1}%</td>");
            html.AppendLine($"<td>{entry.AverageChangePercentage:F2}%</td>");
            html.AppendLine($"<td>{entry.FirstActivityTime:dd/MM/yyyy HH:mm}</td>");
            html.AppendLine($"<td>{entry.LastActivityTime:dd/MM/yyyy HH:mm}</td>");
            html.AppendLine($"<td>{entry.ActiveDuration:hh\\:mm\\:ss}</td>");
            html.AppendLine("</tr>");
        }
        
        html.AppendLine("</table>");
        html.AppendLine($"<p><em>Reporte generado el {DateTime.Now:dd/MM/yyyy HH:mm:ss} por Capturer Dashboard de Actividad</em></p>");
        html.AppendLine("</body></html>");
        
        return html.ToString();
    }

    /// <summary>
    /// Exporta a formato texto plano
    /// </summary>
    private async Task<string> ExportToTextAsync(ActivityReport report)
    {
        var text = new StringBuilder();
        text.AppendLine("REPORTE DE ACTIVIDAD - CAPTURER DASHBOARD");
        text.AppendLine("=" + new string('=', 50));
        text.AppendLine($"Período: {report.ReportStartTime:dd/MM/yyyy HH:mm} - {report.ReportEndTime:dd/MM/yyyy HH:mm}");
        text.AppendLine($"Duración: {report.ReportDuration.TotalHours:F1} horas");
        text.AppendLine($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
        text.AppendLine();
        
        text.AppendLine("RESUMEN:");
        text.AppendLine("-" + new string('-', 30));
        text.AppendLine($"Total de cuadrantes: {report.Summary.TotalQuadrants}");
        text.AppendLine($"Comparaciones realizadas: {report.Summary.TotalComparisons:N0}");
        text.AppendLine($"Actividades detectadas: {report.Summary.TotalActivities:N0}");
        text.AppendLine($"Tasa promedio de actividad: {report.Summary.AverageActivityRate:F1}% ({report.Summary.ActivityLevel})");
        text.AppendLine($"Cuadrante más activo: {report.Summary.HighestActivityQuadrant}");
        text.AppendLine($"Eficiencia de monitoreo: {report.Summary.MonitoringEfficiency:F1}%");
        text.AppendLine();
        
        text.AppendLine("DETALLES POR CUADRANTE:");
        text.AppendLine("-" + new string('-', 30));
        
        foreach (var entry in report.Entries.OrderByDescending(e => e.ActivityRate))
        {
            text.AppendLine($"\n[{entry.QuadrantName}]");
            text.AppendLine($"  Comparaciones: {entry.TotalComparisons:N0}");
            text.AppendLine($"  Actividades: {entry.ActivityDetectionCount:N0}");
            text.AppendLine($"  Tasa de actividad: {entry.ActivityRate:F1}%");
            text.AppendLine($"  Cambio promedio: {entry.AverageChangePercentage:F2}%");
            text.AppendLine($"  Primera actividad: {entry.FirstActivityTime:dd/MM/yyyy HH:mm:ss}");
            text.AppendLine($"  Última actividad: {entry.LastActivityTime:dd/MM/yyyy HH:mm:ss}");
            text.AppendLine($"  Duración activa: {entry.ActiveDuration:hh\\:mm\\:ss}");
        }
        
        return text.ToString();
    }

    /// <summary>
    /// Obtiene la sesión actual como reporte
    /// </summary>
    public ActivityReport? GetCurrentSessionReport()
    {
        lock (_lockObject)
        {
            if (_currentSessionReport == null) return null;
            
            // Create a copy with updated summary
            var copy = JsonSerializer.Deserialize<ActivityReport>(
                JsonSerializer.Serialize(_currentSessionReport));
            copy?.GenerateSummary();
            return copy;
        }
    }

    /// <summary>
    /// Lista reportes guardados
    /// </summary>
    public async Task<List<string>> GetSavedReportsAsync()
    {
        if (!Directory.Exists(_config.ReportsFolder))
            return new List<string>();

        var files = Directory.GetFiles(_config.ReportsFolder, "*.json", SearchOption.AllDirectories);
        return files.OrderByDescending(f => File.GetLastWriteTime(f)).ToList();
    }

    /// <summary>
    /// Carga un reporte desde archivo
    /// </summary>
    public async Task<ActivityReport?> LoadReportAsync(string filePath)
    {
        if (!File.Exists(filePath)) return null;

        try
        {
            var jsonContent = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            return JsonSerializer.Deserialize<ActivityReport>(jsonContent, options);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ActivityReportService] Error cargando reporte {filePath}: {ex.Message}");
            return null;
        }
    }

    private void EnsureReportsFolder()
    {
        try
        {
            Directory.CreateDirectory(_config.ReportsFolder);
            Directory.CreateDirectory(Path.Combine(_config.ReportsFolder, "Sessions"));
            Directory.CreateDirectory(Path.Combine(_config.ReportsFolder, "Exports"));
            Directory.CreateDirectory(Path.Combine(_config.ReportsFolder, "Quadrants"));
            Console.WriteLine($"[ActivityReportService] Carpetas de reportes inicializadas: {_config.ReportsFolder}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ActivityReportService] Error creando carpetas: {ex.Message}");
        }
    }

    public void Dispose()
    {
        try
        {
            // Save current session before disposing
            SaveCurrentSessionReport();
            
            _autoSaveTimer?.Dispose();
            _activityService.ActivityChanged -= OnActivityChanged;
            
            Console.WriteLine("[ActivityReportService] Servicio de reportes finalizado");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ActivityReportService] Error en dispose: {ex.Message}");
        }
    }
}

/// <summary>
/// Argumentos del evento de reporte generado
/// </summary>
public class ActivityReportGeneratedEventArgs : EventArgs
{
    public ActivityReport Report { get; set; } = new();
}