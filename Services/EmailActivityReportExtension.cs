using MimeKit;
using System.Reflection;
using System.Text;
using Capturer.Models;

namespace Capturer.Services;

/// <summary>
/// Extensión del EmailService para envío de reportes de actividad
/// </summary>
public static class EmailActivityReportExtension
{
    /// <summary>
    /// Envía un reporte de actividad por email integrándose con el sistema existente
    /// </summary>
    public static async Task<bool> SendActivityReportAsync(
        this IEmailService emailService,
        ActivityReport report,
        List<string> recipients,
        ActivityEmailIntegration config)
    {
        try
        {
            Console.WriteLine($"[EmailActivityReport] Enviando reporte de actividad: {report.Id}");

            // Create temporary activity report service to handle export
            using var reportService = new ActivityReportService(
                new QuadrantActivityService(), // Temporary instance
                new ActivityReportConfiguration { AutoSaveReports = false });

            var attachments = new List<string>();

            // Export reports in preferred formats
            foreach (var format in config.PreferredFormats)
            {
                try
                {
                    var exportPath = await reportService.ExportReportAsync(report, format);
                    attachments.Add(exportPath);
                    Console.WriteLine($"[EmailActivityReport] Reporte exportado en {format}: {exportPath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[EmailActivityReport] Error exportando {format}: {ex.Message}");
                }
            }

            // Create the email
            var message = new MimeMessage();
            
            // Set sender using reflection to access EmailService configuration
            SetEmailSender(emailService, message);
            
            message.Subject = GenerateSubject(report, config);

            // Create body
            var bodyBuilder = new BodyBuilder();
            
            if (config.EmbedSummaryInEmail)
            {
                bodyBuilder.HtmlBody = GenerateEmailBody(report);
                bodyBuilder.TextBody = GenerateTextEmailBody(report);
            }
            else
            {
                bodyBuilder.TextBody = $"Adjunto encontrará el reporte de actividad para el período " +
                                     $"{report.ReportStartTime:dd/MM/yyyy} a {report.ReportEndTime:dd/MM/yyyy}.\n\n" +
                                     $"Generado automáticamente por Capturer Dashboard de Actividad.";
            }

            // Add attachments if configured
            if (config.AttachDetailedReport && attachments.Any())
            {
                foreach (var attachment in attachments)
                {
                    try
                    {
                        if (File.Exists(attachment))
                        {
                            await bodyBuilder.Attachments.AddAsync(attachment);
                            Console.WriteLine($"[EmailActivityReport] Adjunto agregado: {Path.GetFileName(attachment)}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[EmailActivityReport] Error agregando adjunto {attachment}: {ex.Message}");
                    }
                }
            }

            message.Body = bodyBuilder.ToMessageBody();

            // Add recipients to the message
            foreach (var recipient in recipients)
            {
                message.To.Add(MailboxAddress.Parse(recipient));
            }

            // Use reflection to access the private SendEmailAsync method of EmailService
            var success = await InvokeEmailServiceSendAsync(emailService, message);

            // Cleanup temporary files
            CleanupTemporaryFiles(attachments);

            if (success)
            {
                Console.WriteLine($"[EmailActivityReport] Reporte de actividad enviado exitosamente a {recipients.Count} destinatarios");
            }

            return success;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EmailActivityReport] Error enviando reporte de actividad: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Integra reportes de actividad en reportes manuales existentes
    /// </summary>
    public static async Task<bool> SendManualReportWithActivityAsync(
        this IEmailService emailService,
        List<string> recipients,
        DateTime startDate,
        DateTime endDate,
        ActivityReport? activityReport = null,
        bool includeActivityData = false)
    {
        try
        {
            if (!includeActivityData || activityReport == null)
            {
                // Send regular manual report
                return await emailService.SendManualReportAsync(recipients, startDate, endDate);
            }

            Console.WriteLine($"[EmailActivityReport] Enviando reporte manual con datos de actividad incluidos");

            // Create enhanced email with activity data
            var config = new ActivityEmailIntegration
            {
                AttachDetailedReport = true,
                EmbedSummaryInEmail = true,
                PreferredFormats = new List<string> { "HTML", "CSV" }
            };

            // Send the activity report as part of manual report
            var activitySuccess = await emailService.SendActivityReportAsync(activityReport, recipients, config);
            
            // Also send regular screenshots if needed
            var regularSuccess = await emailService.SendManualReportAsync(recipients, startDate, endDate);

            return activitySuccess && regularSuccess;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EmailActivityReport] Error en reporte manual con actividad: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Integra reportes de actividad en reportes programados
    /// </summary>
    public static async Task<bool> SendScheduledReportWithActivityAsync(
        this IEmailService emailService,
        List<string> recipients,
        DateTime startDate,
        DateTime endDate,
        List<string> screenshots,
        ActivityReport? activityReport = null,
        ActivityEmailIntegration? config = null)
    {
        try
        {
            config ??= new ActivityEmailIntegration
            {
                IncludeInScheduledReports = true,
                AttachDetailedReport = true,
                EmbedSummaryInEmail = false,
                PreferredFormats = new List<string> { "CSV" }
            };

            if (!config.IncludeInScheduledReports || activityReport == null)
            {
                // Send regular scheduled report
                return await emailService.SendEnhancedReportAsync(recipients, startDate, endDate, screenshots, "Scheduled", true);
            }

            Console.WriteLine($"[EmailActivityReport] Enviando reporte programado con datos de actividad");

            // Send activity report first
            var activitySuccess = await emailService.SendActivityReportAsync(activityReport, recipients, config);
            
            // Then send regular screenshots report
            var regularSuccess = await emailService.SendEnhancedReportAsync(recipients, startDate, endDate, screenshots, "Scheduled+Activity", true);

            return activitySuccess && regularSuccess;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EmailActivityReport] Error en reporte programado con actividad: {ex.Message}");
            return false;
        }
    }

    #region Private Helper Methods

    private static string GenerateSubject(ActivityReport report, ActivityEmailIntegration config)
    {
        var fileName = config.ReportFileNamePattern
            .Replace("{StartDate:yyyyMMdd}", report.ReportStartTime.ToString("yyyyMMdd"))
            .Replace("{EndDate:yyyyMMdd}", report.ReportEndTime.ToString("yyyyMMdd"));

        // Use session name if available, otherwise fall back to date range
        if (!string.IsNullOrEmpty(report.SessionName))
        {
            return $"Reporte de Actividad - {report.SessionName} ({report.ReportStartTime:dd/MM/yyyy} a {report.ReportEndTime:dd/MM/yyyy})";
        }
        
        return $"Reporte de Actividad - {report.ReportStartTime:dd/MM/yyyy} a {report.ReportEndTime:dd/MM/yyyy}";
    }

    private static string GenerateEmailBody(ActivityReport report)
    {
        var html = new StringBuilder();
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html><head><meta charset='utf-8'><title>Reporte de Actividad</title>");
        html.AppendLine("<style>");
        html.AppendLine("body { font-family: 'Segoe UI', Arial, sans-serif; margin: 20px; background-color: #f5f5f5; }");
        html.AppendLine(".container { background-color: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }");
        html.AppendLine(".header { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 20px; border-radius: 8px; margin-bottom: 20px; }");
        html.AppendLine(".summary { background-color: #e3f2fd; padding: 15px; margin: 20px 0; border-radius: 5px; border-left: 4px solid #2196F3; }");
        html.AppendLine(".stats-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 15px; margin: 20px 0; }");
        html.AppendLine(".stat-card { background: #f8f9fa; padding: 15px; border-radius: 8px; text-align: center; border: 1px solid #dee2e6; }");
        html.AppendLine(".stat-number { font-size: 24px; font-weight: bold; color: #495057; }");
        html.AppendLine(".stat-label { font-size: 12px; color: #6c757d; text-transform: uppercase; margin-top: 5px; }");
        html.AppendLine("table { border-collapse: collapse; width: 100%; margin: 20px 0; }");
        html.AppendLine("th, td { border: 1px solid #ddd; padding: 12px; text-align: left; }");
        html.AppendLine("th { background-color: #f8f9fa; font-weight: 600; }");
        html.AppendLine("tr:nth-child(even) { background-color: #f8f9fa; }");
        html.AppendLine(".footer { margin-top: 30px; padding-top: 20px; border-top: 1px solid #dee2e6; font-size: 12px; color: #6c757d; }");
        html.AppendLine("</style></head><body>");

        html.AppendLine("<div class='container'>");
        html.AppendLine("<div class='header'>");
        html.AppendLine("<h1>🎯 Reporte de Actividad - Dashboard</h1>");
        if (!string.IsNullOrEmpty(report.SessionName))
        {
            html.AppendLine($"<p>Sesión: <strong>{report.SessionName}</strong></p>");
        }
        html.AppendLine($"<p>Período: <strong>{report.ReportStartTime:dd/MM/yyyy HH:mm} - {report.ReportEndTime:dd/MM/yyyy HH:mm}</strong></p>");
        html.AppendLine($"<p>Duración: <strong>{report.ReportDuration.TotalHours:F1} horas</strong></p>");
        if (!string.IsNullOrEmpty(report.QuadrantConfigurationName))
        {
            html.AppendLine($"<p>Configuración: <strong>{report.QuadrantConfigurationName}</strong></p>");
        }
        html.AppendLine("</div>");

        // Summary cards
        html.AppendLine("<div class='stats-grid'>");
        html.AppendLine($"<div class='stat-card'><div class='stat-number'>{report.Summary.TotalQuadrants}</div><div class='stat-label'>Cuadrantes</div></div>");
        html.AppendLine($"<div class='stat-card'><div class='stat-number'>{report.Summary.TotalComparisons:N0}</div><div class='stat-label'>Comparaciones</div></div>");
        html.AppendLine($"<div class='stat-card'><div class='stat-number'>{report.Summary.TotalActivities:N0}</div><div class='stat-label'>Actividades</div></div>");
        html.AppendLine($"<div class='stat-card'><div class='stat-number'>{report.Summary.AverageActivityRate:F1}%</div><div class='stat-label'>Actividad Promedio</div></div>");
        html.AppendLine("</div>");

        html.AppendLine("<div class='summary'>");
        html.AppendLine($"<h3>📊 Resumen Ejecutivo</h3>");
        html.AppendLine($"<p><strong>Nivel de Actividad:</strong> {report.Summary.ActivityLevel}</p>");
        html.AppendLine($"<p><strong>Cuadrante Más Activo:</strong> {report.Summary.HighestActivityQuadrant}</p>");
        html.AppendLine($"<p><strong>Eficiencia de Monitoreo:</strong> {report.Summary.MonitoringEfficiency:F1}%</p>");
        html.AppendLine("</div>");

        // Details table
        html.AppendLine("<h3>📋 Detalles por Cuadrante</h3>");
        html.AppendLine("<table>");
        html.AppendLine("<tr><th>Cuadrante</th><th>Comparaciones</th><th>Actividades</th><th>Tasa (%)</th><th>Cambio Prom. (%)</th><th>Duración</th></tr>");

        foreach (var entry in report.Entries.OrderByDescending(e => e.ActivityRate))
        {
            html.AppendLine("<tr>");
            html.AppendLine($"<td><strong>{entry.QuadrantName}</strong></td>");
            html.AppendLine($"<td>{entry.TotalComparisons:N0}</td>");
            html.AppendLine($"<td>{entry.ActivityDetectionCount:N0}</td>");
            html.AppendLine($"<td>{entry.ActivityRate:F1}%</td>");
            html.AppendLine($"<td>{entry.AverageChangePercentage:F2}%</td>");
            html.AppendLine($"<td>{entry.ActiveDuration:hh\\:mm\\:ss}</td>");
            html.AppendLine("</tr>");
        }

        html.AppendLine("</table>");

        html.AppendLine("<div class='footer'>");
        html.AppendLine($"<p>📅 Reporte generado automáticamente el {DateTime.Now:dd/MM/yyyy HH:mm:ss}</p>");
        html.AppendLine("<p>🖥️ Capturer - Dashboard de Actividad | Sistema de Monitoreo Empresarial</p>");
        html.AppendLine("</div>");

        html.AppendLine("</div>");
        html.AppendLine("</body></html>");

        return html.ToString();
    }

    private static string GenerateTextEmailBody(ActivityReport report)
    {
        var text = new StringBuilder();
        text.AppendLine("REPORTE DE ACTIVIDAD - CAPTURER DASHBOARD");
        text.AppendLine(new string('=', 50));
        if (!string.IsNullOrEmpty(report.SessionName))
        {
            text.AppendLine($"Sesión: {report.SessionName}");
        }
        text.AppendLine($"Período: {report.ReportStartTime:dd/MM/yyyy HH:mm} - {report.ReportEndTime:dd/MM/yyyy HH:mm}");
        text.AppendLine($"Duración: {report.ReportDuration.TotalHours:F1} horas");
        if (!string.IsNullOrEmpty(report.QuadrantConfigurationName))
        {
            text.AppendLine($"Configuración: {report.QuadrantConfigurationName}");
        }
        text.AppendLine();

        text.AppendLine("RESUMEN:");
        text.AppendLine(new string('-', 20));
        text.AppendLine($"Cuadrantes monitoreados: {report.Summary.TotalQuadrants}");
        text.AppendLine($"Comparaciones realizadas: {report.Summary.TotalComparisons:N0}");
        text.AppendLine($"Actividades detectadas: {report.Summary.TotalActivities:N0}");
        text.AppendLine($"Actividad promedio: {report.Summary.AverageActivityRate:F1}% ({report.Summary.ActivityLevel})");
        text.AppendLine($"Cuadrante más activo: {report.Summary.HighestActivityQuadrant}");
        text.AppendLine($"Eficiencia de monitoreo: {report.Summary.MonitoringEfficiency:F1}%");
        text.AppendLine();

        text.AppendLine("DETALLES POR CUADRANTE:");
        text.AppendLine(new string('-', 25));
        foreach (var entry in report.Entries.OrderByDescending(e => e.ActivityRate))
        {
            text.AppendLine($"\n[{entry.QuadrantName}]");
            text.AppendLine($"  Comparaciones: {entry.TotalComparisons:N0}");
            text.AppendLine($"  Actividades: {entry.ActivityDetectionCount:N0}");
            text.AppendLine($"  Tasa: {entry.ActivityRate:F1}%");
            text.AppendLine($"  Cambio promedio: {entry.AverageChangePercentage:F2}%");
            text.AppendLine($"  Duración activa: {entry.ActiveDuration:hh\\:mm\\:ss}");
        }

        text.AppendLine($"\n\nGenerado automáticamente el {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
        text.AppendLine("Capturer - Sistema de Monitoreo Empresarial");

        return text.ToString();
    }

    private static void SetEmailSender(IEmailService emailService, MimeMessage message)
    {
        try
        {
            // Use reflection to access the private _config field
            var emailServiceType = emailService.GetType();
            var configField = emailServiceType.GetField("_config", 
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (configField != null)
            {
                var config = (CapturerConfiguration)configField.GetValue(emailService)!;
                message.From.Add(new MailboxAddress(config.Email.SenderName, config.Email.Username));
            }
            else
            {
                // Fallback to default sender
                message.From.Add(new MailboxAddress("Capturer Activity Report", "capturer@system.local"));
                Console.WriteLine("[EmailActivityReport] Warning: No se pudo acceder a la configuración, usando sender por defecto");
            }
        }
        catch (Exception ex)
        {
            // Fallback to default sender
            message.From.Add(new MailboxAddress("Capturer Activity Report", "capturer@system.local"));
            Console.WriteLine($"[EmailActivityReport] Warning: Error accediendo a configuración de sender: {ex.Message}");
        }
    }

    private static async Task<bool> InvokeEmailServiceSendAsync(IEmailService emailService, MimeMessage message)
    {
        try
        {
            // Use reflection to access the private SendEmailAsync method
            var emailServiceType = emailService.GetType();
            var sendEmailMethod = emailServiceType.GetMethod("SendEmailAsync", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (sendEmailMethod != null)
            {
                var task = (Task<bool>)sendEmailMethod.Invoke(emailService, new object[] { message })!;
                return await task;
            }
            else
            {
                Console.WriteLine("[EmailActivityReport] Error: No se pudo acceder al método SendEmailAsync");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EmailActivityReport] Error invocando EmailService.SendEmailAsync: {ex.Message}");
            return false;
        }
    }

    private static void CleanupTemporaryFiles(List<string> files)
    {
        foreach (var file in files)
        {
            try
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                    Console.WriteLine($"[EmailActivityReport] Archivo temporal eliminado: {Path.GetFileName(file)}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EmailActivityReport] Error eliminando archivo temporal {file}: {ex.Message}");
            }
        }
    }

    #endregion
}