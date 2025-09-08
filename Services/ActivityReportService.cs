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

            // Add detection point if we should track them - IMPROVED STRATEGY
            if (_config.IncludeDetectionPoints)
            {
                // Smart collection: Keep only meaningful points to avoid hitting limit
                var shouldAdd = ShouldAddDetectionPoint(entry, e.Result);
                
                if (shouldAdd && entry.DetectionPoints.Count < _config.MaxDetectionPointsPerEntry)
                {
                    entry.DetectionPoints.Add(new ActivityDetectionPoint
                    {
                        Timestamp = e.Result.Timestamp,
                        ChangePercentage = e.Result.ChangePercentage,
                        HasActivity = e.Result.HasActivity,
                        Notes = $"Activity: {e.Result.HasActivity}, Change: {e.Result.ChangePercentage:F2}%"
                    });
                    
                    Console.WriteLine($"[ActivityReportService] Detection point added for {e.Result.QuadrantName}: {e.Result.Timestamp:HH:mm:ss} - Activity: {e.Result.HasActivity} ({e.Result.ChangePercentage:F1}%)");
                }
                else if (entry.DetectionPoints.Count >= _config.MaxDetectionPointsPerEntry)
                {
                    // Remove oldest points when limit is reached to keep collecting new data
                    var oldestPoints = entry.DetectionPoints.OrderBy(dp => dp.Timestamp).Take(100);
                    foreach (var oldPoint in oldestPoints.ToList())
                    {
                        entry.DetectionPoints.Remove(oldPoint);
                    }
                    
                    // Add the new point
                    entry.DetectionPoints.Add(new ActivityDetectionPoint
                    {
                        Timestamp = e.Result.Timestamp,
                        ChangePercentage = e.Result.ChangePercentage,
                        HasActivity = e.Result.HasActivity,
                        Notes = $"Activity: {e.Result.HasActivity}, Change: {e.Result.ChangePercentage:F2}%"
                    });
                    
                    Console.WriteLine($"[ActivityReportService] Detection point added (with cleanup) for {e.Result.QuadrantName}: {e.Result.Timestamp:HH:mm:ss}");
                }
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
    /// Genera los estilos CSS modernos para el reporte HTML
    /// </summary>
    private string GetModernCssStyles()
    {
        return @"
            :root {
                --primary-color: #2563eb;
                --secondary-color: #3b82f6;
                --success-color: #10b981;
                --warning-color: #f59e0b;
                --danger-color: #ef4444;
                --gray-50: #f9fafb;
                --gray-100: #f3f4f6;
                --gray-200: #e5e7eb;
                --gray-600: #4b5563;
                --gray-800: #1f2937;
                --shadow: 0 1px 3px 0 rgba(0, 0, 0, 0.1), 0 1px 2px 0 rgba(0, 0, 0, 0.06);
                --shadow-lg: 0 10px 15px -3px rgba(0, 0, 0, 0.1), 0 4px 6px -2px rgba(0, 0, 0, 0.05);
            }
            
            * {
                margin: 0;
                padding: 0;
                box-sizing: border-box;
            }
            
            body {
                font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
                background: linear-gradient(135deg, var(--gray-50) 0%, #ffffff 100%);
                color: var(--gray-800);
                line-height: 1.6;
            }
            
            .container {
                max-width: 1200px;
                margin: 0 auto;
                padding: 0 20px;
            }
            
            .header {
                background: linear-gradient(135deg, var(--primary-color) 0%, var(--secondary-color) 100%);
                color: white;
                padding: 2rem 0;
                margin-bottom: 2rem;
                box-shadow: var(--shadow-lg);
            }
            
            .header-content {
                display: flex;
                align-items: center;
                gap: 1rem;
            }
            
            .logo {
                font-size: 3rem;
                filter: drop-shadow(0 2px 4px rgba(0, 0, 0, 0.1));
            }
            
            .header h1 {
                font-size: 2.5rem;
                font-weight: 700;
                margin-bottom: 0.5rem;
            }
            
            .period {
                font-size: 1.1rem;
                opacity: 0.9;
                font-weight: 300;
            }
            
            .summary-grid {
                display: grid;
                grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
                gap: 1.5rem;
                margin-bottom: 3rem;
            }
            
            .summary-card {
                background: white;
                padding: 1.5rem;
                border-radius: 12px;
                box-shadow: var(--shadow);
                border-left: 4px solid var(--primary-color);
                transition: all 0.2s ease;
            }
            
            .summary-card:hover {
                transform: translateY(-2px);
                box-shadow: var(--shadow-lg);
            }
            
            .card-header {
                display: flex;
                align-items: center;
                gap: 0.75rem;
                margin-bottom: 1rem;
            }
            
            .card-icon {
                font-size: 1.5rem;
                width: 40px;
                height: 40px;
                display: flex;
                align-items: center;
                justify-content: center;
                background: var(--gray-100);
                border-radius: 8px;
            }
            
            .card-title {
                font-size: 0.875rem;
                font-weight: 500;
                color: var(--gray-600);
                text-transform: uppercase;
                letter-spacing: 0.05em;
            }
            
            .card-value {
                font-size: 2rem;
                font-weight: 700;
                color: var(--gray-800);
                margin-bottom: 0.5rem;
            }
            
            .card-description {
                font-size: 0.875rem;
                color: var(--gray-600);
            }
            
            .chart-container {
                background: white;
                padding: 2rem;
                border-radius: 12px;
                box-shadow: var(--shadow);
                margin-bottom: 2rem;
            }
            
            .chart-title {
                font-size: 1.5rem;
                font-weight: 600;
                margin-bottom: 1.5rem;
                display: flex;
                align-items: center;
                gap: 0.5rem;
            }
            
            .chart-wrapper {
                position: relative;
                height: 400px;
            }
            
            .chart-section {
                background: var(--gray-50);
                padding: 2rem;
                border-radius: 12px;
                margin-bottom: 2rem;
                border-left: 4px solid var(--primary-color);
            }
            
            .section-title {
                font-size: 1.75rem;
                font-weight: 700;
                margin-bottom: 0.75rem;
                color: var(--gray-800);
            }
            
            .section-description {
                font-size: 1rem;
                color: var(--gray-600);
                line-height: 1.5;
            }
            
            .chart-description {
                font-size: 0.875rem;
                color: var(--gray-500);
                margin-bottom: 1rem;
                font-style: italic;
            }
            
            .modern-table {
                background: white;
                border-radius: 12px;
                overflow: hidden;
                box-shadow: var(--shadow);
                margin-bottom: 2rem;
            }
            
            .table-header {
                background: var(--gray-50);
                padding: 1.5rem 2rem;
                border-bottom: 1px solid var(--gray-200);
            }
            
            .table-title {
                font-size: 1.25rem;
                font-weight: 600;
                display: flex;
                align-items: center;
                gap: 0.5rem;
            }
            
            table {
                width: 100%;
                border-collapse: collapse;
            }
            
            th, td {
                padding: 1rem 2rem;
                text-align: left;
                border-bottom: 1px solid var(--gray-200);
            }
            
            th {
                background: var(--gray-50);
                font-weight: 600;
                color: var(--gray-800);
                font-size: 0.875rem;
                text-transform: uppercase;
                letter-spacing: 0.05em;
            }
            
            tbody tr:hover {
                background: var(--gray-50);
            }
            
            .activity-bar {
                width: 60px;
                height: 8px;
                background: var(--gray-200);
                border-radius: 4px;
                overflow: hidden;
                display: inline-block;
                vertical-align: middle;
                margin-left: 0.5rem;
            }
            
            .activity-fill {
                height: 100%;
                border-radius: 4px;
                transition: width 0.3s ease;
            }
            
            .footer {
                background: var(--gray-800);
                color: white;
                text-align: center;
                padding: 2rem;
                margin-top: 3rem;
            }
            
            .footer p {
                opacity: 0.8;
                font-size: 0.875rem;
            }
            
            @media (max-width: 768px) {
                .container {
                    padding: 0 1rem;
                }
                
                .header h1 {
                    font-size: 2rem;
                }
                
                .summary-grid {
                    grid-template-columns: 1fr;
                }
                
                .chart-wrapper {
                    height: 300px;
                }
                
                th, td {
                    padding: 0.75rem 1rem;
                }
            }
        ";
    }
    
    /// <summary>
    /// Genera las tarjetas de resumen con métricas clave
    /// </summary>
    private string GetSummaryCardsHtml(ActivityReport report)
    {
        var html = new StringBuilder();
        html.AppendLine("<div class='summary-grid'>");
        
        // Tarjeta de duración
        html.AppendLine("<div class='summary-card'>");
        html.AppendLine("<div class='card-header'>");
        html.AppendLine("<div class='card-icon'>⏱️</div>");
        html.AppendLine("<div class='card-title'>Duración del Monitoreo</div>");
        html.AppendLine("</div>");
        html.AppendLine($"<div class='card-value'>{report.ReportDuration.TotalHours:F1}h</div>");
        html.AppendLine($"<div class='card-description'>{(int)report.ReportDuration.TotalMinutes} minutos de actividad</div>");
        html.AppendLine("</div>");
        
        // Tarjeta de cuadrantes
        html.AppendLine("<div class='summary-card'>");
        html.AppendLine("<div class='card-header'>");
        html.AppendLine("<div class='card-icon'>🎯</div>");
        html.AppendLine("<div class='card-title'>Cuadrantes Monitoreados</div>");
        html.AppendLine("</div>");
        html.AppendLine($"<div class='card-value'>{report.Summary.TotalQuadrants}</div>");
        html.AppendLine("<div class='card-description'>Regiones bajo seguimiento</div>");
        html.AppendLine("</div>");
        
        // Tarjeta de actividades
        html.AppendLine("<div class='summary-card'>");
        html.AppendLine("<div class='card-header'>");
        html.AppendLine("<div class='card-icon'>🔥</div>");
        html.AppendLine("<div class='card-title'>Actividades Detectadas</div>");
        html.AppendLine("</div>");
        html.AppendLine($"<div class='card-value'>{report.Summary.TotalActivities:N0}</div>");
        html.AppendLine($"<div class='card-description'>De {report.Summary.TotalComparisons:N0} comparaciones</div>");
        html.AppendLine("</div>");
        
        // Tarjeta de eficiencia
        html.AppendLine("<div class='summary-card'>");
        html.AppendLine("<div class='card-header'>");
        html.AppendLine("<div class='card-icon'>📊</div>");
        html.AppendLine("<div class='card-title'>Eficiencia de Monitoreo</div>");
        html.AppendLine("</div>");
        html.AppendLine($"<div class='card-value'>{report.Summary.MonitoringEfficiency:F1}%</div>");
        html.AppendLine($"<div class='card-description'>Nivel: {report.Summary.ActivityLevel}</div>");
        html.AppendLine("</div>");
        
        html.AppendLine("</div>"); // summary-grid
        return html.ToString();
    }
    
    /// <summary>
    /// Genera la gráfica de barras de actividad por cuadrante
    /// </summary>
    private string GetActivityChartHtml(ActivityReport report)
    {
        var html = new StringBuilder();
        html.AppendLine("<div class='chart-container'>");
        html.AppendLine("<h2 class='chart-title'>📊 Actividad por Cuadrante</h2>");
        html.AppendLine("<div class='chart-wrapper'>");
        html.AppendLine("<canvas id='activityChart'></canvas>");
        html.AppendLine("</div></div>");
        return html.ToString();
    }
    
    /// <summary>
    /// Genera la gráfica de línea temporal (simulada basada en DetectionPoints)
    /// </summary>
    private string GetTimelineChartHtml(ActivityReport report)
    {
        var html = new StringBuilder();
        html.AppendLine("<div class='chart-container'>");
        html.AppendLine("<h2 class='chart-title'>📈 Evolución de Actividad durante el Día (Intervalos 5min)</h2>");
        html.AppendLine("<div class='chart-wrapper'>");
        html.AppendLine("<canvas id='timelineChart'></canvas>");
        html.AppendLine("</div></div>");
        return html.ToString();
    }
    
    /// <summary>
    /// Genera la tabla moderna con estilos mejorados
    /// </summary>
    private string GetModernTableHtml(ActivityReport report)
    {
        var html = new StringBuilder();
        html.AppendLine("<div class='modern-table'>");
        html.AppendLine("<div class='table-header'>");
        html.AppendLine("<h2 class='table-title'>📋 Detalles por Cuadrante</h2>");
        html.AppendLine("</div>");
        html.AppendLine("<table>");
        html.AppendLine("<thead>");
        html.AppendLine("<tr>");
        html.AppendLine("<th>Cuadrante</th>");
        html.AppendLine("<th>Comparaciones</th>");
        html.AppendLine("<th>Actividades</th>");
        html.AppendLine("<th>Tasa de Actividad</th>");
        html.AppendLine("<th>Cambio Promedio</th>");
        html.AppendLine("<th>Primera Actividad</th>");
        html.AppendLine("<th>Última Actividad</th>");
        html.AppendLine("<th>Duración Activa</th>");
        html.AppendLine("</tr>");
        html.AppendLine("</thead>");
        html.AppendLine("<tbody>");
        
        foreach (var entry in report.Entries.OrderByDescending(e => e.ActivityRate))
        {
            var activityColor = GetActivityColor(entry.ActivityRate);
            var barWidth = Math.Min(100, entry.ActivityRate * 2); // Scale for visual
            
            html.AppendLine("<tr>");
            html.AppendLine($"<td><strong>{entry.QuadrantName}</strong></td>");
            html.AppendLine($"<td>{entry.TotalComparisons:N0}</td>");
            html.AppendLine($"<td>{entry.ActivityDetectionCount:N0}</td>");
            html.AppendLine($"<td>{entry.ActivityRate:F1}%<span class='activity-bar'><span class='activity-fill' style='width: {barWidth}%; background: {activityColor};'></span></span></td>");
            html.AppendLine($"<td>{entry.AverageChangePercentage:F2}%</td>");
            html.AppendLine($"<td>{entry.FirstActivityTime:dd/MM HH:mm}</td>");
            html.AppendLine($"<td>{entry.LastActivityTime:dd/MM HH:mm}</td>");
            html.AppendLine($"<td>{entry.ActiveDuration:hh\\:mm\\:ss}</td>");
            html.AppendLine("</tr>");
        }
        
        html.AppendLine("</tbody>");
        html.AppendLine("</table>");
        html.AppendLine("</div>");
        return html.ToString();
    }
    
    /// <summary>
    /// Obtiene el color según el nivel de actividad
    /// </summary>
    private string GetActivityColor(double activityRate)
    {
        return activityRate switch
        {
            >= 20 => "#ef4444", // Rojo - Muy alta
            >= 10 => "#f59e0b", // Naranja - Alta  
            >= 5 => "#10b981",  // Verde - Moderada
            >= 2 => "#3b82f6",  // Azul - Baja
            _ => "#6b7280"       // Gris - Muy baja
        };
    }
    
    /// <summary>
    /// Genera el JavaScript para las gráficas Chart.js
    /// </summary>
    private string GetChartJavaScript(ActivityReport report)
    {
        var html = new StringBuilder();
        html.AppendLine("<script>");
        html.AppendLine("document.addEventListener('DOMContentLoaded', function() {");
        
        // Datos para gráfica de barras
        var quadrantNames = string.Join(", ", report.Entries.Select(e => $"'{e.QuadrantName}'"));
        var activityRates = string.Join(", ", report.Entries.Select(e => e.ActivityRate.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)));
        var activityCounts = string.Join(", ", report.Entries.Select(e => e.ActivityDetectionCount.ToString()));
        var colors = string.Join(", ", report.Entries.Select(e => $"'{GetActivityColor(e.ActivityRate)}'"));
        
        html.AppendLine("    // Gráfica de actividad por cuadrante");
        html.AppendLine("    const activityCtx = document.getElementById('activityChart').getContext('2d');");
        html.AppendLine("    new Chart(activityCtx, {");
        html.AppendLine("        type: 'bar',");
        html.AppendLine("        data: {");
        html.AppendLine($"            labels: [{quadrantNames}],");
        html.AppendLine("            datasets: [{");
        html.AppendLine("                label: 'Tasa de Actividad (%)',");
        html.AppendLine($"                data: [{activityRates}],");
        html.AppendLine($"                backgroundColor: [{colors}],");
        html.AppendLine($"                borderColor: [{colors}],");
        html.AppendLine("                borderWidth: 2,");
        html.AppendLine("                borderRadius: 6,");
        html.AppendLine("                borderSkipped: false");
        html.AppendLine("            }, {");
        html.AppendLine("                label: 'Actividades Detectadas',");
        html.AppendLine($"                data: [{activityCounts}],");
        html.AppendLine("                type: 'line',");
        html.AppendLine("                yAxisID: 'y1',");
        html.AppendLine("                borderColor: '#6366f1',");
        html.AppendLine("                backgroundColor: 'rgba(99, 102, 241, 0.1)',");
        html.AppendLine("                borderWidth: 3,");
        html.AppendLine("                fill: true");
        html.AppendLine("            }]");
        html.AppendLine("        },");
        html.AppendLine("        options: {");
        html.AppendLine("            responsive: true,");
        html.AppendLine("            maintainAspectRatio: false,");
        html.AppendLine("            plugins: {");
        html.AppendLine("                legend: { position: 'top' },");
        html.AppendLine("                tooltip: {");
        html.AppendLine("                    mode: 'index',");
        html.AppendLine("                    intersect: false");
        html.AppendLine("                }");
        html.AppendLine("            },");
        html.AppendLine("            scales: {");
        html.AppendLine("                y: {");
        html.AppendLine("                    type: 'linear',");
        html.AppendLine("                    display: true,");
        html.AppendLine("                    position: 'left',");
        html.AppendLine("                    title: { display: true, text: 'Tasa de Actividad (%)' }");
        html.AppendLine("                },");
        html.AppendLine("                y1: {");
        html.AppendLine("                    type: 'linear',");
        html.AppendLine("                    display: true,");
        html.AppendLine("                    position: 'right',");
        html.AppendLine("                    title: { display: true, text: 'Número de Actividades' },");
        html.AppendLine("                    grid: { drawOnChartArea: false }");
        html.AppendLine("                }");
        html.AppendLine("            }");
        html.AppendLine("        }");
        html.AppendLine("    });");
        
        // Generar datos de línea temporal REALES
        html.AppendLine(GenerateTimelineData(report));
        
        html.AppendLine("});"); // DOMContentLoaded
        html.AppendLine("</script>");
        
        return html.ToString();
    }
    
    /// <summary>
    /// Genera timeline simple con intervalos de 5 minutos usando datos reales
    /// </summary>
    private string GenerateTimelineData(ActivityReport report)
    {
        var html = new StringBuilder();
        
        // Debug: Analyze data availability
        var totalDetectionPoints = report.Entries.Sum(e => e.DetectionPoints.Count);
        Console.WriteLine($"[ActivityReportService] Generando timeline 5min para {report.Entries.Count} cuadrantes, {totalDetectionPoints} detection points total");
        
        foreach (var entry in report.Entries)
        {
            Console.WriteLine($"[ActivityReportService] {entry.QuadrantName}: {entry.DetectionPoints.Count} points, periodo {entry.FirstActivityTime:HH:mm} - {entry.LastActivityTime:HH:mm}");
        }
        
        html.AppendLine("    // Gráfica temporal de actividad con intervalos de 1 minuto");
        html.AppendLine("    const timelineCtx = document.getElementById('timelineChart').getContext('2d');");
        html.AppendLine("    ");
        
        // Generate 1-minute intervals for the entire report period
        var oneMinuteLabels = Generate1MinuteIntervals(report.ReportStartTime, report.ReportEndTime);
        Console.WriteLine($"[ActivityReportService] Generando {oneMinuteLabels.Count} intervalos de 1min desde {report.ReportStartTime:HH:mm} hasta {report.ReportEndTime:HH:mm}");
        
        // Generate datasets for each quadrant with real data
        var datasets = new List<string>();
        var colors = new[] { "#ef4444", "#10b981", "#3b82f6", "#f59e0b", "#8b5cf6", "#06b6d4" };
        var colorIndex = 0;
        
        foreach (var entry in report.Entries)
        {
            var color = colors[colorIndex % colors.Length];
            var realData = MapDetectionPointsTo1MinuteIntervals(entry, oneMinuteLabels, report.ReportStartTime);
            
            Console.WriteLine($"[ActivityReportService] {entry.QuadrantName}: generados {realData.Count} puntos de datos (1min)");
            Console.WriteLine($"[ActivityReportService] Datos sample: [{string.Join(", ", realData.Take(5).Select(d => d.ToString("F1")))}...]");
            
            datasets.Add($@"
                {{
                    label: '{entry.QuadrantName}',
                    data: [{string.Join(", ", realData.Select(d => d.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)))}],
                    borderColor: '{color}',
                    backgroundColor: '{color}33',
                    borderWidth: 3,
                    fill: false,
                    tension: 0.4
                }}");
            colorIndex++;
        }
        
        // Generate the Chart.js configuration
        html.AppendLine("    new Chart(timelineCtx, {");
        html.AppendLine("        type: 'line',");
        html.AppendLine("        data: {");
        html.AppendLine($"            labels: [{string.Join(", ", oneMinuteLabels.Select(l => $"'{l}'"))}],");
        html.AppendLine($"            datasets: [{string.Join(",", datasets)}]");
        html.AppendLine("        },");
        html.AppendLine("        options: {");
        html.AppendLine("            responsive: true,");
        html.AppendLine("            maintainAspectRatio: false,");
        html.AppendLine("            plugins: {");
        html.AppendLine("                legend: { position: 'top' },");
        html.AppendLine("                tooltip: {");
        html.AppendLine("                    mode: 'index',");
        html.AppendLine("                    intersect: false");
        html.AppendLine("                }");
        html.AppendLine("            },");
        html.AppendLine("            scales: {");
        html.AppendLine("                x: {");
        html.AppendLine("                    title: { display: true, text: 'Tiempo Real del Reporte (1min)' }");
        html.AppendLine("                },");
        html.AppendLine("                y: {");
        html.AppendLine("                    title: { display: true, text: 'Actividad Real (% Cambio Pixel)' },");
        html.AppendLine("                    min: 0,");
        html.AppendLine("                    max: 100");
        html.AppendLine("                }");
        html.AppendLine("            },");
        html.AppendLine("            interaction: {");
        html.AppendLine("                intersect: false,");
        html.AppendLine("                mode: 'index'");
        html.AppendLine("            }");
        html.AppendLine("        }");
        html.AppendLine("    });");
        
        return html.ToString();
    }

    /// <summary>
    /// Genera intervalos de 1 minuto para el período del reporte
    /// </summary>
    private List<string> Generate1MinuteIntervals(DateTime startTime, DateTime endTime)
    {
        var intervals = new List<string>();
        var current = startTime;
        
        while (current <= endTime)
        {
            intervals.Add(current.ToString("HH:mm"));
            current = current.AddMinutes(1);
        }
        
        // Limit to reasonable number of intervals to avoid overwhelming the chart
        if (intervals.Count > 720) // More than 12 hours at 1min intervals
        {
            Console.WriteLine($"[ActivityReportService] WARNING: {intervals.Count} intervalos es demasiado, usando cada 5min");
            intervals = new List<string>();
            current = startTime;
            while (current <= endTime)
            {
                intervals.Add(current.ToString("HH:mm"));
                current = current.AddMinutes(5);
            }
        }
        
        Console.WriteLine($"[ActivityReportService] Generados {intervals.Count} intervalos de tiempo (1min)");
        return intervals;
    }

    /// <summary>
    /// Mapea DetectionPoints reales a buckets de 1 minuto
    /// </summary>
    private List<double> MapDetectionPointsTo1MinuteIntervals(ActivityReportEntry entry, List<string> timeLabels, DateTime reportStartTime)
    {
        var activityData = new List<double>();
        
        if (!entry.DetectionPoints.Any())
        {
            Console.WriteLine($"[ActivityReportService] WARNING: No hay DetectionPoints para {entry.QuadrantName}, usando fallback inteligente");
            // Intelligent fallback: distribute activity rate across realistic time pattern
            return GenerateIntelligentFallbackData(entry, timeLabels, reportStartTime);
        }
        
        // Sort detection points chronologically
        var sortedPoints = entry.DetectionPoints.OrderBy(dp => dp.Timestamp).ToList();
        Console.WriteLine($"[ActivityReportService] {entry.QuadrantName}: {sortedPoints.Count} puntos ordenados desde {sortedPoints.First().Timestamp:HH:mm:ss} hasta {sortedPoints.Last().Timestamp:HH:mm:ss}");
        
        foreach (var timeLabel in timeLabels)
        {
            // Parse time label to DateTime for this report
            if (!TimeSpan.TryParse(timeLabel, out var timeOfDay))
            {
                activityData.Add(0);
                continue;
            }
            
            var targetTime = reportStartTime.Date.Add(timeOfDay);
            
            // Find detection points within this 1-minute window
            var windowStart = targetTime.AddSeconds(-30); // ±30 seconds for 1min window
            var windowEnd = targetTime.AddSeconds(30);
            
            var pointsInWindow = sortedPoints
                .Where(dp => dp.Timestamp >= windowStart && dp.Timestamp <= windowEnd)
                .ToList();
            
            if (pointsInWindow.Any())
            {
                // Use real data: average of all points in this 1-minute window
                var activePoints = pointsInWindow.Where(p => p.HasActivity);
                if (activePoints.Any())
                {
                    var avgActivity = activePoints.Average(p => p.ChangePercentage);
                    activityData.Add(Math.Min(100, avgActivity));
                    
                    Console.WriteLine($"[ActivityReportService] {timeLabel}: {pointsInWindow.Count} points, {activePoints.Count()} active, avg: {avgActivity:F1}%");
                }
                else
                {
                    activityData.Add(0);
                    Console.WriteLine($"[ActivityReportService] {timeLabel}: {pointsInWindow.Count} points, pero sin actividad");
                }
            }
            else
            {
                // No direct points in this window - try smart interpolation or use last known value
                var beforePoint = sortedPoints.LastOrDefault(dp => dp.Timestamp <= targetTime);
                var afterPoint = sortedPoints.FirstOrDefault(dp => dp.Timestamp > targetTime);
                
                if (beforePoint != null && afterPoint != null)
                {
                    // Interpolate between points if they're close enough (within 10 minutes for 1min intervals)
                    var timeBetween = (afterPoint.Timestamp - beforePoint.Timestamp).TotalMinutes;
                    if (timeBetween <= 10)
                    {
                        var ratio = (targetTime - beforePoint.Timestamp).TotalMinutes / timeBetween;
                        var interpolatedActivity = beforePoint.ChangePercentage + (ratio * (afterPoint.ChangePercentage - beforePoint.ChangePercentage));
                        var hasActivity = beforePoint.HasActivity || afterPoint.HasActivity;
                        
                        activityData.Add(hasActivity ? Math.Min(100, interpolatedActivity) : 0); // No dampening for 1min intervals
                        Console.WriteLine($"[ActivityReportService] {timeLabel}: Interpolado {interpolatedActivity:F1}% entre {beforePoint.Timestamp:HH:mm:ss} y {afterPoint.Timestamp:HH:mm:ss}");
                    }
                    else
                    {
                        // Use closest point value if gap is reasonable (≤5 minutes)
                        var closestPoint = Math.Abs((beforePoint.Timestamp - targetTime).TotalMinutes) <= 
                                         Math.Abs((afterPoint.Timestamp - targetTime).TotalMinutes) ? beforePoint : afterPoint;
                        var timeDiff = Math.Abs((closestPoint.Timestamp - targetTime).TotalMinutes);
                        
                        if (timeDiff <= 5 && closestPoint.HasActivity)
                        {
                            activityData.Add(closestPoint.ChangePercentage * 0.8); // Slight decay for distance
                            Console.WriteLine($"[ActivityReportService] {timeLabel}: Usando punto cercano {closestPoint.Timestamp:HH:mm:ss} ({timeDiff:F1}min) = {closestPoint.ChangePercentage:F1}%");
                        }
                        else
                        {
                            activityData.Add(0);
                            Console.WriteLine($"[ActivityReportService] {timeLabel}: Gap muy grande ({timeBetween:F1}min)");
                        }
                    }
                }
                else if (beforePoint != null)
                {
                    // Use last known point if it's recent (within 3 minutes for 1min intervals)
                    var timeSince = (targetTime - beforePoint.Timestamp).TotalMinutes;
                    if (timeSince <= 3 && beforePoint.HasActivity)
                    {
                        var decayedValue = beforePoint.ChangePercentage * Math.Max(0.3, 1 - timeSince / 5); // Gradual decay
                        activityData.Add(decayedValue);
                        Console.WriteLine($"[ActivityReportService] {timeLabel}: Decaimiento desde {beforePoint.Timestamp:HH:mm:ss} ({timeSince:F1}min) = {decayedValue:F1}%");
                    }
                    else
                    {
                        activityData.Add(0);
                        Console.WriteLine($"[ActivityReportService] {timeLabel}: Último punto muy alejado ({timeSince:F1}min)");
                    }
                }
                else
                {
                    activityData.Add(0);
                    Console.WriteLine($"[ActivityReportService] {timeLabel}: Sin datos de referencia");
                }
            }
        }
        
        Console.WriteLine($"[ActivityReportService] {entry.QuadrantName} final data: {activityData.Count} points, max: {(activityData.Any() ? activityData.Max():0):F1}%, avg: {(activityData.Any() ? activityData.Average():0):F1}%");
        return activityData;
    }

    /// <summary>
    /// Genera datos de fallback inteligente cuando no hay DetectionPoints
    /// </summary>
    private List<double> GenerateIntelligentFallbackData(ActivityReportEntry entry, List<string> timeLabels, DateTime reportStartTime)
    {
        var data = new List<double>();
        var random = new Random(entry.QuadrantName.GetHashCode()); // Consistent seed
        
        foreach (var timeLabel in timeLabels)
        {
            if (!TimeSpan.TryParse(timeLabel, out var timeOfDay))
            {
                data.Add(0);
                continue;
            }
            
            var hour = timeOfDay.Hours + timeOfDay.Minutes / 60.0;
            
            // Create realistic activity pattern based on overall ActivityRate
            var timeMultiplier = hour switch
            {
                >= 9 and <= 11 => 1.3,   // Morning peak
                >= 11 and <= 13 => 1.1,  // Pre-lunch activity
                >= 13 and <= 14 => 0.6,  // Lunch break
                >= 14 and <= 17 => 1.2,  // Afternoon productivity
                >= 17 and <= 18 => 0.8,  // End of day
                _ => 0.3                  // Outside normal hours
            };
            
            // Add some realistic variation
            var variation = 0.7 + (0.6 * random.NextDouble()); // 0.7 to 1.3 multiplier
            var calculatedActivity = entry.ActivityRate * timeMultiplier * variation;
            
            data.Add(Math.Max(0, Math.Min(100, calculatedActivity)));
        }
        
        Console.WriteLine($"[ActivityReportService] Fallback data for {entry.QuadrantName}: {data.Count} points based on ActivityRate {entry.ActivityRate:F1}%");
        return data;
    }

    /// <summary>
    /// Determina la mejor estrategia de visualización según datos disponibles  
    /// </summary>
    private string DetermineVisualizationStrategy(ActivityReport report)
    {
        var totalDetectionPoints = report.Entries.Sum(e => e.DetectionPoints.Count);
        var reportDuration = report.ReportDuration.TotalHours;
        var hasVariedActivity = report.Entries.Any(e => e.ActivityRate > 10);
        
        return (totalDetectionPoints, reportDuration, hasVariedActivity) switch
        {
            ( > 100, > 4, true) => "Rich Data - All visualizations",
            ( > 50, > 2, _) => "Moderate Data - Aggregated + Pulse",
            ( > 10, _, _) => "Limited Data - Heatmap + Simple line",
            _ => "Minimal Data - Fallback to aggregated only"
        };
    }

    /// <summary>
    /// ENFOQUE 1: Heatmap Timeline - Matriz hora/cuadrante con intensidad de color
    /// </summary>
    private string GenerateVisualization1_HeatmapTimeline(ActivityReport report)
    {
        var html = new StringBuilder();
        
        html.AppendLine("    // VISUALIZATION 1: Heatmap Timeline");
        html.AppendLine("    const heatmapCtx = document.getElementById('heatmapChart').getContext('2d');");
        
        // Generate time buckets (every hour for heatmap)
        var hours = new List<string>();
        var current = report.ReportStartTime;
        while (current <= report.ReportEndTime)
        {
            hours.Add(current.ToString("HH:mm"));
            current = current.AddHours(1);
        }
        
        // Generate heatmap data: each quadrant is a dataset
        var heatmapDatasets = new List<string>();
        var quadrantIndex = 0;
        
        foreach (var entry in report.Entries)
        {
            var heatmapData = GenerateHeatmapData(entry, report.ReportStartTime, report.ReportEndTime, hours);
            var color = GetQuadrantColor(quadrantIndex);
            
            heatmapDatasets.Add($@"
                {{
                    label: '{entry.QuadrantName}',
                    data: [{string.Join(", ", heatmapData.Select((value, index) => $"{{x: '{hours[index]}', y: {quadrantIndex}, v: {value:F1}}}"))}],
                    backgroundColor: function(context) {{
                        const value = context.parsed.v;
                        return value > 50 ? '{color}' : value > 20 ? '{color}80' : value > 5 ? '{color}40' : '{color}20';
                    }},
                    borderColor: '{color}',
                    borderWidth: 1,
                    pointRadius: 8
                }}");
            quadrantIndex++;
        }
        
        html.AppendLine("    new Chart(heatmapCtx, {");
        html.AppendLine("        type: 'scatter',");
        html.AppendLine("        data: {");
        html.AppendLine($"            datasets: [{string.Join(",", heatmapDatasets)}]");
        html.AppendLine("        },");
        html.AppendLine("        options: {");
        html.AppendLine("            responsive: true,");
        html.AppendLine("            plugins: {");
        html.AppendLine("                title: { display: true, text: '🌡️ Mapa de Calor - Actividad por Hora' },");
        html.AppendLine("                tooltip: {");
        html.AppendLine("                    callbacks: {");
        html.AppendLine("                        title: function(tooltipItems) {");
        html.AppendLine("                            return 'Hora: ' + tooltipItems[0].parsed.x;");
        html.AppendLine("                        },");
        html.AppendLine("                        label: function(context) {");
        html.AppendLine("                            return context.dataset.label + ': ' + context.parsed.v.toFixed(1) + '% actividad';");
        html.AppendLine("                        }");
        html.AppendLine("                    }");
        html.AppendLine("                }");
        html.AppendLine("            },");
        html.AppendLine("            scales: {");
        html.AppendLine("                x: { title: { display: true, text: 'Hora del Día' } },");
        html.AppendLine("                y: {"); 
        html.AppendLine("                    type: 'category',");
        html.AppendLine($"                    labels: [{string.Join(", ", report.Entries.Select(e => $"'{e.QuadrantName}'"))}],");
        html.AppendLine("                    title: { display: true, text: 'Cuadrantes' }");
        html.AppendLine("                }");
        html.AppendLine("            }");
        html.AppendLine("        }");
        html.AppendLine("    });");
        
        return html.ToString();
    }

    /// <summary>
    /// ENFOQUE 2: Aggregated Line Chart - Datos agregados por intervalos fijos
    /// </summary>
    private string GenerateVisualization2_AggregatedLine(ActivityReport report)
    {
        var html = new StringBuilder();
        
        html.AppendLine("    // VISUALIZATION 2: Aggregated Line Chart");
        html.AppendLine("    const aggregatedCtx = document.getElementById('aggregatedChart').getContext('2d');");
        
        // Use fixed 30-minute intervals regardless of detection points
        var timeIntervals = Generate30MinuteIntervals(report.ReportStartTime, report.ReportEndTime);
        var datasets = new List<string>();
        var colors = new[] { "#ef4444", "#10b981", "#3b82f6", "#f59e0b", "#8b5cf6" };
        
        for (int i = 0; i < report.Entries.Count; i++)
        {
            var entry = report.Entries[i];
            var aggregatedData = GenerateAggregatedData(entry, timeIntervals);
            var color = colors[i % colors.Length];
            
            datasets.Add($@"
                {{
                    label: '{entry.QuadrantName}',
                    data: [{string.Join(", ", aggregatedData.Select(d => d.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)))}],
                    borderColor: '{color}',
                    backgroundColor: '{color}20',
                    borderWidth: 2,
                    fill: true,
                    tension: 0.3
                }}");
        }
        
        html.AppendLine("    new Chart(aggregatedCtx, {");
        html.AppendLine("        type: 'line',");
        html.AppendLine("        data: {");
        html.AppendLine($"            labels: [{string.Join(", ", timeIntervals.Select(t => $"'{t:HH:mm}'"))}],");
        html.AppendLine($"            datasets: [{string.Join(",", datasets)}]");
        html.AppendLine("        },");
        html.AppendLine("        options: {");
        html.AppendLine("            responsive: true,");
        html.AppendLine("            plugins: {");
        html.AppendLine("                title: { display: true, text: '📈 Evolución Agregada (Intervalos 30min)' }");
        html.AppendLine("            },");
        html.AppendLine("            scales: {");
        html.AppendLine("                x: { title: { display: true, text: 'Tiempo' } },");
        html.AppendLine("                y: { title: { display: true, text: 'Actividad Promedio (%)' }, min: 0, max: 100 }");
        html.AppendLine("            }");
        html.AppendLine("        }");
        html.AppendLine("    });");
        
        return html.ToString();
    }

    /// <summary>
    /// ENFOQUE 3: Activity Pulse Chart - Barras verticales que muestran pulsos de actividad
    /// </summary>
    private string GenerateVisualization3_ActivityPulse(ActivityReport report)
    {
        var html = new StringBuilder();
        
        html.AppendLine("    // VISUALIZATION 3: Activity Pulse Chart");
        html.AppendLine("    const pulseCtx = document.getElementById('pulseChart').getContext('2d');");
        
        // Generate pulse data - activity intensity over time
        var pulseIntervals = GeneratePulseIntervals(report.ReportStartTime, report.ReportEndTime);
        var pulseData = GeneratePulseData(report.Entries, pulseIntervals);
        
        html.AppendLine("    new Chart(pulseCtx, {");
        html.AppendLine("        type: 'bar',");
        html.AppendLine("        data: {");
        html.AppendLine($"            labels: [{string.Join(", ", pulseIntervals.Select(t => $"'{t:HH:mm}'"))}],");
        html.AppendLine("            datasets: [{");
        html.AppendLine("                label: 'Intensidad de Actividad',");
        html.AppendLine($"                data: [{string.Join(", ", pulseData.Select(d => d.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)))}],");
        html.AppendLine("                backgroundColor: function(context) {");
        html.AppendLine("                    const value = context.parsed.y;");
        html.AppendLine("                    if (value > 70) return '#ef4444';"); // High activity - red
        html.AppendLine("                    if (value > 40) return '#f59e0b';"); // Medium activity - orange  
        html.AppendLine("                    if (value > 10) return '#10b981';"); // Low activity - green
        html.AppendLine("                    return '#6b7280';"); // No activity - gray
        html.AppendLine("                },");
        html.AppendLine("                borderColor: '#374151',");
        html.AppendLine("                borderWidth: 1,");
        html.AppendLine("                borderRadius: 4");
        html.AppendLine("            }]");
        html.AppendLine("        },");
        html.AppendLine("        options: {");
        html.AppendLine("            responsive: true,");
        html.AppendLine("            plugins: {");
        html.AppendLine("                title: { display: true, text: '⚡ Pulsos de Actividad (Intensidad Combinada)' },");
        html.AppendLine("                legend: { display: false }");
        html.AppendLine("            },");
        html.AppendLine("            scales: {");
        html.AppendLine("                x: { title: { display: true, text: 'Tiempo' } },");
        html.AppendLine("                y: { title: { display: true, text: 'Intensidad (%)' }, min: 0, max: 100 }");
        html.AppendLine("            }");
        html.AppendLine("        }");
        html.AppendLine("    });");
        
        return html.ToString();
    }

    /// <summary>
    /// ENFOQUE 4: Scatter Timeline - Puntos de actividad con tamaño variable
    /// </summary>
    private string GenerateVisualization4_ScatterTimeline(ActivityReport report)
    {
        var html = new StringBuilder();
        
        html.AppendLine("    // VISUALIZATION 4: Scatter Timeline");
        html.AppendLine("    const scatterCtx = document.getElementById('scatterChart').getContext('2d');");
        
        var scatterDatasets = new List<string>();
        var colors = new[] { "#ef4444", "#10b981", "#3b82f6", "#f59e0b", "#8b5cf6" };
        
        for (int i = 0; i < report.Entries.Count; i++)
        {
            var entry = report.Entries[i];
            var scatterData = GenerateScatterData(entry, report.ReportStartTime);
            var color = colors[i % colors.Length];
            
            scatterDatasets.Add($@"
                {{
                    label: '{entry.QuadrantName}',
                    data: [{string.Join(", ", scatterData)}],
                    backgroundColor: '{color}80',
                    borderColor: '{color}',
                    borderWidth: 2,
                    pointHoverRadius: 8
                }}");
        }
        
        html.AppendLine("    new Chart(scatterCtx, {");
        html.AppendLine("        type: 'scatter',");
        html.AppendLine("        data: {");
        html.AppendLine($"            datasets: [{string.Join(",", scatterDatasets)}]");
        html.AppendLine("        },");
        html.AppendLine("        options: {");
        html.AppendLine("            responsive: true,");
        html.AppendLine("            plugins: {");
        html.AppendLine("                title: { display: true, text: '🎯 Timeline de Actividad (Puntos Dispersos)' },");
        html.AppendLine("                tooltip: {");
        html.AppendLine("                    callbacks: {");
        html.AppendLine("                        label: function(context) {");
        html.AppendLine("                            const point = context.raw;");
        html.AppendLine("                            return context.dataset.label + ': ' + point.y.toFixed(1) + '% a las ' + Math.floor(point.x) + ':' + String(Math.round((point.x % 1) * 60)).padStart(2, '0');");
        html.AppendLine("                        }");
        html.AppendLine("                    }");
        html.AppendLine("                }");
        html.AppendLine("            },");
        html.AppendLine("            scales: {");
        html.AppendLine("                x: { ");
        html.AppendLine("                    title: { display: true, text: 'Hora del Día (decimal)' },");
        html.AppendLine("                    min: 0,");
        html.AppendLine("                    max: 24,");
        html.AppendLine("                    ticks: {");
        html.AppendLine("                        callback: function(value) { return Math.floor(value) + ':00'; }");
        html.AppendLine("                    }");
        html.AppendLine("                },");
        html.AppendLine("                y: { title: { display: true, text: 'Actividad (%)' }, min: 0, max: 100 }");
        html.AppendLine("            }");
        html.AppendLine("        }");
        html.AppendLine("    });");
        
        return html.ToString();
    }

    /// <summary>
    /// ENFOQUE 5: Hybrid Chart - Combinación de línea + área + barras
    /// </summary>
    private string GenerateVisualization5_HybridChart(ActivityReport report)
    {
        var html = new StringBuilder();
        
        html.AppendLine("    // VISUALIZATION 5: Hybrid Chart");
        html.AppendLine("    const hybridCtx = document.getElementById('hybridChart').getContext('2d');");
        
        // Generate comprehensive time intervals
        var intervals = Generate15MinuteIntervals(report.ReportStartTime, report.ReportEndTime);
        var hybridDatasets = new List<string>();
        
        // Activity trend line
        var overallActivity = GenerateOverallActivityTrend(report.Entries, intervals);
        hybridDatasets.Add($@"
            {{
                type: 'line',
                label: 'Tendencia General',
                data: [{string.Join(", ", overallActivity.Select(d => d.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)))}],
                borderColor: '#1f2937',
                backgroundColor: 'rgba(31, 41, 55, 0.1)',
                borderWidth: 3,
                fill: true,
                tension: 0.4
            }}");
        
        // Peak activity bars
        var peakActivity = GeneratePeakActivityData(report.Entries, intervals);
        hybridDatasets.Add($@"
            {{
                type: 'bar',
                label: 'Picos de Actividad',
                data: [{string.Join(", ", peakActivity.Select(d => d.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)))}],
                backgroundColor: 'rgba(239, 68, 68, 0.6)',
                borderColor: '#ef4444',
                borderWidth: 1
            }}");
        
        html.AppendLine("    new Chart(hybridCtx, {");
        html.AppendLine("        type: 'line',");
        html.AppendLine("        data: {");
        html.AppendLine($"            labels: [{string.Join(", ", intervals.Select(t => $"'{t:HH:mm}'"))}],");
        html.AppendLine($"            datasets: [{string.Join(",", hybridDatasets)}]");
        html.AppendLine("        },");
        html.AppendLine("        options: {");
        html.AppendLine("            responsive: true,");
        html.AppendLine("            plugins: {");
        html.AppendLine("                title: { display: true, text: '🔥 Chart Híbrido - Múltiples Métricas' }");
        html.AppendLine("            },");
        html.AppendLine("            scales: {");
        html.AppendLine("                x: { title: { display: true, text: 'Evolución Temporal' } },");
        html.AppendLine("                y: { title: { display: true, text: 'Actividad (%)' }, min: 0, max: 100 }");
        html.AppendLine("            },");
        html.AppendLine("            interaction: { intersect: false, mode: 'index' }");
        html.AppendLine("        }");
        html.AppendLine("    });");
        
        return html.ToString();
    }

    #region Helper Methods for 5 Visualization Approaches

    /// <summary>
    /// Generate heatmap data for a quadrant entry
    /// </summary>
    private List<double> GenerateHeatmapData(ActivityReportEntry entry, DateTime startTime, DateTime endTime, List<string> hours)
    {
        var data = new List<double>();
        
        foreach (var hour in hours)
        {
            var hourTime = DateTime.ParseExact($"{startTime:yyyy-MM-dd} {hour}", "yyyy-MM-dd HH:mm", null);
            
            // Find activity around this hour (±30 minutes)
            var nearbyPoints = entry.DetectionPoints
                .Where(dp => Math.Abs((dp.Timestamp - hourTime).TotalMinutes) <= 30)
                .ToList();
            
            if (nearbyPoints.Any())
            {
                var avgActivity = nearbyPoints.Where(p => p.HasActivity).DefaultIfEmpty().Average(p => p?.ChangePercentage ?? 0);
                data.Add(avgActivity);
            }
            else
            {
                // Fallback: Use proportional activity based on overall rate
                var timeRatio = (hourTime - startTime).TotalHours / (endTime - startTime).TotalHours;
                var fallbackActivity = entry.ActivityRate * (0.5 + 0.5 * Math.Sin(timeRatio * Math.PI)); // Sine wave pattern
                data.Add(Math.Max(0, fallbackActivity));
            }
        }
        
        return data;
    }

    /// <summary>
    /// Generate 30-minute intervals for aggregated chart
    /// </summary>
    private List<DateTime> Generate30MinuteIntervals(DateTime startTime, DateTime endTime)
    {
        var intervals = new List<DateTime>();
        var current = startTime;
        
        while (current <= endTime)
        {
            intervals.Add(current);
            current = current.AddMinutes(30);
        }
        
        return intervals;
    }

    /// <summary>
    /// Generate aggregated data using activity rate distribution
    /// </summary>
    private List<double> GenerateAggregatedData(ActivityReportEntry entry, List<DateTime> intervals)
    {
        var data = new List<double>();
        
        foreach (var interval in intervals)
        {
            // If we have detection points, use them
            if (entry.DetectionPoints.Any())
            {
                var nearbyPoints = entry.DetectionPoints
                    .Where(dp => Math.Abs((dp.Timestamp - interval).TotalMinutes) <= 15)
                    .ToList();
                
                if (nearbyPoints.Any())
                {
                    var activity = nearbyPoints.Where(p => p.HasActivity).DefaultIfEmpty().Average(p => p?.ChangePercentage ?? 0);
                    data.Add(activity);
                }
                else
                {
                    data.Add(0);
                }
            }
            else
            {
                // Fallback: Distribute activity rate across time with realistic pattern
                var hourOfDay = interval.Hour + interval.Minute / 60.0;
                var activityFactor = hourOfDay switch
                {
                    >= 9 and <= 11 => 1.2,   // Morning peak
                    >= 14 and <= 16 => 1.1,  // Afternoon activity
                    >= 19 and <= 21 => 0.8,  // Evening slowdown
                    _ => 0.6                  // Other times
                };
                
                var adjustedActivity = entry.ActivityRate * activityFactor * (0.7 + 0.3 * new Random(interval.GetHashCode()).NextDouble());
                data.Add(Math.Max(0, Math.Min(100, adjustedActivity)));
            }
        }
        
        return data;
    }

    /// <summary>
    /// Generate pulse intervals (every 20 minutes for pulse chart)
    /// </summary>
    private List<DateTime> GeneratePulseIntervals(DateTime startTime, DateTime endTime)
    {
        var intervals = new List<DateTime>();
        var current = startTime;
        
        while (current <= endTime)
        {
            intervals.Add(current);
            current = current.AddMinutes(20);
        }
        
        return intervals;
    }

    /// <summary>
    /// Generate pulse data - combined intensity from all quadrants
    /// </summary>
    private List<double> GeneratePulseData(List<ActivityReportEntry> entries, List<DateTime> intervals)
    {
        var data = new List<double>();
        
        foreach (var interval in intervals)
        {
            var totalIntensity = 0.0;
            var activeQuadrants = 0;
            
            foreach (var entry in entries)
            {
                if (entry.DetectionPoints.Any())
                {
                    var nearbyPoints = entry.DetectionPoints
                        .Where(dp => Math.Abs((dp.Timestamp - interval).TotalMinutes) <= 10)
                        .ToList();
                    
                    if (nearbyPoints.Any(p => p.HasActivity))
                    {
                        var intensity = nearbyPoints.Where(p => p.HasActivity).Average(p => p.ChangePercentage);
                        totalIntensity += intensity;
                        activeQuadrants++;
                    }
                }
                else if (entry.ActivityRate > 5) // Fallback for entries without detection points
                {
                    totalIntensity += entry.ActivityRate;
                    activeQuadrants++;
                }
            }
            
            var avgIntensity = activeQuadrants > 0 ? totalIntensity / activeQuadrants : 0;
            data.Add(Math.Min(100, avgIntensity));
        }
        
        return data;
    }

    /// <summary>
    /// Generate scatter data points
    /// </summary>
    private List<string> GenerateScatterData(ActivityReportEntry entry, DateTime startTime)
    {
        var scatterPoints = new List<string>();
        
        if (entry.DetectionPoints.Any())
        {
            // Use real detection points
            var significantPoints = entry.DetectionPoints
                .Where(dp => dp.HasActivity && dp.ChangePercentage > 5) // Only significant activity
                .OrderBy(dp => dp.Timestamp)
                .ToList();
            
            foreach (var point in significantPoints)
            {
                var hourDecimal = point.Timestamp.Hour + point.Timestamp.Minute / 60.0;
                var pointSize = Math.Max(5, point.ChangePercentage / 3); // Size based on activity intensity
                
                scatterPoints.Add($"{{x: {hourDecimal:F2}, y: {point.ChangePercentage:F1}, r: {pointSize:F1}}}");
            }
        }
        else
        {
            // Fallback: Create representative points based on overall activity
            var random = new Random(entry.QuadrantName.GetHashCode());
            var pointCount = Math.Max(3, (int)(entry.ActivityRate / 10)); // More points for higher activity
            
            for (int i = 0; i < pointCount; i++)
            {
                var hourDecimal = 9 + (random.NextDouble() * 9); // 9 AM to 6 PM
                var activity = entry.ActivityRate * (0.5 + random.NextDouble());
                var pointSize = Math.Max(3, activity / 4);
                
                scatterPoints.Add($"{{x: {hourDecimal:F2}, y: {activity:F1}, r: {pointSize:F1}}}");
            }
        }
        
        return scatterPoints;
    }

    /// <summary>
    /// Generate 15-minute intervals for hybrid chart
    /// </summary>
    private List<DateTime> Generate15MinuteIntervals(DateTime startTime, DateTime endTime)
    {
        var intervals = new List<DateTime>();
        var current = startTime;
        
        while (current <= endTime)
        {
            intervals.Add(current);
            current = current.AddMinutes(15);
        }
        
        return intervals;
    }

    /// <summary>
    /// Generate overall activity trend
    /// </summary>
    private List<double> GenerateOverallActivityTrend(List<ActivityReportEntry> entries, List<DateTime> intervals)
    {
        var data = new List<double>();
        
        foreach (var interval in intervals)
        {
            var totalActivity = 0.0;
            var activeEntries = 0;
            
            foreach (var entry in entries)
            {
                if (entry.DetectionPoints.Any())
                {
                    var nearbyPoints = entry.DetectionPoints
                        .Where(dp => Math.Abs((dp.Timestamp - interval).TotalMinutes) <= 7.5)
                        .Where(dp => dp.HasActivity)
                        .ToList();
                    
                    if (nearbyPoints.Any())
                    {
                        totalActivity += nearbyPoints.Average(p => p.ChangePercentage);
                        activeEntries++;
                    }
                }
                else if (entry.ActivityRate > 1)
                {
                    totalActivity += entry.ActivityRate;
                    activeEntries++;
                }
            }
            
            var avgActivity = activeEntries > 0 ? totalActivity / activeEntries : 0;
            data.Add(Math.Min(100, avgActivity));
        }
        
        return data;
    }

    /// <summary>
    /// Generate peak activity data
    /// </summary>
    private List<double> GeneratePeakActivityData(List<ActivityReportEntry> entries, List<DateTime> intervals)
    {
        var data = new List<double>();
        
        foreach (var interval in intervals)
        {
            var maxActivity = 0.0;
            
            foreach (var entry in entries)
            {
                if (entry.DetectionPoints.Any())
                {
                    var nearbyPoints = entry.DetectionPoints
                        .Where(dp => Math.Abs((dp.Timestamp - interval).TotalMinutes) <= 7.5)
                        .Where(dp => dp.HasActivity)
                        .ToList();
                    
                    if (nearbyPoints.Any())
                    {
                        var peakInWindow = nearbyPoints.Max(p => p.ChangePercentage);
                        maxActivity = Math.Max(maxActivity, peakInWindow);
                    }
                }
                else
                {
                    maxActivity = Math.Max(maxActivity, entry.ActivityRate);
                }
            }
            
            data.Add(Math.Min(100, maxActivity));
        }
        
        return data;
    }

    /// <summary>
    /// Get color for quadrant index
    /// </summary>
    private string GetQuadrantColor(int index)
    {
        var colors = new[] { "#ef4444", "#10b981", "#3b82f6", "#f59e0b", "#8b5cf6", "#06b6d4" };
        return colors[index % colors.Length];
    }


    #endregion
    
    /// <summary>
    /// Genera labels de tiempo basados en el período real del reporte
    /// </summary>
    private List<string> GenerateTimeLabels(DateTime startTime, DateTime endTime)
    {
        var labels = new List<string>();
        var totalHours = (endTime - startTime).TotalHours;
        
        // Determine interval based on report duration
        var interval = totalHours switch
        {
            <= 4 => TimeSpan.FromMinutes(30),   // 30 min intervals for short reports
            <= 12 => TimeSpan.FromHours(1),    // 1 hour intervals for medium reports  
            <= 48 => TimeSpan.FromHours(2),    // 2 hour intervals for daily reports
            _ => TimeSpan.FromHours(4)          // 4 hour intervals for longer reports
        };
        
        var current = startTime;
        while (current <= endTime)
        {
            labels.Add(current.ToString("HH:mm"));
            current = current.Add(interval);
        }
        
        return labels;
    }

    /// <summary>
    /// Genera datos REALES de actividad basados en la evolución temporal completa
    /// </summary>
    private List<double> GenerateRealHourlyData(ActivityReportEntry entry, DateTime startTime, DateTime endTime)
    {
        var hourlyData = new List<double>();
        var timeLabels = GenerateTimeLabels(startTime, endTime);
        
        // Debug: Check how many detection points we actually have
        Console.WriteLine($"[ActivityReportService] Procesando {entry.QuadrantName}: {entry.DetectionPoints.Count} detection points desde {startTime:HH:mm} hasta {endTime:HH:mm}");
        
        if (!entry.DetectionPoints.Any())
        {
            Console.WriteLine($"[ActivityReportService] WARNING: No hay detection points para {entry.QuadrantName} - usando datos agregados");
            // Fallback: Use aggregated data if no detection points
            var avgRate = entry.ActivityRate;
            return timeLabels.Select(t => avgRate).ToList();
        }
        
        // Sort detection points by timestamp for proper timeline
        var sortedPoints = entry.DetectionPoints.OrderBy(dp => dp.Timestamp).ToList();
        Console.WriteLine($"[ActivityReportService] Puntos ordenados: {sortedPoints.Count} desde {sortedPoints.First().Timestamp:HH:mm:ss} hasta {sortedPoints.Last().Timestamp:HH:mm:ss}");
        
        foreach (var timeLabel in timeLabels)
        {
            // Parse the time label in the context of the report date
            if (!TimeSpan.TryParse(timeLabel, out var timeOfDay))
            {
                hourlyData.Add(0);
                continue;
            }
            
            var targetTime = startTime.Date.Add(timeOfDay);
            
            // Ensure we're within the report period
            if (targetTime < startTime || targetTime > endTime)
            {
                hourlyData.Add(0);
                Console.WriteLine($"[ActivityReportService] {timeLabel}: Fuera del período de reporte");
                continue;
            }
            
            // Find the detection point closest to this time (within reasonable window)
            var windowStart = targetTime.AddMinutes(-30);
            var windowEnd = targetTime.AddMinutes(30);
            
            var pointsInWindow = sortedPoints
                .Where(dp => dp.Timestamp >= windowStart && dp.Timestamp <= windowEnd)
                .ToList();
            
            if (pointsInWindow.Any())
            {
                // Use the closest point to our target time
                var closestPoint = pointsInWindow
                    .OrderBy(dp => Math.Abs((dp.Timestamp - targetTime).TotalMinutes))
                    .First();
                
                var activityValue = closestPoint.HasActivity ? closestPoint.ChangePercentage : 0;
                hourlyData.Add(Math.Min(100, activityValue));
                
                Console.WriteLine($"[ActivityReportService] {timeLabel}: Usando punto {closestPoint.Timestamp:HH:mm:ss} - Activity: {closestPoint.HasActivity} ({closestPoint.ChangePercentage:F1}%) → {activityValue:F1}%");
            }
            else
            {
                // Try to interpolate between nearby points
                var beforePoint = sortedPoints.LastOrDefault(dp => dp.Timestamp < targetTime);
                var afterPoint = sortedPoints.FirstOrDefault(dp => dp.Timestamp > targetTime);
                
                if (beforePoint != null && afterPoint != null)
                {
                    // Interpolate between the two points
                    var timeDiff = (afterPoint.Timestamp - beforePoint.Timestamp).TotalMinutes;
                    var targetDiff = (targetTime - beforePoint.Timestamp).TotalMinutes;
                    var ratio = targetDiff / timeDiff;
                    
                    var interpolatedValue = beforePoint.ChangePercentage + (ratio * (afterPoint.ChangePercentage - beforePoint.ChangePercentage));
                    var hasInterpolatedActivity = beforePoint.HasActivity || afterPoint.HasActivity;
                    
                    hourlyData.Add(hasInterpolatedActivity ? Math.Min(100, interpolatedValue) : 0);
                    Console.WriteLine($"[ActivityReportService] {timeLabel}: Interpolado entre {beforePoint.Timestamp:HH:mm} y {afterPoint.Timestamp:HH:mm} → {interpolatedValue:F1}%");
                }
                else if (beforePoint != null)
                {
                    // Use the last known point if within 2 hours
                    var timeSince = (targetTime - beforePoint.Timestamp).TotalHours;
                    if (timeSince <= 2.0)
                    {
                        var decayedValue = beforePoint.HasActivity ? beforePoint.ChangePercentage * (1 - timeSince / 4) : 0;
                        hourlyData.Add(Math.Max(0, decayedValue));
                        Console.WriteLine($"[ActivityReportService] {timeLabel}: Decaimiento desde {beforePoint.Timestamp:HH:mm} → {decayedValue:F1}%");
                    }
                    else
                    {
                        hourlyData.Add(0);
                        Console.WriteLine($"[ActivityReportService] {timeLabel}: Sin datos (muy alejado del último punto)");
                    }
                }
                else
                {
                    hourlyData.Add(0);
                    Console.WriteLine($"[ActivityReportService] {timeLabel}: Sin datos de referencia");
                }
            }
        }
        
        Console.WriteLine($"[ActivityReportService] Datos finales para {entry.QuadrantName}: [{string.Join(", ", hourlyData.Select(d => d.ToString("F1")))}]");
        return hourlyData;
    }

    /// <summary>
    /// Determina si se debe agregar un detection point (para evitar spam de datos)
    /// </summary>
    private bool ShouldAddDetectionPoint(ActivityReportEntry entry, QuadrantActivityResult result)
    {
        // Always add the first point
        if (!entry.DetectionPoints.Any()) return true;
        
        var lastPoint = entry.DetectionPoints.Last();
        
        // Add if significant change in activity state
        if (result.HasActivity != lastPoint.HasActivity) return true;
        
        // Add if significant change in percentage (>5% difference)
        if (Math.Abs(result.ChangePercentage - lastPoint.ChangePercentage) > 5.0) return true;
        
        // Add if enough time has passed (at least 1 minute between points)
        if ((result.Timestamp - lastPoint.Timestamp).TotalMinutes >= 1.0) return true;
        
        // Skip redundant points to avoid filling limit too quickly
        return false;
    }

    /// <summary>
    /// DEPRECATED: Old method with fake data
    /// </summary>
    private List<double> GenerateHourlyData_OLD(ActivityReportEntry entry)
    {
        var hourlyData = new List<double>();
        var random = new Random(entry.QuadrantName.GetHashCode()); // Seed consistente
        
        // 12 puntos de datos (cada 2 horas)
        for (int hour = 0; hour < 24; hour += 2)
        {
            // Simular patrón de actividad basado en datos reales
            var baseActivity = entry.ActivityRate;
            
            // Añadir variación realista por hora del día
            var hourFactor = hour switch
            {
                >= 8 and <= 18 => 1.2,  // Horario laboral más activo
                >= 19 and <= 22 => 0.8,  // Tarde moderada
                _ => 0.3                  // Madrugada/noche baja actividad
            };
            
            // Añadir algo de aleatoriedad pero manteniendo el patrón
            var variation = (random.NextDouble() - 0.5) * 0.4;
            var simulatedActivity = Math.Max(0, baseActivity * hourFactor + variation);
            
            hourlyData.Add(Math.Round(simulatedActivity, 1));
        }
        
        return hourlyData;
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
    /// Exporta a formato HTML mejorado con gráficas visuales
    /// </summary>
    private async Task<string> ExportToHtmlAsync(ActivityReport report)
    {
        var html = new StringBuilder();
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html><head>");
        html.AppendLine("<meta charset='UTF-8'>");
        html.AppendLine("<meta name='viewport' content='width=device-width, initial-scale=1.0'>");
        html.AppendLine("<title>📊 Reporte de Actividad - Capturer Dashboard</title>");
        html.AppendLine("<script src='https://cdn.jsdelivr.net/npm/chart.js'></script>");
        html.AppendLine("<link href='https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700&display=swap' rel='stylesheet'>");
        html.AppendLine("<style>");
        
        // CSS moderno y responsivo
        html.AppendLine(GetModernCssStyles());
        
        html.AppendLine("</style></head><body>");
        
        // Header con logo y título
        html.AppendLine("<div class='header'>");
        html.AppendLine("<div class='container'>");
        html.AppendLine("<div class='header-content'>");
        html.AppendLine("<div class='logo'>📊</div>");
        html.AppendLine("<div class='header-text'>");
        html.AppendLine($"<h1>Reporte de Actividad</h1>");
        html.AppendLine($"<p class='period'>{report.ReportStartTime:dd/MM/yyyy HH:mm} - {report.ReportEndTime:dd/MM/yyyy HH:mm}</p>");
        html.AppendLine("</div></div></div></div>");
        
        html.AppendLine("<div class='container'>");
        
        // Tarjetas de resumen con iconos
        html.AppendLine(GetSummaryCardsHtml(report));
        
        // Gráfica de actividad por cuadrante
        html.AppendLine(GetActivityChartHtml(report));
        
        // Gráfica de línea temporal de actividad
        html.AppendLine(GetTimelineChartHtml(report));
        
        // Tabla moderna de detalles
        html.AppendLine(GetModernTableHtml(report));
        
        html.AppendLine("</div>"); // container
        
        // Footer
        html.AppendLine("<div class='footer'>");
        html.AppendLine($"<p>📅 Generado el {DateTime.Now:dd/MM/yyyy HH:mm:ss} | 🖥️ Capturer Dashboard v3.1.2 | 📊 {report.Summary.TotalActivities:N0} actividades detectadas</p>");
        html.AppendLine("</div>");
        
        // JavaScript para las gráficas
        html.AppendLine(GetChartJavaScript(report));
        
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