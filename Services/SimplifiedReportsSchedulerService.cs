using System.IO.Compression;
using System.Text.Json;
using Capturer.Models;
using Capturer.Services;
using Capturer.Forms;

namespace Capturer.Services;

/// <summary>
/// Servicio SIMPLIFICADO para reportes automáticos del ActivityDashboard
/// Elimina complejidad innecesaria y se enfoca en funcionalidad core
/// </summary>
public class SimplifiedReportsSchedulerService : IDisposable
{
    private readonly ActivityReportService _reportService;
    private readonly IEmailService _emailService;
    private readonly System.Threading.Timer _checkTimer;
    private readonly object _lockObject = new object();
    
    // Simple configuration
    private SimplifiedReportsConfig _config = new();
    private string _configFilePath = "";

    public event EventHandler<ReportEmailSentEventArgs>? EmailSent;

    public SimplifiedReportsSchedulerService(
        ActivityReportService reportService,
        IEmailService emailService)
    {
        _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        
        InitializeConfigPath();
        LoadConfiguration();
        
        // Simple timer: check every 30 minutes if it's time to send
        _checkTimer = new System.Threading.Timer(
            CheckAndSendReports, 
            null, 
            TimeSpan.FromMinutes(1), // First check in 1 minute
            TimeSpan.FromMinutes(30) // Check every 30 minutes
        );
        
        Console.WriteLine($"[SimplifiedReportsScheduler] Servicio iniciado. Config: {(_config.IsDaily ? "Diario" : "Semanal")} a las {_config.EmailTime:hh\\:mm}");
    }

    public async Task<SimplifiedReportsConfig> ShowConfigurationDialogAsync(CapturerConfiguration? capturerConfig = null)
    {
        // Use provided config or create basic one
        var configToUse = capturerConfig ?? new CapturerConfiguration();
        
        using var configForm = new SimplifiedReportsConfigForm(configToUse, _config, _emailService);
        
        if (configForm.ShowDialog() == DialogResult.OK)
        {
            _config = configForm.ReportsConfig;
            _config.IsEnabled = true; // Enable when user saves configuration
            SaveConfiguration();
            Console.WriteLine("[SimplifiedReportsScheduler] Configuración actualizada y activada");
            return _config;
        }
        
        return _config;
    }

    private void CheckAndSendReports(object? state)
    {
        if (!_config.IsEnabled) return;

        lock (_lockObject)
        {
            try
            {
                var now = DateTime.Now;
                var targetTime = DateTime.Today.Add(_config.EmailTime);
                
                // Check if it's time to send (within 30-minute window)
                if (Math.Abs((now - targetTime).TotalMinutes) <= 30)
                {
                    // Check if we should send today based on configuration
                    if (_config.ShouldSendToday(now))
                    {
                        // Avoid duplicate sends on same day
                        if (_config.LastEmailSent.Date == DateTime.Today)
                        {
                            Console.WriteLine("[SimplifiedReportsScheduler] Email ya enviado hoy, omitiendo");
                            return;
                        }

                        Console.WriteLine($"[SimplifiedReportsScheduler] Hora de enviar reporte: {now:HH:mm}");
                        
                        // Send async without blocking the timer
                        _ = Task.Run(async () => await ProcessScheduledReportAsync(now));
                    }
                    else
                    {
                        Console.WriteLine($"[SimplifiedReportsScheduler] Hoy {now.DayOfWeek} no está programado para envío");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SimplifiedReportsScheduler] Error en verificación: {ex.Message}");
            }
        }
    }

    private async Task ProcessScheduledReportAsync(DateTime sendTime)
    {
        try
        {
            if (_config.IsDaily)
            {
                await SendDailyHtmlReportAsync(sendTime);
            }
            else
            {
                await SendWeeklyZipReportAsync(sendTime);
            }
            
            // Update last sent time
            _config.LastEmailSent = DateTime.Now;
            SaveConfiguration();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SimplifiedReportsScheduler] Error procesando reporte: {ex.Message}");
            EmailSent?.Invoke(this, new ReportEmailSentEventArgs { Success = false, ErrorMessage = ex.Message });
        }
    }

    /// <summary>
    /// Envía reporte HTML del día actual
    /// </summary>
    private async Task SendDailyHtmlReportAsync(DateTime sendTime)
    {
        try
        {
            Console.WriteLine("[SimplifiedReportsScheduler] Generando reporte HTML diario...");
            
            // Generate report for today only
            var startTime = DateTime.Today.AddHours(8); // 8 AM 
            var endTime = DateTime.Today.AddHours(20);  // 8 PM
            
            // System records 24/7, no filtering needed
            Console.WriteLine($"[SimplifiedReportsScheduler] Generando reporte para {sendTime:dddd, dd/MM/yyyy}");

            var report = await _reportService.GenerateReportAsync(startTime, endTime, "Daily-Automated");
            
            if (report.Entries.Any())
            {
                // Export to HTML
                var htmlPath = await _reportService.ExportReportAsync(report, "HTML");
                Console.WriteLine($"[SimplifiedReportsScheduler] HTML generado: {htmlPath}");

                // Prepare email
                var subject = $"📊 Reporte Diario - {sendTime:dd/MM/yyyy}";
                var body = GenerateSimpleEmailBody(report, "daily");
                var attachmentFiles = new List<string> { htmlPath };

                // Send using EmailService
                var success = await _emailService.SendActivityDashboardReportAsync(
                    _config.SelectedRecipients,
                    subject,
                    body,
                    attachmentFiles,
                    false // No ZIP for daily, just attach HTML directly
                );

                Console.WriteLine($"[SimplifiedReportsScheduler] Email diario {(success ? "enviado exitosamente" : "falló")}");
                EmailSent?.Invoke(this, new ReportEmailSentEventArgs 
                { 
                    Success = success, 
                    ReportType = "Daily",
                    Recipients = _config.SelectedRecipients.Count,
                    AttachmentCount = 1
                });

                // Cleanup
                if (File.Exists(htmlPath))
                {
                    File.Delete(htmlPath);
                    Console.WriteLine("[SimplifiedReportsScheduler] Archivo temporal limpiado");
                }
            }
            else
            {
                Console.WriteLine("[SimplifiedReportsScheduler] Sin datos para reporte diario");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SimplifiedReportsScheduler] Error en reporte diario: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Envía ZIP con reportes HTML de toda la semana
    /// </summary>
    private async Task SendWeeklyZipReportAsync(DateTime sendTime)
    {
        try
        {
            Console.WriteLine("[SimplifiedReportsScheduler] Generando reportes semanales...");
            
            var htmlFiles = new List<string>();
            var startOfWeek = GetStartOfWeek(sendTime);
            
            // Generate HTML for each day of the week
            for (int i = 0; i < 7; i++)
            {
                var dayDate = startOfWeek.AddDays(i);
                
                // System records all days 24/7, generate report for each day
                Console.WriteLine($"[SimplifiedReportsScheduler] Procesando {dayDate:dd/MM} ({dayDate.DayOfWeek})");

                var dayStart = dayDate.AddHours(8);
                var dayEnd = dayDate.AddHours(20);
                
                try
                {
                    var dailyReport = await _reportService.GenerateReportAsync(dayStart, dayEnd, $"Weekly-{dayDate:dd-MM}");
                    
                    if (dailyReport.Entries.Any())
                    {
                        var htmlPath = await _reportService.ExportReportAsync(dailyReport, "HTML");
                        htmlFiles.Add(htmlPath);
                        Console.WriteLine($"[SimplifiedReportsScheduler] HTML generado para {dayDate:dd/MM}: {Path.GetFileName(htmlPath)}");
                    }
                    else
                    {
                        Console.WriteLine($"[SimplifiedReportsScheduler] Sin datos para {dayDate:dd/MM}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SimplifiedReportsScheduler] Error generando reporte para {dayDate:dd/MM}: {ex.Message}");
                }
            }

            if (htmlFiles.Any())
            {
                // Create ZIP with all HTML reports
                var zipPath = await CreateWeeklyZipAsync(htmlFiles, startOfWeek);
                Console.WriteLine($"[SimplifiedReportsScheduler] ZIP creado: {zipPath}");

                // Prepare email
                var subject = $"📦 Reporte Semanal - Semana del {startOfWeek:dd/MM/yyyy}";
                var body = GenerateWeeklyZipEmailBody(startOfWeek, sendTime, htmlFiles.Count);
                var attachmentFiles = new List<string> { zipPath };

                // Send using EmailService
                var success = await _emailService.SendActivityDashboardReportAsync(
                    _config.SelectedRecipients,
                    subject,
                    body,
                    attachmentFiles,
                    false // ZIP already created
                );

                Console.WriteLine($"[SimplifiedReportsScheduler] Email semanal {(success ? "enviado exitosamente" : "falló")}");
                EmailSent?.Invoke(this, new ReportEmailSentEventArgs 
                { 
                    Success = success, 
                    ReportType = "Weekly",
                    Recipients = _config.SelectedRecipients.Count,
                    AttachmentCount = htmlFiles.Count
                });

                // Cleanup all temporary files
                CleanupTemporaryFiles(htmlFiles);
                if (File.Exists(zipPath))
                {
                    File.Delete(zipPath);
                }
            }
            else
            {
                Console.WriteLine("[SimplifiedReportsScheduler] Sin datos para reporte semanal");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SimplifiedReportsScheduler] Error en reporte semanal: {ex.Message}");
            throw;
        }
    }

    private async Task<string> CreateWeeklyZipAsync(List<string> htmlFiles, DateTime startOfWeek)
    {
        var tempFolder = Path.GetTempPath();
        var zipFileName = $"Reporte_Semanal_{startOfWeek:yyyyMMdd}.zip";
        var zipPath = Path.Combine(tempFolder, zipFileName);

        // Delete if exists
        if (File.Exists(zipPath))
        {
            File.Delete(zipPath);
        }

        using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            foreach (var htmlFile in htmlFiles)
            {
                if (File.Exists(htmlFile))
                {
                    var fileName = Path.GetFileName(htmlFile);
                    archive.CreateEntryFromFile(htmlFile, fileName);
                    Console.WriteLine($"[SimplifiedReportsScheduler] Agregado al ZIP: {fileName}");
                }
            }
        }

        Console.WriteLine($"[SimplifiedReportsScheduler] ZIP creado: {zipPath} ({new FileInfo(zipPath).Length / 1024}KB)");
        return zipPath;
    }

    private string GenerateSimpleEmailBody(ActivityReport report, string reportType)
    {
        var body = $@"
            <div style='font-family: Segoe UI, Arial, sans-serif; max-width: 600px;'>
                <h2 style='color: #2563eb;'>📊 {(reportType == "daily" ? "Reporte Diario" : "Reporte Semanal")} - ActivityDashboard</h2>
                
                <div style='background: #f8f9fa; padding: 15px; border-radius: 8px; margin: 15px 0;'>
                    <p><strong>Período:</strong> {report.ReportStartTime:dd/MM/yyyy HH:mm} - {report.ReportEndTime:dd/MM/yyyy HH:mm}</p>
                    <p><strong>Duración:</strong> {report.ReportDuration.TotalHours:F1} horas</p>
                    <p><strong>Cuadrantes:</strong> {report.Summary.TotalQuadrants}</p>
                    <p><strong>Actividades detectadas:</strong> {report.Summary.TotalActivities:N0}</p>
                    <p><strong>Actividad promedio:</strong> {report.Summary.AverageActivityRate:F1}%</p>
                </div>
                
                <p>📎 Adjunto encontrará el reporte detallado en formato HTML con gráficos interactivos.</p>
                
                <hr style='margin: 20px 0; border: none; border-top: 1px solid #dee2e6;'>
                <p style='color: #6c757d; font-size: 12px;'>
                    📅 Generado automáticamente el {DateTime.Now:dd/MM/yyyy HH:mm:ss}<br>
                    🖥️ Capturer Dashboard v2.5 - Sistema de Monitoreo Empresarial
                </p>
            </div>";
        
        return body;
    }

    private string GenerateWeeklyZipEmailBody(DateTime startOfWeek, DateTime sendTime, int fileCount)
    {
        var endOfWeek = startOfWeek.AddDays(6);
        
        var body = $@"
            <div style='font-family: Segoe UI, Arial, sans-serif; max-width: 600px;'>
                <h2 style='color: #2563eb;'>📦 Reporte Semanal - ActivityDashboard</h2>
                
                <div style='background: #f8f9fa; padding: 15px; border-radius: 8px; margin: 15px 0;'>
                    <p><strong>Semana:</strong> {startOfWeek:dd/MM/yyyy} - {endOfWeek:dd/MM/yyyy}</p>
                    <p><strong>Generado:</strong> {sendTime:dd/MM/yyyy HH:mm}</p>
                    <p><strong>Archivos incluidos:</strong> {fileCount} reportes HTML</p>
                    <p><strong>Contenido:</strong> Reportes de toda la semana (sistema registra 24/7)</p>
                </div>
                
                <div style='background: #e3f2fd; padding: 15px; border-radius: 8px; margin: 15px 0;'>
                    <h3 style='color: #1976d2; margin-top: 0;'>📋 Contenido del ZIP:</h3>
                    <ul style='margin: 10px 0;'>
                        <li>Reportes HTML individuales por día</li>
                        <li>Gráficos interactivos con datos por minuto</li>
                        <li>Estadísticas detalladas por cuadrante</li>
                        <li>Timeline de actividad completa</li>
                    </ul>
                </div>
                
                <p>📎 Descargue y extraiga el archivo ZIP adjunto para acceder a todos los reportes de la semana.</p>
                
                <hr style='margin: 20px 0; border: none; border-top: 1px solid #dee2e6;'>
                <p style='color: #6c757d; font-size: 12px;'>
                    📅 Generado automáticamente el {DateTime.Now:dd/MM/yyyy HH:mm:ss}<br>
                    🖥️ Capturer Dashboard v2.5 - Sistema de Monitoreo Empresarial
                </p>
            </div>";
        
        return body;
    }


    private DateTime GetStartOfWeek(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-1 * diff).Date;
    }

    private void CleanupTemporaryFiles(List<string> files)
    {
        foreach (var file in files)
        {
            try
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                    Console.WriteLine($"[SimplifiedReportsScheduler] Limpiado: {Path.GetFileName(file)}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SimplifiedReportsScheduler] Error limpiando {file}: {ex.Message}");
            }
        }
    }

    private void InitializeConfigPath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var configFolder = Path.Combine(appDataPath, "Capturer");
        Directory.CreateDirectory(configFolder);
        _configFilePath = Path.Combine(configFolder, "simplified-reports-config.json");
    }

    private void LoadConfiguration()
    {
        try
        {
            if (File.Exists(_configFilePath))
            {
                var json = File.ReadAllText(_configFilePath);
                var loaded = JsonSerializer.Deserialize<SimplifiedReportsConfig>(json);
                if (loaded != null)
                {
                    _config = loaded;
                    Console.WriteLine($"[SimplifiedReportsScheduler] Configuración cargada: {(_config.IsDaily ? "Diario" : "Semanal")}");
                }
            }
            else
            {
                Console.WriteLine("[SimplifiedReportsScheduler] Sin configuración previa, usando defaults");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SimplifiedReportsScheduler] Error cargando configuración: {ex.Message}");
        }
    }

    private void SaveConfiguration()
    {
        try
        {
            var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            File.WriteAllText(_configFilePath, json);
            Console.WriteLine("[SimplifiedReportsScheduler] Configuración guardada");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SimplifiedReportsScheduler] Error guardando configuración: {ex.Message}");
        }
    }

    public void Dispose()
    {
        try
        {
            _checkTimer?.Dispose();
            Console.WriteLine("[SimplifiedReportsScheduler] Servicio finalizado");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SimplifiedReportsScheduler] Error en dispose: {ex.Message}");
        }
    }
}

/// <summary>
/// Event args for email sent notifications
/// </summary>
public class ReportEmailSentEventArgs : EventArgs
{
    public bool Success { get; set; }
    public string ReportType { get; set; } = "";
    public int Recipients { get; set; }
    public int AttachmentCount { get; set; }
    public string? ErrorMessage { get; set; }
}