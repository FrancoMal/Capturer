using System.Globalization;
using System.Text.RegularExpressions; // ★ NUEVO: Para extracción de fechas
using Capturer.Models;
using Capturer.Services;

namespace Capturer.Services;

/// <summary>
/// Servicio de programación avanzada para reportes diarios del ActivityDashboard
/// Integra configuración de email existente para generar reportes HTML por cuadrante
/// </summary>
public class ActivityDashboardSchedulerService : IDisposable
{
    private readonly ActivityReportService _reportService;
    private readonly QuadrantActivityService _activityService;
    private readonly CapturerConfiguration _config;
    private readonly IEmailService _emailService;
    private readonly System.Threading.Timer _dailyReportTimer;
    private readonly System.Threading.Timer _cleanupTimer; // ★ NUEVO: Timer para limpieza de reportes
    private readonly object _lockObject = new object();
    
    // Configuración específica del dashboard
    private ActivityDashboardScheduleConfig _dashboardConfig = new();
    private string _dashboardReportsFolder = "";

    public event EventHandler<DailyReportGeneratedEventArgs>? DailyReportGenerated;
    public event EventHandler<DailyReportEmailSentEventArgs>? DailyReportEmailSent;

    public ActivityDashboardSchedulerService(
        ActivityReportService reportService,
        QuadrantActivityService activityService,
        CapturerConfiguration config,
        IEmailService emailService)
    {
        _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
        _activityService = activityService ?? throw new ArgumentNullException(nameof(activityService));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        
        InitializeDashboardReportsFolder();
        
        // ★ NUEVO: Limpieza inicial de archivos antiguos del dashboard
        PerformInitialDashboardCleanup();
        
        LoadDashboardConfiguration();
        
        // Programar timer para reportes diarios
        _dailyReportTimer = new System.Threading.Timer(
            CheckAndGenerateDailyReport, 
            null, 
            TimeSpan.FromMinutes(1), // Primera verificación en 1 minuto
            TimeSpan.FromMinutes(15) // Verificar cada 15 minutos
        );
        
        // ★ NUEVO: Timer para limpieza periódica del dashboard (cada 6 horas)
        _cleanupTimer = new System.Threading.Timer(
            PerformPeriodicDashboardCleanup,
            null,
            TimeSpan.FromHours(6), // Primera limpieza en 6 horas
            TimeSpan.FromHours(6)  // Limpiar cada 6 horas
        );
        
        Console.WriteLine($"[ActivityDashboardScheduler] Servicio iniciado. Carpeta reportes: {_dashboardReportsFolder}");
    }

    /// <summary>
    /// Configura la programación de reportes diarios
    /// </summary>
    public void ConfigureDailyReports(ActivityDashboardScheduleConfig config)
    {
        lock (_lockObject)
        {
            _dashboardConfig = config;
            SaveDashboardConfiguration();
            Console.WriteLine($"[ActivityDashboardScheduler] Configuración actualizada: " +
                             $"Habilitado={config.IsEnabled}, Hora={config.ReportTime:HH\\:mm}, " +
                             $"Período={config.StartTime:HH\\:mm}-{config.EndTime:HH\\:mm}");
        }
    }

    /// <summary>
    /// Genera reporte diario manualmente para testing
    /// </summary>
    public async Task<List<string>> GenerateDailyReportManuallyAsync(DateTime? targetDate = null)
    {
        var reportDate = targetDate ?? DateTime.Now.Date.AddDays(-1);
        return await GenerateDailyReportAsync(reportDate);
    }
    
    /// <summary>
    /// Obtiene la configuración actual del scheduler
    /// </summary>
    public ActivityDashboardScheduleConfig GetCurrentConfiguration()
    {
        lock (_lockObject)
        {
            return _dashboardConfig;
        }
    }

    /// <summary>
    /// Envía email de prueba con los reportes del día actual
    /// </summary>
    public async Task<bool> SendTestEmailAsync()
    {
        try
        {
            if (!_dashboardConfig.EmailConfig.IsEnabled || !_dashboardConfig.EmailConfig.Recipients.Any())
            {
                Console.WriteLine("[ActivityDashboardScheduler] Email deshabilitado o sin destinatarios");
                return false;
            }

            // Generar reporte para hoy
            var today = DateTime.Now.Date;
            var generatedReports = await GenerateDailyReportAsync(today);
            
            if (!generatedReports.Any())
            {
                Console.WriteLine("[ActivityDashboardScheduler] No se generaron reportes para enviar");
                return false;
            }

            // Enviar email
            var success = await SendDailyReportEmailAsync(today, generatedReports, isTest: true);
            
            return success;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ActivityDashboardScheduler] Error enviando email de prueba: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Obtiene lista de reportes disponibles para selección
    /// </summary>
    public List<DashboardReportInfo> GetAvailableReports(DateTime startDate, DateTime endDate)
    {
        var reports = new List<DashboardReportInfo>();
        
        try
        {
            var reportFiles = GetGeneratedReports(startDate, endDate);
            
            foreach (var filePath in reportFiles)
            {
                var fileInfo = new FileInfo(filePath);
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                
                // Extraer información del nombre del archivo
                var reportInfo = new DashboardReportInfo
                {
                    FilePath = filePath,
                    FileName = fileName,
                    CreationDate = fileInfo.CreationTime,
                    Size = fileInfo.Length,
                    ReportType = fileName.Contains("Consolidado") ? "Consolidado" : "Individual",
                    QuadrantName = ExtractQuadrantNameFromFileName(fileName)
                };
                
                reports.Add(reportInfo);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ActivityDashboardScheduler] Error obteniendo reportes: {ex.Message}");
        }
        
        return reports.OrderByDescending(r => r.CreationDate).ToList();
    }

    /// <summary>
    /// Envía reportes seleccionados por email
    /// </summary>
    public async Task<bool> SendSelectedReportsAsync(List<string> selectedReports)
    {
        try
        {
            if (!_dashboardConfig.EmailConfig.IsEnabled || !_dashboardConfig.EmailConfig.Recipients.Any())
            {
                return false;
            }

            if (!selectedReports.Any())
            {
                return false;
            }

            return await SendCustomReportEmailAsync(selectedReports);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ActivityDashboardScheduler] Error enviando reportes seleccionados: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Verifica si debe generar reporte diario y lo genera
    /// </summary>
    private async void CheckAndGenerateDailyReport(object? state)
    {
        try
        {
            if (!_dashboardConfig.IsEnabled) return;

            var now = DateTime.Now;
            var currentTime = now.TimeOfDay;
            
            // Verificar si es la hora correcta para generar reporte (±15 minutos de tolerancia)
            var targetTime = _dashboardConfig.ReportTime;
            var timeDifference = Math.Abs((currentTime - targetTime).TotalMinutes);
            
            if (timeDifference > 15) return; // No es el momento
            
            // Verificar si ya generamos el reporte hoy
            var todayReportMarker = Path.Combine(_dashboardReportsFolder, "Markers", $"daily_report_{now:yyyyMMdd}.marker");
            if (File.Exists(todayReportMarker)) return; // Ya generado hoy
            
            // Verificar días activos si está configurado
            if (_dashboardConfig.UseWeekdayFilter && 
                !_dashboardConfig.ActiveWeekDays.Contains(now.DayOfWeek)) return;
            
            // Generar reporte para ayer
            var reportDate = now.Date.AddDays(-1);
            var generatedReports = await GenerateDailyReportAsync(reportDate);
            
            // Marcar como generado para evitar duplicados
            Directory.CreateDirectory(Path.GetDirectoryName(todayReportMarker)!);
            await File.WriteAllTextAsync(todayReportMarker, $"Generated at: {now:yyyy-MM-dd HH:mm:ss}");
            
            // Disparar evento
            DailyReportGenerated?.Invoke(this, new DailyReportGeneratedEventArgs 
            { 
                ReportDate = reportDate,
                GeneratedFiles = generatedReports,
                QuadrantCount = generatedReports.Count
            });
            
            // Enviar email si está habilitado
            if (_dashboardConfig.EmailConfig.IsEnabled && _dashboardConfig.EmailConfig.Recipients.Any())
            {
                _ = Task.Run(async () => await SendDailyReportEmailAsync(reportDate, generatedReports));
            }
            
            Console.WriteLine($"[ActivityDashboardScheduler] Reporte diario generado para {reportDate:yyyy-MM-dd}: {generatedReports.Count} cuadrantes");
            
            // ★ NUEVO: Limpieza automática después de generar reporte diario
            _ = Task.Run(() => 
            {
                try
                {
                    var deletedCount = CleanupOldDashboardReports();
                    if (deletedCount > 0)
                    {
                        Console.WriteLine($"[ActivityDashboardScheduler] Limpieza post-reporte: {deletedCount} archivos antiguos eliminados");
                    }
                }
                catch (Exception cleanupEx)
                {
                    Console.WriteLine($"[ActivityDashboardScheduler] Error en limpieza post-reporte: {cleanupEx.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ActivityDashboardScheduler] Error en verificación de reporte diario: {ex.Message}");
        }
    }

    /// <summary>
    /// Genera el reporte diario completo por cuadrantes
    /// </summary>
    private async Task<List<string>> GenerateDailyReportAsync(DateTime reportDate)
    {
        var generatedReports = new List<string>();
        
        try
        {
            // Crear reporte base con datos del período especificado
            var startDateTime = reportDate.Add(_dashboardConfig.StartTime);
            var endDateTime = reportDate.Add(_dashboardConfig.EndTime);
            
            // Si EndTime es menor que StartTime, asumir que cruza medianoche
            if (_dashboardConfig.EndTime < _dashboardConfig.StartTime)
            {
                endDateTime = endDateTime.AddDays(1);
            }
            
            var baseReport = new ActivityReport
            {
                ReportStartTime = startDateTime,
                ReportEndTime = endDateTime,
                QuadrantConfigurationName = _dashboardConfig.QuadrantConfigName,
                ReportType = "DailyDashboard",
                Comments = $"Reporte diario automático del Dashboard de Actividad para {GetDayNameInSpanish(reportDate.DayOfWeek)} {reportDate:dd/MM/yyyy}"
            };

            // Obtener estadísticas actuales de todos los cuadrantes
            var allStats = _activityService.GetAllActivityStats();
            
            if (!allStats.Any())
            {
                Console.WriteLine($"[ActivityDashboardScheduler] No hay estadísticas de cuadrantes para el reporte de {reportDate:yyyy-MM-dd}");
                return generatedReports;
            }

            // Generar reporte consolidado
            var consolidatedReport = await GenerateConsolidatedReport(baseReport, allStats, reportDate);
            if (consolidatedReport != null)
            {
                var consolidatedPath = await _reportService.ExportReportAsync(consolidatedReport, "HTML");
                generatedReports.Add(consolidatedPath);
                
                // Mover a carpeta específica de reportes diarios
                var dailyReportsFolder = Path.Combine(_dashboardReportsFolder, "Daily");
                Directory.CreateDirectory(dailyReportsFolder);
                
                var finalPath = Path.Combine(dailyReportsFolder, 
                    $"{GetDayNameInSpanish(reportDate.DayOfWeek)}_{reportDate:yyyy-MM-dd}_Consolidado.html");
                
                if (File.Exists(consolidatedPath))
                {
                    File.Move(consolidatedPath, finalPath, true);
                    generatedReports[generatedReports.Count - 1] = finalPath;
                }
            }

            // Generar reportes individuales por cuadrante si está habilitado
            if (_dashboardConfig.GeneratePerQuadrantReports)
            {
                foreach (var (quadrantName, stats) in allStats)
                {
                    var quadrantReport = await GenerateQuadrantReport(baseReport, quadrantName, stats, reportDate);
                    if (quadrantReport != null)
                    {
                        var quadrantPath = await _reportService.ExportReportAsync(quadrantReport, "HTML");
                        
                        // Organizar en carpetas por cuadrante
                        var quadrantFolder = Path.Combine(_dashboardReportsFolder, "ByQuadrant", SanitizeFileName(quadrantName));
                        Directory.CreateDirectory(quadrantFolder);
                        
                        var finalPath = Path.Combine(quadrantFolder, 
                            $"{GetDayNameInSpanish(reportDate.DayOfWeek)}_{reportDate:yyyy-MM-dd}_{SanitizeFileName(quadrantName)}.html");
                        
                        if (File.Exists(quadrantPath))
                        {
                            File.Move(quadrantPath, finalPath, true);
                            generatedReports.Add(finalPath);
                        }
                    }
                }
            }

            Console.WriteLine($"[ActivityDashboardScheduler] Generados {generatedReports.Count} reportes para {reportDate:yyyy-MM-dd}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ActivityDashboardScheduler] Error generando reporte diario: {ex.Message}");
        }

        return generatedReports;
    }

    /// <summary>
    /// Genera reporte consolidado con todos los cuadrantes
    /// </summary>
    private async Task<ActivityReport> GenerateConsolidatedReport(
        ActivityReport baseReport, 
        Dictionary<string, QuadrantActivityStats> allStats,
        DateTime reportDate)
    {
        var report = new ActivityReport
        {
            Id = baseReport.Id,
            ReportStartTime = baseReport.ReportStartTime,
            ReportEndTime = baseReport.ReportEndTime,
            QuadrantConfigurationName = baseReport.QuadrantConfigurationName,
            ReportType = baseReport.ReportType,
            Comments = $"📊 {GetDayNameInSpanish(reportDate.DayOfWeek)} {reportDate:dd/MM/yyyy} - Reporte Consolidado de Actividad\n\n" +
                      $"Período: {_dashboardConfig.StartTime:HH\\:mm} - {_dashboardConfig.EndTime:HH\\:mm}\n" +
                      $"Cuadrantes monitoreados: {allStats.Count}\n" +
                      $"Configuración: {_dashboardConfig.QuadrantConfigName}\n\n" +
                      "Este reporte consolida la actividad de todos los cuadrantes monitoreados durante el período especificado.",
            SessionName = $"Consolidado_{GetDayNameInSpanish(reportDate.DayOfWeek)}_{reportDate:yyyyMMdd}",
            SessionStartTime = baseReport.ReportStartTime
        };

        // Convertir estadísticas a entradas de reporte
        foreach (var (quadrantName, stats) in allStats)
        {
            var entry = CreateReportEntryFromStats(quadrantName, stats, baseReport.ReportStartTime, baseReport.ReportEndTime);
            report.Entries.Add(entry);
        }

        // Generar resumen
        report.GenerateSummary();
        
        return report;
    }

    /// <summary>
    /// Genera reporte individual para un cuadrante específico
    /// </summary>
    private async Task<ActivityReport> GenerateQuadrantReport(
        ActivityReport baseReport,
        string quadrantName,
        QuadrantActivityStats stats,
        DateTime reportDate)
    {
        var report = new ActivityReport
        {
            Id = $"{baseReport.Id}_{SanitizeFileName(quadrantName)}",
            ReportStartTime = baseReport.ReportStartTime,
            ReportEndTime = baseReport.ReportEndTime,
            QuadrantConfigurationName = baseReport.QuadrantConfigurationName,
            ReportType = $"{baseReport.ReportType}_Individual",
            Comments = $"🎯 {GetDayNameInSpanish(reportDate.DayOfWeek)} {reportDate:dd/MM/yyyy} - Cuadrante: {quadrantName}\n\n" +
                      $"Período: {_dashboardConfig.StartTime:HH\\:mm} - {_dashboardConfig.EndTime:HH\\:mm}\n" +
                      $"Configuración: {_dashboardConfig.QuadrantConfigName}\n\n" +
                      $"Este reporte detalla la actividad específica del cuadrante '{quadrantName}' durante el período especificado.",
            SessionName = $"{SanitizeFileName(quadrantName)}_{GetDayNameInSpanish(reportDate.DayOfWeek)}_{reportDate:yyyyMMdd}",
            SessionStartTime = baseReport.ReportStartTime
        };

        // Crear entrada única para este cuadrante
        var entry = CreateReportEntryFromStats(quadrantName, stats, baseReport.ReportStartTime, baseReport.ReportEndTime);
        report.Entries.Add(entry);

        // Generar resumen
        report.GenerateSummary();
        
        return report;
    }

    /// <summary>
    /// Convierte estadísticas de actividad a entrada de reporte
    /// </summary>
    private ActivityReportEntry CreateReportEntryFromStats(
        string quadrantName, 
        QuadrantActivityStats stats,
        DateTime reportStart,
        DateTime reportEnd)
    {
        return new ActivityReportEntry
        {
            QuadrantName = quadrantName,
            TotalComparisons = stats.TotalComparisons,
            ActivityDetectionCount = stats.ActivityDetectionCount,
            ActivityRate = stats.ActivityRate,
            AverageChangePercentage = stats.AverageChangePercentage,
            FirstActivityTime = stats.SessionStartTime > reportStart ? stats.SessionStartTime : reportStart,
            LastActivityTime = stats.LastActivityTime < reportEnd ? stats.LastActivityTime : reportEnd,
            ActiveDuration = stats.SessionDuration
        };
    }

    /// <summary>
    /// Obtiene nombre del día en español
    /// </summary>
    private string GetDayNameInSpanish(DayOfWeek dayOfWeek)
    {
        var culture = new CultureInfo("es-ES");
        return culture.DateTimeFormat.GetDayName(dayOfWeek);
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
    /// Inicializa la carpeta de reportes del dashboard
    /// </summary>
    private void InitializeDashboardReportsFolder()
    {
        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        _dashboardReportsFolder = Path.Combine(documentsPath, "Capturer", "ActivityDashboard", "Reports");
        
        try
        {
            Directory.CreateDirectory(_dashboardReportsFolder);
            Directory.CreateDirectory(Path.Combine(_dashboardReportsFolder, "Daily"));
            Directory.CreateDirectory(Path.Combine(_dashboardReportsFolder, "ByQuadrant"));
            Directory.CreateDirectory(Path.Combine(_dashboardReportsFolder, "Markers"));
            
            Console.WriteLine($"[ActivityDashboardScheduler] Carpeta de reportes inicializada: {_dashboardReportsFolder}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ActivityDashboardScheduler] Error creando carpetas: {ex.Message}");
            _dashboardReportsFolder = Path.GetTempPath(); // Fallback
        }
    }

    #region ★ NUEVO: Sistema de Limpieza Dashboard - Retención 2 Días
    
    /// <summary>
    /// Realiza limpieza inicial al iniciar el servicio del dashboard
    /// </summary>
    private void PerformInitialDashboardCleanup()
    {
        try
        {
            Console.WriteLine("[ActivityDashboardScheduler] Ejecutando limpieza inicial de reportes antiguos...");
            var deletedCount = CleanupOldDashboardReports();
            Console.WriteLine($"[ActivityDashboardScheduler] Limpieza inicial completada. Archivos eliminados: {deletedCount}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ActivityDashboardScheduler] Error en limpieza inicial: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Limpieza periódica del dashboard llamada por timer
    /// </summary>
    private void PerformPeriodicDashboardCleanup(object? state)
    {
        try
        {
            var deletedCount = CleanupOldDashboardReports();
            if (deletedCount > 0)
            {
                Console.WriteLine($"[ActivityDashboardScheduler] Limpieza periódica completada. Archivos eliminados: {deletedCount}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ActivityDashboardScheduler] Error en limpieza periódica: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Limpia archivos antiguos del dashboard, manteniendo solo los últimos 2 días
    /// RETENCIÓN: Solo archivos de ayer y hoy - elimina todo lo más antiguo
    /// </summary>
    /// <returns>Número de archivos eliminados</returns>
    private int CleanupOldDashboardReports()
    {
        var deletedCount = 0;
        var cutoffDate = DateTime.Now.Date.AddDays(-2); // Todo lo anterior a anteayer
        
        Console.WriteLine($"[ActivityDashboardScheduler] Limpiando reportes anteriores a {cutoffDate:yyyy-MM-dd}");
        
        try
        {
            // 1. Limpiar carpeta Daily
            var dailyFolder = Path.Combine(_dashboardReportsFolder, "Daily");
            if (Directory.Exists(dailyFolder))
            {
                deletedCount += CleanupDashboardFolderByDate(dailyFolder, cutoffDate, "*.html");
            }
            
            // 2. Limpiar carpeta ByQuadrant (incluyendo subcarpetas)
            var quadrantFolder = Path.Combine(_dashboardReportsFolder, "ByQuadrant");
            if (Directory.Exists(quadrantFolder))
            {
                deletedCount += CleanupQuadrantFolders(quadrantFolder, cutoffDate);
            }
            
            // 3. Limpiar Markers antiguos
            var markersFolder = Path.Combine(_dashboardReportsFolder, "Markers");
            if (Directory.Exists(markersFolder))
            {
                deletedCount += CleanupDashboardFolderByDate(markersFolder, cutoffDate, "*.marker");
            }
            
            // 4. Limpiar archivos JSON de configuración antiguos si existen
            deletedCount += CleanupDashboardFolderByDate(_dashboardReportsFolder, cutoffDate, "*.json");
            
            return deletedCount;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ActivityDashboardScheduler] Error en limpieza: {ex.Message}");
            return deletedCount;
        }
    }
    
    /// <summary>
    /// Limpia archivos en una carpeta del dashboard según fecha límite
    /// </summary>
    private int CleanupDashboardFolderByDate(string folderPath, DateTime cutoffDate, string searchPattern)
    {
        var deletedCount = 0;
        
        try
        {
            if (!Directory.Exists(folderPath)) return 0;
            
            var files = Directory.GetFiles(folderPath, searchPattern, SearchOption.TopDirectoryOnly);
            
            foreach (var file in files)
            {
                try
                {
                    var fileInfo = new FileInfo(file);
                    
                    // Para archivos del dashboard, usar fecha de creación o la más reciente
                    var fileDate = fileInfo.CreationTime > fileInfo.LastWriteTime 
                        ? fileInfo.CreationTime.Date 
                        : fileInfo.LastWriteTime.Date;
                    
                    // Alternativamente, intentar extraer fecha del nombre del archivo
                    var dateFromName = ExtractDateFromDashboardFileName(Path.GetFileName(file));
                    if (dateFromName.HasValue)
                    {
                        fileDate = dateFromName.Value.Date;
                    }
                    
                    if (fileDate < cutoffDate)
                    {
                        File.Delete(file);
                        deletedCount++;
                        Console.WriteLine($"[ActivityDashboardScheduler] Eliminado: {Path.GetFileName(file)} (fecha: {fileDate:yyyy-MM-dd})");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ActivityDashboardScheduler] Error eliminando {file}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ActivityDashboardScheduler] Error accediendo carpeta {folderPath}: {ex.Message}");
        }
        
        return deletedCount;
    }
    
    /// <summary>
    /// Limpia subcarpetas de cuadrantes recursivamente
    /// </summary>
    private int CleanupQuadrantFolders(string quadrantBaseFolder, DateTime cutoffDate)
    {
        var deletedCount = 0;
        
        try
        {
            if (!Directory.Exists(quadrantBaseFolder)) return 0;
            
            var subfolders = Directory.GetDirectories(quadrantBaseFolder);
            
            foreach (var subfolder in subfolders)
            {
                // Limpiar archivos HTML en cada subcarpeta de cuadrante
                deletedCount += CleanupDashboardFolderByDate(subfolder, cutoffDate, "*.html");
                deletedCount += CleanupDashboardFolderByDate(subfolder, cutoffDate, "*.json");
                
                // Si la subcarpeta está vacía después de la limpieza, eliminarla
                try
                {
                    if (!Directory.EnumerateFileSystemEntries(subfolder).Any())
                    {
                        Directory.Delete(subfolder);
                        Console.WriteLine($"[ActivityDashboardScheduler] Subcarpeta vacía eliminada: {Path.GetFileName(subfolder)}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ActivityDashboardScheduler] Error eliminando subcarpeta vacía {subfolder}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ActivityDashboardScheduler] Error limpiando subcarpetas de cuadrantes: {ex.Message}");
        }
        
        return deletedCount;
    }
    
    /// <summary>
    /// Extrae fecha del nombre de archivo del dashboard si es posible
    /// </summary>
    private DateTime? ExtractDateFromDashboardFileName(string fileName)
    {
        try
        {
            // Buscar patrones de fecha en nombres de archivos del dashboard
            // Ejemplos: "Lunes_2024-09-11_Consolidado.html", "daily_report_20240911.marker"
            
            var datePatterns = new[]
            {
                @"(\d{4}-\d{2}-\d{2})",     // yyyy-MM-dd
                @"(\d{8})",                 // yyyyMMdd
            };
            
            foreach (var pattern in datePatterns)
            {
                var match = System.Text.RegularExpressions.Regex.Match(fileName, pattern);
                if (match.Success)
                {
                    var dateString = match.Groups[1].Value;
                    
                    // Intentar parsear según el formato
                    if (dateString.Length == 8 && DateTime.TryParseExact(dateString, "yyyyMMdd", null, DateTimeStyles.None, out var date1))
                    {
                        return date1;
                    }
                    else if (DateTime.TryParseExact(dateString, "yyyy-MM-dd", null, DateTimeStyles.None, out var date2))
                    {
                        return date2;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ActivityDashboardScheduler] Error extrayendo fecha del archivo {fileName}: {ex.Message}");
        }
        
        return null;
    }
    
    #endregion

    /// <summary>
    /// Carga configuración del dashboard desde archivo
    /// </summary>
    private void LoadDashboardConfiguration()
    {
        try
        {
            var configPath = Path.Combine(_dashboardReportsFolder, "dashboard_schedule_config.json");
            if (File.Exists(configPath))
            {
                var jsonContent = File.ReadAllText(configPath);
                var config = System.Text.Json.JsonSerializer.Deserialize<ActivityDashboardScheduleConfig>(jsonContent);
                if (config != null)
                {
                    _dashboardConfig = config;
                    Console.WriteLine($"[ActivityDashboardScheduler] Configuración cargada desde {configPath}");
                }
            }
            else
            {
                // Configuración por defecto basada en configuración de email existente
                _dashboardConfig = new ActivityDashboardScheduleConfig
                {
                    IsEnabled = _config.Schedule.EnableAutomaticReports,
                    ReportTime = TimeSpan.FromHours(23), // 11 PM por defecto
                    Frequency = DashboardReportFrequency.Daily,
                    CustomDays = 1,
                    StartTime = _config.Schedule.StartTime,
                    EndTime = _config.Schedule.EndTime,
                    UseWeekdayFilter = !_config.Schedule.IncludeWeekends,
                    ActiveWeekDays = _config.Schedule.ActiveWeekDays.ToList(),
                    QuadrantConfigName = _config.QuadrantSystem.ActiveConfigurationName,
                    GeneratePerQuadrantReports = true,
                    EmailConfig = new DashboardEmailConfig
                    {
                        IsEnabled = false,
                        Recipients = new List<string>(),
                        UseZipFormat = true,
                        SendSeparateEmails = false,
                        SelectedReportTypes = new List<string> { "Consolidado" }
                    }
                };
                SaveDashboardConfiguration();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ActivityDashboardScheduler] Error cargando configuración: {ex.Message}");
        }
    }

    /// <summary>
    /// Guarda configuración del dashboard
    /// </summary>
    private void SaveDashboardConfiguration()
    {
        try
        {
            var configPath = Path.Combine(_dashboardReportsFolder, "dashboard_schedule_config.json");
            var jsonContent = System.Text.Json.JsonSerializer.Serialize(_dashboardConfig, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            File.WriteAllText(configPath, jsonContent);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ActivityDashboardScheduler] Error guardando configuración: {ex.Message}");
        }
    }

    /// <summary>
    /// Obtiene reportes generados en un período
    /// </summary>
    public List<string> GetGeneratedReports(DateTime startDate, DateTime endDate)
    {
        var reports = new List<string>();
        
        try
        {
            // Buscar en carpeta Daily
            var dailyFolder = Path.Combine(_dashboardReportsFolder, "Daily");
            if (Directory.Exists(dailyFolder))
            {
                reports.AddRange(Directory.GetFiles(dailyFolder, "*.html", SearchOption.TopDirectoryOnly));
            }
            
            // Buscar en carpetas de cuadrantes
            var quadrantFolder = Path.Combine(_dashboardReportsFolder, "ByQuadrant");
            if (Directory.Exists(quadrantFolder))
            {
                reports.AddRange(Directory.GetFiles(quadrantFolder, "*.html", SearchOption.AllDirectories));
            }
            
            // Filtrar por fechas si es necesario
            return reports
                .Where(f => 
                {
                    var fileInfo = new FileInfo(f);
                    return fileInfo.CreationTime.Date >= startDate.Date && 
                           fileInfo.CreationTime.Date <= endDate.Date;
                })
                .OrderByDescending(f => new FileInfo(f).CreationTime)
                .ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ActivityDashboardScheduler] Error obteniendo reportes: {ex.Message}");
            return reports;
        }
    }

    /// <summary>
    /// Envía email con reporte diario
    /// </summary>
    private async Task<bool> SendDailyReportEmailAsync(DateTime reportDate, List<string> reportFiles, bool isTest = false)
    {
        try
        {
            var dayName = GetDayNameInSpanish(reportDate.DayOfWeek);
            var subject = isTest 
                ? $"[TEST] {dayName} {reportDate:dd/MM/yyyy} - Reporte de Actividad Dashboard"
                : $"{dayName} {reportDate:dd/MM/yyyy} - Reporte de Actividad Dashboard";
            
            var body = GenerateEmailBody(reportDate, reportFiles.Count, isTest);
            
            // Filtrar archivos según configuración
            var filesToSend = FilterReportsByConfiguration(reportFiles);
            
            if (!filesToSend.Any())
            {
                Console.WriteLine("[ActivityDashboardScheduler] No hay archivos para enviar según configuración");
                return false;
            }
            
            bool success;
            
            if (_dashboardConfig.EmailConfig.SendSeparateEmails)
            {
                success = await SendSeparateEmailsAsync(subject, body, filesToSend);
            }
            else
            {
                success = await SendCombinedEmailAsync(subject, body, filesToSend);
            }
            
            // Disparar evento de email enviado
            if (success)
            {
                DailyReportEmailSent?.Invoke(this, new DailyReportEmailSentEventArgs
                {
                    ReportDate = reportDate,
                    Recipients = _dashboardConfig.EmailConfig.Recipients,
                    FilesCount = filesToSend.Count,
                    IsTest = isTest
                });
            }
            
            return success;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ActivityDashboardScheduler] Error enviando email: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Envía email con reportes personalizados seleccionados
    /// </summary>
    private async Task<bool> SendCustomReportEmailAsync(List<string> selectedReports)
    {
        try
        {
            var subject = $"Reportes Dashboard Seleccionados - {DateTime.Now:dd/MM/yyyy HH:mm}";
            var body = GenerateCustomEmailBody(selectedReports);
            
            return await SendCombinedEmailAsync(subject, body, selectedReports);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ActivityDashboardScheduler] Error enviando email personalizado: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Envía emails separados por cada archivo
    /// </summary>
    private async Task<bool> SendSeparateEmailsAsync(string baseSubject, string baseBody, List<string> files)
    {
        bool allSuccess = true;
        
        foreach (var file in files)
        {
            try
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                var individualSubject = $"{baseSubject} - {fileName}";
                var individualBody = $"{baseBody}\n\nArchivo adjunto: {fileName}";
                
                var success = await _emailService.SendActivityDashboardReportAsync(
                    _dashboardConfig.EmailConfig.Recipients,
                    individualSubject,
                    individualBody,
                    new List<string> { file },
                    _dashboardConfig.EmailConfig.UseZipFormat
                );
                
                if (!success)
                {
                    allSuccess = false;
                    Console.WriteLine($"[ActivityDashboardScheduler] Error enviando email para archivo: {fileName}");
                }
            }
            catch (Exception ex)
            {
                allSuccess = false;
                Console.WriteLine($"[ActivityDashboardScheduler] Error enviando email individual: {ex.Message}");
            }
        }
        
        return allSuccess;
    }

    /// <summary>
    /// Envía un email combinado con todos los archivos
    /// </summary>
    private async Task<bool> SendCombinedEmailAsync(string subject, string body, List<string> files)
    {
        try
        {
            return await _emailService.SendActivityDashboardReportAsync(
                _dashboardConfig.EmailConfig.Recipients,
                subject,
                body,
                files,
                _dashboardConfig.EmailConfig.UseZipFormat
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ActivityDashboardScheduler] Error enviando email combinado: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Filtra reportes según la configuración
    /// </summary>
    private List<string> FilterReportsByConfiguration(List<string> allReports)
    {
        var filtered = new List<string>();
        
        foreach (var report in allReports)
        {
            var fileName = Path.GetFileNameWithoutExtension(report);
            
            // Verificar tipos seleccionados
            bool shouldInclude = false;
            
            if (_dashboardConfig.EmailConfig.SelectedReportTypes.Contains("Consolidado") && 
                fileName.Contains("Consolidado"))
            {
                shouldInclude = true;
            }
            
            if (_dashboardConfig.EmailConfig.SelectedReportTypes.Contains("Individual") && 
                !fileName.Contains("Consolidado"))
            {
                shouldInclude = true;
            }
            
            if (shouldInclude)
            {
                filtered.Add(report);
            }
        }
        
        return filtered;
    }

    /// <summary>
    /// Genera cuerpo del email para reporte diario
    /// </summary>
    private string GenerateEmailBody(DateTime reportDate, int fileCount, bool isTest = false)
    {
        var dayName = GetDayNameInSpanish(reportDate.DayOfWeek);
        var testPrefix = isTest ? "[PRUEBA] " : "";
        
        return $@"{testPrefix}Reporte Automático de Actividad Dashboard

📅 Fecha: {dayName} {reportDate:dd/MM/yyyy}
⏰ Período: {_dashboardConfig.StartTime:HH\:mm} - {_dashboardConfig.EndTime:HH\:mm}
📊 Cuadrantes: {_dashboardConfig.QuadrantConfigName}
📁 Archivos generados: {fileCount}

Este es un reporte automático generado por el sistema Capturer - Activity Dashboard.
Los archivos adjuntos contienen el análisis detallado de actividad por cuadrantes.

---
🤖 Generado automáticamente por Capturer v3.2.0
📧 Configurado desde Activity Dashboard - Reportes Diarios";
    }

    /// <summary>
    /// Genera cuerpo del email para reportes personalizados
    /// </summary>
    private string GenerateCustomEmailBody(List<string> files)
    {
        var fileNames = files.Select(f => Path.GetFileNameWithoutExtension(f)).ToList();
        
        return $@"Reportes Dashboard Seleccionados

📅 Solicitud: {DateTime.Now:dd/MM/yyyy HH:mm}
📁 Archivos incluidos: {files.Count}

Archivos adjuntos:
{string.Join("\n", fileNames.Select(name => $"• {name}"))}

Estos reportes fueron seleccionados manualmente desde Activity Dashboard.

---
🤖 Generado por Capturer v3.2.0
📧 Enviado desde Activity Dashboard";
    }

    /// <summary>
    /// Extrae nombre del cuadrante del nombre de archivo
    /// </summary>
    private string ExtractQuadrantNameFromFileName(string fileName)
    {
        try
        {
            if (fileName.Contains("Consolidado"))
                return "Consolidado";
                
            var parts = fileName.Split('_');
            if (parts.Length >= 3)
            {
                return parts[2]; // Formato: DayName_Date_QuadrantName
            }
        }
        catch
        {
            // Silently fail and return default
        }
        
        return "Desconocido";
    }

    public void Dispose()
    {
        try
        {
            _dailyReportTimer?.Dispose();
            _cleanupTimer?.Dispose(); // ★ NUEVO: Limpiar timer de cleanup
            Console.WriteLine("[ActivityDashboardScheduler] Servicio finalizado");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ActivityDashboardScheduler] Error en dispose: {ex.Message}");
        }
    }
}

/// <summary>
/// Configuración específica para reportes programados del dashboard
/// </summary>
public class ActivityDashboardScheduleConfig
{
    public bool IsEnabled { get; set; } = false;
    public TimeSpan ReportTime { get; set; } = TimeSpan.FromHours(23); // 11:00 PM por defecto
    public DashboardReportFrequency Frequency { get; set; } = DashboardReportFrequency.Daily;
    public int CustomDays { get; set; } = 1;
    public TimeSpan StartTime { get; set; } = TimeSpan.FromHours(8);   // 8:00 AM
    public TimeSpan EndTime { get; set; } = TimeSpan.FromHours(18);    // 6:00 PM
    public bool UseWeekdayFilter { get; set; } = true;
    public List<DayOfWeek> ActiveWeekDays { get; set; } = new()
    {
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, 
        DayOfWeek.Thursday, DayOfWeek.Friday
    };
    public string QuadrantConfigName { get; set; } = "Default";
    public bool GeneratePerQuadrantReports { get; set; } = true;
    public DashboardEmailConfig EmailConfig { get; set; } = new();
    public string ReportSubjectTemplate { get; set; } = "{DayName} {Date:dd/MM/yyyy} - Reporte de Actividad";
}

/// <summary>
/// Configuración de email específica para dashboard
/// </summary>
public class DashboardEmailConfig
{
    public bool IsEnabled { get; set; } = false;
    public List<string> Recipients { get; set; } = new();
    public bool UseZipFormat { get; set; } = true;
    public bool SendSeparateEmails { get; set; } = false;
    public List<string> SelectedReportTypes { get; set; } = new() { "Consolidado" }; // "Consolidado", "Individual"
}

/// <summary>
/// Frecuencia de reportes del dashboard
/// </summary>
public enum DashboardReportFrequency
{
    Daily = 1,
    Weekly = 7,
    Custom = 0
}

/// <summary>
/// Información de reporte de dashboard
/// </summary>
public class DashboardReportInfo
{
    public string FilePath { get; set; } = "";
    public string FileName { get; set; } = "";
    public DateTime CreationDate { get; set; }
    public long Size { get; set; }
    public string ReportType { get; set; } = "";
    public string QuadrantName { get; set; } = "";
}

/// <summary>
/// Argumentos del evento de reporte diario generado
/// </summary>
public class DailyReportGeneratedEventArgs : EventArgs
{
    public DateTime ReportDate { get; set; }
    public List<string> GeneratedFiles { get; set; } = new();
    public int QuadrantCount { get; set; }
}

/// <summary>
/// Argumentos del evento de email de reporte enviado
/// </summary>
public class DailyReportEmailSentEventArgs : EventArgs
{
    public DateTime ReportDate { get; set; }
    public List<string> Recipients { get; set; } = new();
    public int FilesCount { get; set; }
    public bool IsTest { get; set; }
}