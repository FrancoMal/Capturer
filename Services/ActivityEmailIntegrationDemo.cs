using Capturer.Models;

namespace Capturer.Services;

/// <summary>
/// Demostración de cómo integrar los reportes de actividad con el sistema de email existente
/// </summary>
public class ActivityEmailIntegrationDemo
{
    private readonly IEmailService _emailService;
    private readonly ActivityReportService _reportService;
    private readonly QuadrantActivityService _activityService;

    public ActivityEmailIntegrationDemo(
        IEmailService emailService, 
        QuadrantActivityService activityService)
    {
        _emailService = emailService;
        _activityService = activityService;
        _reportService = new ActivityReportService(activityService);
    }

    /// <summary>
    /// Ejemplo 1: Envío manual de reporte con datos de actividad
    /// </summary>
    public async Task<bool> SendManualReportWithActivityExample(
        List<string> recipients, 
        DateTime startDate, 
        DateTime endDate)
    {
        try
        {
            Console.WriteLine("[ActivityEmailDemo] Generando reporte manual con datos de actividad...");

            // Generate activity report for the specified period
            var activityReport = await _reportService.GenerateReportAsync(startDate, endDate, "Manual");

            // Configure email integration
            var emailConfig = new ActivityEmailIntegration
            {
                IncludeInManualReports = true,
                AttachDetailedReport = true,
                EmbedSummaryInEmail = true,
                PreferredFormats = new List<string> { "HTML", "CSV" }
            };

            // Send using the extension method
            var success = await _emailService.SendActivityReportAsync(activityReport, recipients, emailConfig);

            if (success)
            {
                Console.WriteLine("[ActivityEmailDemo] Reporte manual con actividad enviado exitosamente");
            }

            return success;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ActivityEmailDemo] Error en reporte manual con actividad: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Ejemplo 2: Envío programado con datos de actividad
    /// </summary>
    public async Task<bool> SendScheduledReportWithActivityExample(
        List<string> recipients,
        DateTime startDate,
        DateTime endDate,
        List<string> screenshots)
    {
        try
        {
            Console.WriteLine("[ActivityEmailDemo] Generando reporte programado con datos de actividad...");

            // Generate activity report
            var activityReport = await _reportService.GenerateReportAsync(startDate, endDate, "Scheduled");

            // Configure for scheduled reports (more conservative)
            var emailConfig = new ActivityEmailIntegration
            {
                IncludeInScheduledReports = true,
                AttachDetailedReport = true,
                EmbedSummaryInEmail = false, // Don't embed in scheduled emails to keep them clean
                PreferredFormats = new List<string> { "CSV" } // Only CSV for scheduled reports
            };

            // Send using the extension method
            var success = await _emailService.SendScheduledReportWithActivityAsync(
                recipients, startDate, endDate, screenshots, activityReport, emailConfig);

            if (success)
            {
                Console.WriteLine("[ActivityEmailDemo] Reporte programado con actividad enviado exitosamente");
            }

            return success;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ActivityEmailDemo] Error en reporte programado con actividad: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Ejemplo 3: Configuración empresarial con reportes de actividad automáticos
    /// </summary>
    public async Task<bool> SendEnterpriseActivityReportExample(
        List<string> recipients,
        string reportType = "Weekly")
    {
        try
        {
            Console.WriteLine($"[ActivityEmailDemo] Generando reporte empresarial {reportType}...");

            // Calculate date range based on report type
            var (startDate, endDate) = CalculateReportPeriod(reportType);

            // Generate comprehensive activity report
            var activityReport = await _reportService.GenerateReportAsync(startDate, endDate, reportType);

            // Enterprise configuration with all formats
            var emailConfig = new ActivityEmailIntegration
            {
                AttachDetailedReport = true,
                EmbedSummaryInEmail = true,
                PreferredFormats = new List<string> { "HTML", "CSV", "JSON" },
                ReportFileNamePattern = $"Enterprise_Activity_{reportType}_{{StartDate:yyyyMMdd}}_{{EndDate:yyyyMMdd}}"
            };

            // Send comprehensive report
            var success = await _emailService.SendActivityReportAsync(activityReport, recipients, emailConfig);

            if (success)
            {
                Console.WriteLine($"[ActivityEmailDemo] Reporte empresarial {reportType} enviado exitosamente");
            }

            return success;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ActivityEmailDemo] Error en reporte empresarial: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Ejemplo 4: Reporte de actividad solo (sin screenshots)
    /// </summary>
    public async Task<bool> SendActivityOnlyReportExample(
        List<string> recipients,
        DateTime startDate,
        DateTime endDate,
        string? customMessage = null)
    {
        try
        {
            Console.WriteLine("[ActivityEmailDemo] Enviando reporte de actividad pura...");

            // Generate activity report
            var activityReport = await _reportService.GenerateReportAsync(startDate, endDate, "ActivityOnly");
            
            if (!string.IsNullOrEmpty(customMessage))
            {
                activityReport.Comments = customMessage;
            }

            // Configuration for activity-only reports
            var emailConfig = new ActivityEmailIntegration
            {
                AttachDetailedReport = true,
                EmbedSummaryInEmail = true,
                PreferredFormats = new List<string> { "HTML" }, // Only HTML for rich viewing
                ReportFileNamePattern = "ActivityReport_{StartDate:yyyyMMdd}_{EndDate:yyyyMMdd}"
            };

            var success = await _emailService.SendActivityReportAsync(activityReport, recipients, emailConfig);

            if (success)
            {
                Console.WriteLine("[ActivityEmailDemo] Reporte de actividad pura enviado exitosamente");
            }

            return success;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ActivityEmailDemo] Error en reporte de actividad pura: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Método utilitario para mostrar cómo usar desde el dashboard principal
    /// </summary>
    public static async Task DemoFromMainForm(
        IEmailService emailService,
        QuadrantActivityService activityService,
        List<string> recipients)
    {
        var demo = new ActivityEmailIntegrationDemo(emailService, activityService);

        // Ejemplo de uso desde Form1 o desde el Dashboard
        var yesterday = DateTime.Now.Date.AddDays(-1);
        var today = DateTime.Now.Date;

        Console.WriteLine("[ActivityEmailDemo] === DEMO DE INTEGRACIÓN DE REPORTES DE ACTIVIDAD ===");

        // 1. Reporte manual con actividad
        await demo.SendManualReportWithActivityExample(recipients, yesterday, today);

        // 2. Reporte de actividad pura
        await demo.SendActivityOnlyReportExample(recipients, yesterday, today, 
            "Reporte generado desde el Dashboard de Actividad");

        // 3. Reporte empresarial semanal
        await demo.SendEnterpriseActivityReportExample(recipients, "Weekly");

        Console.WriteLine("[ActivityEmailDemo] === FIN DEL DEMO ===");
    }

    private (DateTime startDate, DateTime endDate) CalculateReportPeriod(string reportType)
    {
        var now = DateTime.Now;
        return reportType.ToLower() switch
        {
            "daily" => (now.Date.AddDays(-1), now.Date),
            "weekly" => (now.Date.AddDays(-7), now.Date),
            "monthly" => (now.Date.AddMonths(-1), now.Date),
            _ => (now.Date.AddDays(-1), now.Date)
        };
    }

    public void Dispose()
    {
        _reportService?.Dispose();
    }
}

/// <summary>
/// Configuración simplificada para casos de uso comunes
/// </summary>
public static class CommonActivityEmailConfigurations
{
    /// <summary>
    /// Configuración para reportes manuales con máximo detalle
    /// </summary>
    public static ActivityEmailIntegration ManualDetailed => new()
    {
        IncludeInManualReports = true,
        AttachDetailedReport = true,
        EmbedSummaryInEmail = true,
        PreferredFormats = new List<string> { "HTML", "CSV" }
    };

    /// <summary>
    /// Configuración para reportes programados (conservadora)
    /// </summary>
    public static ActivityEmailIntegration ScheduledConservative => new()
    {
        IncludeInScheduledReports = true,
        AttachDetailedReport = true,
        EmbedSummaryInEmail = false,
        PreferredFormats = new List<string> { "CSV" }
    };

    /// <summary>
    /// Configuración empresarial con todos los formatos
    /// </summary>
    public static ActivityEmailIntegration Enterprise => new()
    {
        AttachDetailedReport = true,
        EmbedSummaryInEmail = true,
        PreferredFormats = new List<string> { "HTML", "CSV", "JSON" }
    };

    /// <summary>
    /// Configuración minimalista (solo datos esenciales)
    /// </summary>
    public static ActivityEmailIntegration Minimal => new()
    {
        AttachDetailedReport = false,
        EmbedSummaryInEmail = true,
        PreferredFormats = new List<string> { "HTML" }
    };
}