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
        html.AppendLine("<h2 class='chart-title'>📈 Evolución de Actividad durante el Día</h2>");
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
        
        // Generar datos de línea temporal simulados
        html.AppendLine("    // Gráfica temporal de actividad");
        html.AppendLine("    const timelineCtx = document.getElementById('timelineChart').getContext('2d');");
        
        // Generar datos simulados de actividad por hora del día
        html.AppendLine(GenerateTimelineData(report));
        
        html.AppendLine("});"); // DOMContentLoaded
        html.AppendLine("</script>");
        
        return html.ToString();
    }
    
    /// <summary>
    /// Genera datos temporales simulados para la gráfica de línea
    /// </summary>
    private string GenerateTimelineData(ActivityReport report)
    {
        var html = new StringBuilder();
        
        // Crear datasets para cada cuadrante
        var datasets = new List<string>();
        var colors = new[] { "#ef4444", "#10b981", "#3b82f6", "#f59e0b", "#8b5cf6", "#06b6d4" };
        var colorIndex = 0;
        
        foreach (var entry in report.Entries)
        {
            var color = colors[colorIndex % colors.Length];
            colorIndex++;
            
            // Simular datos de actividad por hora basados en los datos reales
            var hourlyData = GenerateHourlyData(entry);
            
            datasets.Add($@"
                {{
                    label: '{entry.QuadrantName}',
                    data: [{string.Join(", ", hourlyData)}],
                    borderColor: '{color}',
                    backgroundColor: '{color}33',
                    borderWidth: 3,
                    fill: false,
                    tension: 0.4
                }}");
        }
        
        html.AppendLine("    new Chart(timelineCtx, {");
        html.AppendLine("        type: 'line',");
        html.AppendLine("        data: {");
        html.AppendLine("            labels: ['00:00', '02:00', '04:00', '06:00', '08:00', '10:00', '12:00', '14:00', '16:00', '18:00', '20:00', '22:00'],");
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
        html.AppendLine("                    title: { display: true, text: 'Hora del Día' }");
        html.AppendLine("                },");
        html.AppendLine("                y: {");
        html.AppendLine("                    title: { display: true, text: 'Nivel de Actividad (%)' },");
        html.AppendLine("                    min: 0");
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
    /// Genera datos de actividad por hora para un cuadrante específico
    /// </summary>
    private List<double> GenerateHourlyData(ActivityReportEntry entry)
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
        html.AppendLine($"<p>📅 Generado el {DateTime.Now:dd/MM/yyyy HH:mm:ss} | 🖥️ Capturer Dashboard v2.4 | 📊 {report.Summary.TotalActivities:N0} actividades detectadas</p>");
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