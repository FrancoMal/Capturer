using System.IO.Compression;
using System.Text; // ★ NUEVO: Para Encoding.UTF8
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
    /// Envía reporte HTML del día ANTERIOR completo (8 AM - 8 PM)
    /// MEJORA: Reporta período FINALIZADO para datos completos y confiables
    /// </summary>
    private async Task SendDailyHtmlReportAsync(DateTime sendTime)
    {
        try
        {
            Console.WriteLine("[SimplifiedReportsScheduler] Generando reporte HTML diario (día anterior)...");
            
            // ✅ CORREGIDO: Generate report for YESTERDAY (complete day)
            var yesterday = sendTime.Date.AddDays(-1);
            var startTime = yesterday.AddHours(8); // AYER 8 AM 
            var endTime = yesterday.AddHours(20);   // AYER 8 PM
            
            Console.WriteLine($"[SimplifiedReportsScheduler] Generando reporte para AYER {yesterday:dddd, dd/MM/yyyy} (8AM-8PM)");

            var report = await _reportService.GenerateReportAsync(startTime, endTime, "Daily-Automated");
            
            if (report.Entries.Any())
            {
                // Export to HTML
                var htmlPath = await _reportService.ExportReportAsync(report, "HTML");
                Console.WriteLine($"[SimplifiedReportsScheduler] HTML generado: {htmlPath}");

                // ✅ MEJORADO: Subject claro especificando período reportado
                var reportDate = sendTime.Date.AddDays(-1);
                var subject = $"📊 Reporte Diario de AYER - {reportDate:dddd dd/MM/yyyy} (8AM-8PM)";
                
                // ★ NUEVA MEJORA: Usar HTML del reporte como cuerpo del email
                var htmlContent = await File.ReadAllTextAsync(htmlPath, Encoding.UTF8);
                var emailHtmlBody = await GenerateEnhancedDailyEmailHtml(htmlContent, report, reportDate);
                
                var attachmentFiles = new List<string> { htmlPath };

                // ★ MEJORADO: Enviar con HTML embebido como cuerpo del email
                var success = await SendDailyEmailWithEmbeddedHtml(
                    _config.SelectedRecipients,
                    subject,
                    emailHtmlBody,
                    attachmentFiles
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
                
                // ★ NUEVO: Después de generar reporte diario, activar limpieza de archivos antiguos
                // Nota: Esto se coordina con ActivityReportService para mantener retención de 2 días
                _ = Task.Run(() =>
                {
                    try
                    {
                        // Llamar a ActivityReportService indirectamente a través de su limpieza periódica
                        // La limpieza real se maneja en ActivityDashboardSchedulerService y ActivityReportService
                        Console.WriteLine("[SimplifiedReportsScheduler] Reportes diarios coordinados - sistema de limpieza activado en otros servicios");
                    }
                    catch (Exception cleanupEx)
                    {
                        Console.WriteLine($"[SimplifiedReportsScheduler] Info cleanup: {cleanupEx.Message}");
                    }
                });
            }
            else
            {
                Console.WriteLine($"[SimplifiedReportsScheduler] Sin datos para reporte diario del {sendTime.Date.AddDays(-1):yyyy-MM-dd}");
                
                // Send notification email even if no data
                var yesterdayEmpty = sendTime.Date.AddDays(-1);
                var subject = $"📊 Reporte Diario de AYER - {yesterdayEmpty:dddd dd/MM/yyyy} (Sin Datos)";
                var body = GenerateEmptyDailyReportBody(yesterdayEmpty);
                
                await _emailService.SendActivityDashboardReportAsync(
                    _config.SelectedRecipients,
                    subject,
                    body,
                    new List<string>(), // No attachments
                    false
                );
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SimplifiedReportsScheduler] Error en reporte diario: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Envía ZIP con reportes HTML de la SEMANA PASADA completa (lunes-domingo)
    /// MEJORA: Reporta semana FINALIZADA para datos completos y confiables
    /// </summary>
    private async Task SendWeeklyZipReportAsync(DateTime sendTime)
    {
        try
        {
            Console.WriteLine("[SimplifiedReportsScheduler] Generando reportes semanales (semana pasada)...");
            
            var htmlFiles = new List<string>();
            // ✅ CORREGIDO: Get LAST week (complete week)
            var lastWeekStart = GetStartOfWeek(sendTime.AddDays(-7)); // Semana anterior
            
            Console.WriteLine($"[SimplifiedReportsScheduler] Procesando SEMANA PASADA: {lastWeekStart:dd/MM/yyyy} - {lastWeekStart.AddDays(6):dd/MM/yyyy}");
            
            // Generate HTML for each day of LAST week
            for (int i = 0; i < 7; i++)
            {
                var dayDate = lastWeekStart.AddDays(i);
                
                // System records all days 24/7, generate report for each day of LAST week
                Console.WriteLine($"[SimplifiedReportsScheduler] Procesando {dayDate:dd/MM} ({dayDate.DayOfWeek}) [SEMANA PASADA]");

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
                var zipPath = await CreateWeeklyZipAsync(htmlFiles, lastWeekStart);
                Console.WriteLine($"[SimplifiedReportsScheduler] ZIP creado: {zipPath}");

                // ✅ MEJORADO: Subject claro especificando semana reportada
                var lastWeekEnd = lastWeekStart.AddDays(6);
                var subject = $"📦 Reporte Semanal de la SEMANA PASADA - {lastWeekStart:dd/MM} al {lastWeekEnd:dd/MM/yyyy}";
                var body = GenerateWeeklyZipEmailBody(lastWeekStart, sendTime, htmlFiles.Count);
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
                
                // ★ NUEVO: Nota de coordinación - la limpieza de archivos persistentes
                // se maneja automáticamente por ActivityDashboardScheduler y ActivityReportService
                Console.WriteLine("[SimplifiedReportsScheduler] Reportes semanales coordinados - sistema de limpieza activo");
            }
            else
            {
                var lastWeekStartEmpty = GetStartOfWeek(sendTime.AddDays(-7));
                var lastWeekEndEmpty = lastWeekStartEmpty.AddDays(6);
                Console.WriteLine($"[SimplifiedReportsScheduler] Sin datos para reporte semanal {lastWeekStartEmpty:yyyy-MM-dd} - {lastWeekEndEmpty:yyyy-MM-dd}");
                
                // Send notification email even if no data
                var subject = $"📦 Reporte Semanal de la SEMANA PASADA - {lastWeekStartEmpty:dd/MM} al {lastWeekEndEmpty:dd/MM/yyyy} (Sin Datos)";
                var body = GenerateEmptyWeeklyReportBody(lastWeekStartEmpty, lastWeekEndEmpty);
                
                await _emailService.SendActivityDashboardReportAsync(
                    _config.SelectedRecipients,
                    subject,
                    body,
                    new List<string>(), // No attachments
                    false
                );
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

    private string GenerateSimpleEmailBody(ActivityReport report, string reportType, DateTime? specificDate = null)
    {
        var reportTitle = reportType == "daily" ? 
            (specificDate.HasValue ? $"Reporte Diario de AYER ({specificDate.Value:dddd dd/MM/yyyy})" : "Reporte Diario") : 
            "Reporte Semanal";
            
        var body = $@"
            <div style='font-family: Segoe UI, Arial, sans-serif; max-width: 600px;'>
                <h2 style='color: #2563eb;'>📊 {reportTitle} - ActivityDashboard</h2>
                
                <div style='background: #f8f9fa; padding: 15px; border-radius: 8px; margin: 15px 0;'>
                    <p><strong>🕒 Período Reportado:</strong> {report.ReportStartTime:dd/MM/yyyy HH:mm} - {report.ReportEndTime:dd/MM/yyyy HH:mm}</p>
                    <p><strong>⏱️ Duración:</strong> {report.ReportDuration.TotalHours:F1} horas</p>
                    <p><strong>Cuadrantes:</strong> {report.Summary.TotalQuadrants}</p>
                    <p><strong>Actividades detectadas:</strong> {report.Summary.TotalActivities:N0}</p>
                    <p><strong>Actividad promedio:</strong> {report.Summary.AverageActivityRate:F1}%</p>
                </div>
                
                {(reportType == "daily" && specificDate.HasValue ? 
                    $"<div style='background: #e8f5e8; padding: 10px; border-radius: 5px; margin: 15px 0; border-left: 4px solid #28a745;'>\n" +
                    $"    <p style='margin: 0; color: #155724;'><strong>ℹ️ Aclaración:</strong> Este reporte contiene los datos <strong>completos</strong> del día anterior ({specificDate.Value:dddd dd/MM/yyyy}) de 8:00 AM a 8:00 PM.</p>\n" +
                    "</div>" : 
                    "")}
                
                <p>📎 Adjunto encontrará el reporte detallado en formato HTML con gráficos interactivos.</p>
                
                <hr style='margin: 20px 0; border: none; border-top: 1px solid #dee2e6;'>
                <p style='color: #6c757d; font-size: 12px;'>
                    📅 Generado automáticamente el {DateTime.Now:dd/MM/yyyy HH:mm:ss}<br>
                    🖥️ Capturer Dashboard v3.2.0 - Sistema de Monitoreo Empresarial
                </p>
            </div>";
        
        return body;
    }

    private string GenerateWeeklyZipEmailBody(DateTime startOfWeek, DateTime sendTime, int fileCount)
    {
        var endOfWeek = startOfWeek.AddDays(6);
        
        var body = $@"
            <div style='font-family: Segoe UI, Arial, sans-serif; max-width: 600px;'>
                <h2 style='color: #2563eb;'>📦 Reporte Semanal de la SEMANA PASADA - ActivityDashboard</h2>
                
                <div style='background: #e8f5e8; padding: 15px; border-radius: 8px; margin: 15px 0; border-left: 4px solid #28a745;'>
                    <p><strong>ℹ️ Aclaración:</strong> Este reporte contiene los datos <strong>completos</strong> de la semana anterior.</p>
                </div>
                
                <div style='background: #f8f9fa; padding: 15px; border-radius: 8px; margin: 15px 0;'>
                    <p><strong>🕒 Semana Reportada:</strong> {startOfWeek:dd/MM/yyyy} - {endOfWeek:dd/MM/yyyy}</p>
                    <p><strong>📅 Generado:</strong> {sendTime:dd/MM/yyyy HH:mm}</p>
                    <p><strong>📎 Archivos incluidos:</strong> {fileCount} reportes HTML</p>
                    <p><strong>📋 Contenido:</strong> Reportes diarios completos (8AM-8PM cada día)</p>
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
                    🖥️ Capturer Dashboard v3.2.0 - Sistema de Monitoreo Empresarial
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

    private string GenerateEmptyDailyReportBody(DateTime reportDate)
    {
        return $@"
            <div style='font-family: Segoe UI, Arial, sans-serif; max-width: 600px;'>
                <h2 style='color: #2563eb;'>📊 Reporte Diario de AYER ({reportDate:dddd dd/MM/yyyy}) - ActivityDashboard</h2>
                
                <div style='background: #fff3cd; padding: 15px; border-radius: 8px; margin: 15px 0; border-left: 4px solid #ffc107;'>
                    <p style='margin: 0; color: #856404;'><strong>⚠️ Sin Datos:</strong> No se registró actividad en los cuadrantes durante el período de ayer ({reportDate:dd/MM/yyyy}) de 8:00 AM a 8:00 PM.</p>
                </div>
                
                <div style='background: #f8f9fa; padding: 15px; border-radius: 8px; margin: 15px 0;'>
                    <p><strong>🕒 Período Revisado:</strong> {reportDate:dd/MM/yyyy} 08:00 - 20:00</p>
                    <p><strong>📋 Posibles Causas:</strong></p>
                    <ul style='margin: 10px 0; padding-left: 20px;'>
                        <li>No hubo actividad en los cuadrantes monitoreados</li>
                        <li>Sistema de monitoreo pausado temporalmente</li>
                        <li>Configuración de cuadrantes pendiente</li>
                    </ul>
                </div>
                
                <hr style='margin: 20px 0; border: none; border-top: 1px solid #dee2e6;'>
                <p style='color: #6c757d; font-size: 12px;'>
                    📅 Generado automáticamente el {DateTime.Now:dd/MM/yyyy HH:mm:ss}<br>
                    🖥️ Capturer Dashboard v3.2.0 - Sistema de Monitoreo Empresarial
                </p>
            </div>";
    }
    
    private string GenerateEmptyWeeklyReportBody(DateTime startOfWeek, DateTime endOfWeek)
    {
        return $@"
            <div style='font-family: Segoe UI, Arial, sans-serif; max-width: 600px;'>
                <h2 style='color: #2563eb;'>📦 Reporte Semanal de la SEMANA PASADA ({startOfWeek:dd/MM} - {endOfWeek:dd/MM/yyyy}) - ActivityDashboard</h2>
                
                <div style='background: #fff3cd; padding: 15px; border-radius: 8px; margin: 15px 0; border-left: 4px solid #ffc107;'>
                    <p style='margin: 0; color: #856404;'><strong>⚠️ Sin Datos:</strong> No se registró actividad en los cuadrantes durante toda la semana pasada.</p>
                </div>
                
                <div style='background: #f8f9fa; padding: 15px; border-radius: 8px; margin: 15px 0;'>
                    <p><strong>🕒 Semana Revisada:</strong> {startOfWeek:dd/MM/yyyy} - {endOfWeek:dd/MM/yyyy}</p>
                    <p><strong>📋 Posibles Causas:</strong></p>
                    <ul style='margin: 10px 0; padding-left: 20px;'>
                        <li>No hubo actividad en los cuadrantes monitoreados</li>
                        <li>Sistema de monitoreo pausado durante la semana</li>
                        <li>Configuración de cuadrantes pendiente</li>
                        <li>Período de vacaciones o fin de semana extendido</li>
                    </ul>
                </div>
                
                <hr style='margin: 20px 0; border: none; border-top: 1px solid #dee2e6;'>
                <p style='color: #6c757d; font-size: 12px;'>
                    📅 Generado automáticamente el {DateTime.Now:dd/MM/yyyy HH:mm:ss}<br>
                    🖥️ Capturer Dashboard v3.2.0 - Sistema de Monitoreo Empresarial
                </p>
            </div>";
    }
    
    /// <summary>
    /// ★ NUEVO: Genera HTML embebido para email de reporte diario
    /// Procesa el HTML del reporte y lo adapta para email con información adicional
    /// </summary>
    private async Task<string> GenerateEnhancedDailyEmailHtml(string reportHtmlContent, ActivityReport report, DateTime reportDate)
    {
        try
        {
            // Leer el HTML completo del reporte
            var html = reportHtmlContent;
            
            // ✅ Backend Reliability: Validar que el HTML sea válido
            if (string.IsNullOrEmpty(html) || !html.Contains("<!DOCTYPE html>"))
            {
                Console.WriteLine("[SimplifiedReportsScheduler] HTML inválido, usando fallback");
                return GenerateSimpleEmailBody(report, "daily", reportDate);
            }
            
            // ★ MEJORA: Insertar información adicional de email antes del cierre del body
            var emailFooter = $@"
            <div style='margin-top: 40px; padding: 20px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; border-radius: 10px;'>
                <h3 style='margin: 0 0 10px 0; color: white;'>📎 Reporte Adjunto</h3>
                <p style='margin: 5px 0; opacity: 0.9;'>📄 Para su conveniencia, también encontrará este reporte como archivo HTML adjunto.</p>
                <p style='margin: 5px 0; opacity: 0.9;'>🖥️ El archivo adjunto contiene la misma información con funcionalidad completa offline.</p>
            </div>
            
            <div style='margin-top: 30px; padding: 15px; background: #f8f9fa; border-radius: 8px; border-left: 4px solid #007bff;'>
                <p style='margin: 0; font-size: 12px; color: #6c757d;'>
                    📅 Email generado automáticamente el {DateTime.Now:dd/MM/yyyy HH:mm:ss}<br>
                    🖥️ Capturer Dashboard v3.2.0 - Sistema de Monitoreo Empresarial<br>
                    🕒 Reporte del período: {reportDate:dddd dd/MM/yyyy} 8:00 AM - 8:00 PM
                </p>
            </div>";
            
            // Insertar footer antes del cierre del body
            var bodyCloseIndex = html.LastIndexOf("</body>");
            if (bodyCloseIndex > 0)
            {
                html = html.Insert(bodyCloseIndex, emailFooter);
            }
            else
            {
                // Fallback: agregar al final
                html += emailFooter + "</body></html>";
            }
            
            return html;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SimplifiedReportsScheduler] Error procesando HTML para email: {ex.Message}");
            // Fallback a email simple
            return GenerateSimpleEmailBody(report, "daily", reportDate);
        }
    }
    
    /// <summary>
    /// ★ NUEVO: Envía email diario con HTML embebido como cuerpo
    /// Utiliza el nuevo método especializado de EmailService para HTML completo
    /// </summary>
    private async Task<bool> SendDailyEmailWithEmbeddedHtml(List<string> recipients, string subject, string htmlBody, List<string> attachmentFiles)
    {
        try
        {
            // ★ USAR NUEVO MÉTODO especializado para HTML embebido
            var success = await _emailService.SendDailyActivityReportWithEmbeddedHtmlAsync(
                recipients,
                subject,
                htmlBody, // ★ CLAVE: HTML completo del reporte como cuerpo
                attachmentFiles // ★ ADEMÁS: Archivo HTML adjunto
            );
            
            return success;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SimplifiedReportsScheduler] Error enviando email con HTML embebido: {ex.Message}");
            return false;
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