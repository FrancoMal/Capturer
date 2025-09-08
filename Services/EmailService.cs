using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.IO.Compression;
using System.Text;
using Capturer.Models;

namespace Capturer.Services;

public interface IEmailService
{
    Task<bool> SendWeeklyReportAsync(List<string> recipients, DateTime startDate, DateTime endDate);
    Task<bool> SendManualReportAsync(List<string> recipients, DateTime startDate, DateTime endDate, bool useZipFormat = true);
    Task<bool> SendEnhancedReportAsync(List<string> recipients, DateTime startDate, DateTime endDate, List<string> screenshots, string reportType, bool useZipFormat = true);
    Task<bool> SendUnifiedReportAsync(List<string> recipients, ReportPeriod period, List<string> baseScreenshots, string reportType, bool useZipFormat = true);
    Task<bool> SendQuadrantReportAsync(List<string> recipients, DateTime startDate, DateTime endDate, List<string> selectedQuadrants, bool useZipFormat = true);
    Task<bool> SendRoutineQuadrantReportsAsync(List<string> recipients, DateTime startDate, DateTime endDate, List<string> selectedQuadrants, bool useZipFormat = true, bool separateEmailPerQuadrant = false);
    Task<bool> SendActivityDashboardReportAsync(List<string> recipients, string subject, string body, List<string> attachmentFiles, bool useZipFormat = true);
    
    // Activity report methods (consolidated from EmailActivityReportExtension)
    Task<bool> SendActivityReportAsync(ActivityReport report, List<string> recipients, ActivityEmailIntegration config);
    Task<bool> SendManualReportWithActivityAsync(List<string> recipients, DateTime startDate, DateTime endDate, ActivityReport? activityReport = null, bool includeActivityData = false);
    Task<bool> SendScheduledReportWithActivityAsync(List<string> recipients, DateTime startDate, DateTime endDate, List<string> screenshots, ActivityReport? activityReport = null, ActivityEmailIntegration? config = null);
    
    Task<bool> ValidateEmailConfigAsync();
    Task<bool> TestConnectionAsync();
    List<string> GetAvailableQuadrantFolders();
    event EventHandler<EmailSentEventArgs>? EmailSent;
}

public class EmailService : IEmailService
{
    private readonly IConfigurationManager _configManager;
    private readonly IFileService _fileService;
    private readonly IQuadrantService? _quadrantService;
    private readonly ActivityReportService? _activityReportService;
    private CapturerConfiguration _config = new();
    private const long MaxAttachmentSize = 25 * 1024 * 1024; // 25MB

    public event EventHandler<EmailSentEventArgs>? EmailSent;

    public EmailService(IConfigurationManager configManager, IFileService fileService, IQuadrantService? quadrantService = null, ActivityReportService? activityReportService = null)
    {
        _configManager = configManager;
        _fileService = fileService;
        _quadrantService = quadrantService;
        _activityReportService = activityReportService;
        // Initialize with default configuration
        _config = new CapturerConfiguration();
        // Load configuration asynchronously in a fire-and-forget manner
        _ = Task.Run(async () => await LoadConfigurationAsync());
    }

    private async Task LoadConfigurationAsync()
    {
        try
        {
            _config = await _configManager.LoadConfigurationAsync();
            Console.WriteLine($"Email configuration loaded. SMTP: {_config.Email.SmtpServer}, Recipients: {_config.Email.Recipients.Count}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading email configuration: {ex.Message}");
            _config = new CapturerConfiguration();
        }
    }

    public async Task<bool> SendWeeklyReportAsync(List<string> recipients, DateTime startDate, DateTime endDate)
    {
        return await SendReportAsync(recipients, startDate, endDate, "Reporte Semanal Capturer", true);
    }

    public async Task<bool> SendManualReportAsync(List<string> recipients, DateTime startDate, DateTime endDate, bool useZipFormat = true)
    {
        return await SendReportAsync(recipients, startDate, endDate, "Reporte Manual Capturer", false, useZipFormat);
    }

    public async Task<bool> SendEnhancedReportAsync(List<string> recipients, DateTime startDate, DateTime endDate, List<string> screenshots, string reportType, bool useZipFormat = true)
    {
        try
        {
            await LoadConfigurationAsync();

            if (!screenshots.Any())
            {
                // Send empty report
                return await SendEmptyReportAsync(recipients, startDate, endDate, reportType);
            }

            // Create and send email
            var message = CreateEmailMessage(recipients, startDate, endDate, screenshots.Count, reportType, false);
            var multipart = new Multipart("mixed");
            multipart.Add(message.Body);
            
            var tempFiles = new List<string>();
            
            if (useZipFormat)
            {
                // Prepare ZIP attachment with provided screenshots
                var attachmentPath = await PrepareCustomAttachmentsAsync(screenshots, startDate, endDate, reportType);
                if (!string.IsNullOrEmpty(attachmentPath))
                {
                    // Read ZIP file content into memory to avoid file handle issues
                    var zipBytes = await File.ReadAllBytesAsync(attachmentPath);
                    var attachment = new MimePart("application", "zip")
                    {
                        Content = new MimeContent(new MemoryStream(zipBytes)),
                        ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                        ContentTransferEncoding = ContentEncoding.Base64,
                        FileName = Path.GetFileName(attachmentPath)
                    };
                    
                    multipart.Add(attachment);
                    tempFiles.Add(attachmentPath);
                }
            }
            else
            {
                // Add individual files as attachments
                long totalSize = 0;
                foreach (var screenshotPath in screenshots)
                {
                    if (!File.Exists(screenshotPath)) continue;
                    
                    var fileInfo = new FileInfo(screenshotPath);
                    if (totalSize + fileInfo.Length > MaxAttachmentSize)
                    {
                        break; // Stop adding files if we exceed the limit
                    }
                    
                    // Read file content into memory to avoid file handle issues
                    var fileBytes = await File.ReadAllBytesAsync(screenshotPath);
                    var attachment = new MimePart("image", "png")
                    {
                        Content = new MimeContent(new MemoryStream(fileBytes)),
                        ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                        ContentTransferEncoding = ContentEncoding.Base64,
                        FileName = Path.GetFileName(screenshotPath)
                    };
                    
                    multipart.Add(attachment);
                    totalSize += fileInfo.Length;
                }
            }
            
            message.Body = multipart;

            var success = await SendEmailAsync(message);

            // Cleanup temp files
            foreach (var tempFile in tempFiles)
            {
                if (File.Exists(tempFile))
                {
                    try { File.Delete(tempFile); } catch { /* Ignore cleanup errors */ }
                }
            }

            // Fire event
            EmailSent?.Invoke(this, new EmailSentEventArgs
            {
                Recipients = recipients,
                AttachmentCount = screenshots.Count,
                SentDate = DateTime.Now,
                Success = success
            });

            return success;
        }
        catch (Exception ex)
        {
            EmailSent?.Invoke(this, new EmailSentEventArgs
            {
                Recipients = recipients,
                Success = false,
                ErrorMessage = ex.Message,
                SentDate = DateTime.Now
            });
            return false;
        }
    }

    private async Task<bool> SendReportAsync(List<string> recipients, DateTime startDate, DateTime endDate, string subjectPrefix, bool isWeekly, bool useZipFormat = true)
    {
        try
        {
            await LoadConfigurationAsync();

            // Get screenshots for date range
            var screenshots = await _fileService.GetScreenshotsByDateRangeAsync(startDate, endDate);
            if (!screenshots.Any())
            {
                // Send empty report
                return await SendEmptyReportAsync(recipients, startDate, endDate, subjectPrefix);
            }

            // Create and send email
            var message = CreateEmailMessage(recipients, startDate, endDate, screenshots.Count, subjectPrefix, isWeekly);
            var multipart = new Multipart("mixed");
            multipart.Add(message.Body);
            
            var tempFiles = new List<string>();
            
            if (useZipFormat)
            {
                // Prepare ZIP attachment
                var attachmentPath = await PrepareAttachmentsAsync(screenshots, startDate, endDate);
                if (!string.IsNullOrEmpty(attachmentPath))
                {
                    // Read ZIP file content into memory to avoid file handle issues
                    var zipBytes = await File.ReadAllBytesAsync(attachmentPath);
                    var attachment = new MimePart("application", "zip")
                    {
                        Content = new MimeContent(new MemoryStream(zipBytes)),
                        ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                        ContentTransferEncoding = ContentEncoding.Base64,
                        FileName = Path.GetFileName(attachmentPath)
                    };
                    
                    multipart.Add(attachment);
                    tempFiles.Add(attachmentPath);
                }
            }
            else
            {
                // Add individual files as attachments
                long totalSize = 0;
                foreach (var screenshotPath in screenshots)
                {
                    if (!File.Exists(screenshotPath)) continue;
                    
                    var fileInfo = new FileInfo(screenshotPath);
                    if (totalSize + fileInfo.Length > MaxAttachmentSize)
                    {
                        break; // Stop adding files if we exceed the limit
                    }
                    
                    // Read file content into memory to avoid file handle issues
                    var fileBytes = await File.ReadAllBytesAsync(screenshotPath);
                    var attachment = new MimePart("image", "png")
                    {
                        Content = new MimeContent(new MemoryStream(fileBytes)),
                        ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                        ContentTransferEncoding = ContentEncoding.Base64,
                        FileName = Path.GetFileName(screenshotPath)
                    };
                    
                    multipart.Add(attachment);
                    totalSize += fileInfo.Length;
                }
            }
            
            message.Body = multipart;

            var success = await SendEmailAsync(message);

            // Cleanup temp files
            foreach (var tempFile in tempFiles)
            {
                if (File.Exists(tempFile))
                {
                    try { File.Delete(tempFile); } catch { /* Ignore cleanup errors */ }
                }
            }

            // Fire event
            EmailSent?.Invoke(this, new EmailSentEventArgs
            {
                Recipients = recipients,
                AttachmentCount = screenshots.Count,
                SentDate = DateTime.Now,
                Success = success
            });

            return success;
        }
        catch (Exception ex)
        {
            EmailSent?.Invoke(this, new EmailSentEventArgs
            {
                Recipients = recipients,
                Success = false,
                ErrorMessage = ex.Message,
                SentDate = DateTime.Now
            });
            return false;
        }
    }

    private async Task<bool> SendEmptyReportAsync(List<string> recipients, DateTime startDate, DateTime endDate, string subjectPrefix)
    {
        var message = CreateEmailMessage(recipients, startDate, endDate, 0, subjectPrefix, false);
        return await SendEmailAsync(message);
    }

    private MimeMessage CreateEmailMessage(List<string> recipients, DateTime startDate, DateTime endDate, int screenshotCount, string subjectPrefix, bool isWeekly)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_config.Email.SenderName, _config.Email.Username));
        
        foreach (var recipient in recipients)
        {
            message.To.Add(MailboxAddress.Parse(recipient));
        }

        var dateRange = $"{startDate:yyyy-MM-dd} a {endDate:yyyy-MM-dd}";
        message.Subject = $"{subjectPrefix} - {dateRange}";

        var bodyBuilder = new BodyBuilder();
        bodyBuilder.HtmlBody = CreateEmailHtml(startDate, endDate, screenshotCount, isWeekly);
        bodyBuilder.TextBody = CreateEmailText(startDate, endDate, screenshotCount, isWeekly);
        
        message.Body = bodyBuilder.ToMessageBody();
        return message;
    }

    private string CreateEmailHtml(DateTime startDate, DateTime endDate, int count, bool isWeekly)
    {
        var reportType = isWeekly ? "semanal" : "manual";
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Reporte Capturer</title>
</head>
<body style='font-family: Arial, sans-serif; margin: 20px;'>
    <h2 style='color: #2c3e50;'>📸 Reporte {reportType.ToUpper()} - Capturer</h2>
    
    <table style='border-collapse: collapse; width: 100%; margin: 20px 0;'>
        <tr style='background-color: #f8f9fa;'>
            <td style='padding: 10px; border: 1px solid #dee2e6; font-weight: bold;'>Período del reporte:</td>
            <td style='padding: 10px; border: 1px solid #dee2e6;'>{startDate:yyyy-MM-dd} hasta {endDate:yyyy-MM-dd}</td>
        </tr>
        <tr>
            <td style='padding: 10px; border: 1px solid #dee2e6; font-weight: bold;'>Total de capturas:</td>
            <td style='padding: 10px; border: 1px solid #dee2e6;'>{count}</td>
        </tr>
        <tr style='background-color: #f8f9fa;'>
            <td style='padding: 10px; border: 1px solid #dee2e6; font-weight: bold;'>Computadora:</td>
            <td style='padding: 10px; border: 1px solid #dee2e6;'>{Environment.MachineName}</td>
        </tr>
        <tr>
            <td style='padding: 10px; border: 1px solid #dee2e6; font-weight: bold;'>Generado:</td>
            <td style='padding: 10px; border: 1px solid #dee2e6;'>{DateTime.Now:yyyy-MM-dd HH:mm:ss}</td>
        </tr>
    </table>
    
    {(count > 0 ? "<p>📎 Las capturas de pantalla están adjuntas en formato ZIP.</p>" : "<p>⚠️ No se encontraron capturas para el período especificado.</p>")}
    
    <hr style='margin: 30px 0; border: none; border-top: 1px solid #dee2e6;'>
    <p style='color: #6c757d; font-size: 12px;'>
        Este reporte fue generado automáticamente por Capturer.<br>
        Si tienes alguna pregunta, contacta al administrador del sistema.
    </p>
</body>
</html>";
    }

    private string CreateEmailText(DateTime startDate, DateTime endDate, int count, bool isWeekly)
    {
        var reportType = isWeekly ? "SEMANAL" : "MANUAL";
        return $@"
REPORTE {reportType} - CAPTURER
===============================

Período del reporte: {startDate:yyyy-MM-dd} hasta {endDate:yyyy-MM-dd}
Total de capturas: {count}
Computadora: {Environment.MachineName}
Generado: {DateTime.Now:yyyy-MM-dd HH:mm:ss}

{(count > 0 ? "Las capturas de pantalla están adjuntas en formato ZIP." : "No se encontraron capturas para el período especificado.")}

---
Este reporte fue generado automáticamente por Capturer.
Si tienes alguna pregunta, contacta al administrador del sistema.
";
    }

    private async Task<string?> PrepareAttachmentsAsync(List<string> screenshots, DateTime startDate, DateTime endDate)
    {
        if (!screenshots.Any()) return null;

        var tempPath = Path.GetTempPath();
        var zipFileName = $"capturas_{startDate:yyyyMMdd}-{endDate:yyyyMMdd}_{DateTime.Now:HHmmss}.zip";
        var zipPath = Path.Combine(tempPath, zipFileName);

        try
        {
            using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
            
            long totalSize = 0;
            var addedFiles = new List<string>();

            foreach (var screenshotPath in screenshots)
            {
                if (!File.Exists(screenshotPath)) continue;

                var fileInfo = new FileInfo(screenshotPath);
                
                // Check size limit
                if (totalSize + fileInfo.Length > MaxAttachmentSize)
                {
                    break; // Stop adding files if we exceed the limit
                }

                archive.CreateEntryFromFile(screenshotPath, Path.GetFileName(screenshotPath));
                totalSize += fileInfo.Length;
                addedFiles.Add(screenshotPath);
            }

            if (!addedFiles.Any())
            {
                File.Delete(zipPath);
                return null;
            }

            return zipPath;
        }
        catch (Exception ex)
        {
            if (File.Exists(zipPath))
                File.Delete(zipPath);
            
            Console.WriteLine($"Error creating ZIP: {ex.Message}");
            return null;
        }
    }

    private async Task<bool> SendEmailAsync(MimeMessage message)
    {
        try
        {
            using var client = new SmtpClient();
            
            Console.WriteLine($"Attempting to connect to SMTP server: {_config.Email.SmtpServer}:{_config.Email.SmtpPort}");
            Console.WriteLine($"Security mode: {_config.Email.SecurityMode}");
            
            // Configure security based on mode
            var securityOptions = _config.Email.SecurityMode switch
            {
                Models.SmtpSecurityMode.None => SecureSocketOptions.None,
                Models.SmtpSecurityMode.StartTls => SecureSocketOptions.StartTls,
                Models.SmtpSecurityMode.Ssl => SecureSocketOptions.SslOnConnect,
                Models.SmtpSecurityMode.Auto => SecureSocketOptions.Auto,
                _ => SecureSocketOptions.StartTls
            };
            
            // Set timeout
            client.Timeout = _config.Email.ConnectionTimeout * 1000;
            
            await client.ConnectAsync(_config.Email.SmtpServer, _config.Email.SmtpPort, securityOptions);

            if (!string.IsNullOrEmpty(_config.Email.Username))
            {
                Console.WriteLine($"Authenticating with username: {_config.Email.Username}");
                await client.AuthenticateAsync(_config.Email.Username, _config.Email.Password);
            }

            Console.WriteLine($"Sending email to {message.To.Count} recipients...");
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
            
            Console.WriteLine("Email sent successfully!");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending email: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> ValidateEmailConfigAsync()
    {
        await LoadConfigurationAsync();
        
        if (string.IsNullOrEmpty(_config.Email.SmtpServer) ||
            string.IsNullOrEmpty(_config.Email.Username) ||
            string.IsNullOrEmpty(_config.Email.Password) ||
            !_config.Email.Recipients.Any())
        {
            return false;
        }

        return await TestConnectionAsync();
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            await LoadConfigurationAsync();

            using var client = new SmtpClient();
            
            var securityOptions = _config.Email.SecurityMode switch
            {
                Models.SmtpSecurityMode.None => SecureSocketOptions.None,
                Models.SmtpSecurityMode.StartTls => SecureSocketOptions.StartTls,
                Models.SmtpSecurityMode.Ssl => SecureSocketOptions.SslOnConnect,
                Models.SmtpSecurityMode.Auto => SecureSocketOptions.Auto,
                _ => SecureSocketOptions.StartTls
            };
            
            client.Timeout = _config.Email.ConnectionTimeout * 1000;
            
            await client.ConnectAsync(_config.Email.SmtpServer, _config.Email.SmtpPort, securityOptions);

            if (!string.IsNullOrEmpty(_config.Email.Username))
            {
                await client.AuthenticateAsync(_config.Email.Username, _config.Email.Password);
            }

            await client.DisconnectAsync(true);
            Console.WriteLine("SMTP connection test successful!");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Email connection test failed: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> SendQuadrantReportAsync(List<string> recipients, DateTime startDate, DateTime endDate, List<string> selectedQuadrants, bool useZipFormat = true)
    {
        try
        {
            await LoadConfigurationAsync();

            if (!recipients.Any())
            {
                Console.WriteLine("No recipients specified for quadrant report");
                return false;
            }

            if (!selectedQuadrants.Any())
            {
                Console.WriteLine("No quadrants selected for report");
                return false;
            }

            // Get files from selected quadrants
            var quadrantFiles = new List<string>();
            if (_quadrantService != null)
            {
                quadrantFiles = await _quadrantService.GetQuadrantFilesAsync(selectedQuadrants, startDate, endDate);
            }
            else
            {
                // Fallback: get files directly from quadrant folders
                quadrantFiles = await GetQuadrantFilesDirectAsync(selectedQuadrants, startDate, endDate);
            }

            // Create email message
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_config.Email.SenderName, _config.Email.Username));
            
            foreach (var recipient in recipients)
            {
                message.To.Add(new MailboxAddress("", recipient));
            }

            var quadrantList = string.Join(", ", selectedQuadrants);
            message.Subject = $"Reporte de Cuadrantes - {startDate:yyyy-MM-dd} a {endDate:yyyy-MM-dd}";

            // Create email body
            var bodyBuilder = new BodyBuilder();
            bodyBuilder.HtmlBody = CreateQuadrantEmailHtml(startDate, endDate, quadrantFiles.Count, quadrantList);
            bodyBuilder.TextBody = CreateQuadrantEmailText(startDate, endDate, quadrantFiles.Count, quadrantList);

            // Add attachments
            if (quadrantFiles.Any() && useZipFormat)
            {
                var zipPath = await PrepareQuadrantAttachmentsAsync(quadrantFiles, selectedQuadrants, startDate, endDate);
                if (zipPath != null)
                {
                    bodyBuilder.Attachments.Add(zipPath);
                    
                    // Schedule cleanup
                    _ = Task.Delay(TimeSpan.FromMinutes(10)).ContinueWith(_ =>
                    {
                        try { File.Delete(zipPath); } catch { }
                    });
                }
            }
            else if (quadrantFiles.Any())
            {
                // Add individual files (up to 10 to avoid too many attachments)
                var filesToAttach = quadrantFiles.Take(10).ToList();
                foreach (var file in filesToAttach)
                {
                    if (File.Exists(file))
                    {
                        bodyBuilder.Attachments.Add(file);
                    }
                }
            }

            message.Body = bodyBuilder.ToMessageBody();

            // Send email
            var success = await SendEmailAsync(message);
            
            // Fire event
            EmailSent?.Invoke(this, new EmailSentEventArgs
            {
                Recipients = recipients,
                Success = success,
                FileCount = quadrantFiles.Count,
                ErrorMessage = success ? null : "Error enviando reporte de cuadrantes"
            });

            return success;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending quadrant report: {ex.Message}");
            
            EmailSent?.Invoke(this, new EmailSentEventArgs
            {
                Recipients = recipients,
                Success = false,
                FileCount = 0,
                ErrorMessage = ex.Message
            });
            
            return false;
        }
    }
    
    public async Task<bool> SendRoutineQuadrantReportsAsync(List<string> recipients, DateTime startDate, DateTime endDate, List<string> selectedQuadrants, bool useZipFormat = true, bool separateEmailPerQuadrant = false)
    {
        try
        {
            await LoadConfigurationAsync();

            if (!recipients.Any())
            {
                Console.WriteLine("No recipients specified for routine quadrant reports");
                return false;
            }

            if (!selectedQuadrants.Any())
            {
                Console.WriteLine("No quadrants selected for routine reports");
                return false;
            }

            bool overallSuccess = true;
            
            if (separateEmailPerQuadrant)
            {
                // Send separate email for each quadrant
                foreach (var quadrant in selectedQuadrants)
                {
                    try
                    {
                        var success = await SendSingleQuadrantReportAsync(recipients, startDate, endDate, quadrant, useZipFormat, "Reporte Automático de Cuadrante");
                        if (!success)
                        {
                            Console.WriteLine($"Failed to send routine report for quadrant: {quadrant}");
                            overallSuccess = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending routine report for quadrant {quadrant}: {ex.Message}");
                        overallSuccess = false;
                    }
                }
            }
            else
            {
                // Send combined email with all quadrants
                overallSuccess = await SendCombinedQuadrantReportAsync(recipients, startDate, endDate, selectedQuadrants, useZipFormat, "Reporte Automático de Cuadrantes");
            }
            
            // Fire overall event
            EmailSent?.Invoke(this, new EmailSentEventArgs
            {
                Recipients = recipients,
                Success = overallSuccess,
                FileCount = separateEmailPerQuadrant ? selectedQuadrants.Count : 1, // Number of emails sent
                ErrorMessage = overallSuccess ? null : "Error en uno o más emails de cuadrantes",
                SentDate = DateTime.Now
            });

            return overallSuccess;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in SendRoutineQuadrantReportsAsync: {ex.Message}");
            
            EmailSent?.Invoke(this, new EmailSentEventArgs
            {
                Recipients = recipients,
                Success = false,
                FileCount = 0,
                ErrorMessage = ex.Message
            });
            
            return false;
        }
    }

    private async Task<bool> SendSingleQuadrantReportAsync(List<string> recipients, DateTime startDate, DateTime endDate, string quadrantName, bool useZipFormat, string subjectPrefix)
    {
        try
        {
            // Get files from the single quadrant
            var quadrantFiles = new List<string>();
            if (_quadrantService != null)
            {
                quadrantFiles = await _quadrantService.GetQuadrantFilesAsync(new List<string> { quadrantName }, startDate, endDate);
            }
            else
            {
                quadrantFiles = await GetQuadrantFilesDirectAsync(new List<string> { quadrantName }, startDate, endDate);
            }

            // Create email message
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_config.Email.SenderName, _config.Email.Username));
            
            foreach (var recipient in recipients)
            {
                message.To.Add(new MailboxAddress("", recipient));
            }

            message.Subject = $"{subjectPrefix} '{quadrantName}' - {startDate:yyyy-MM-dd} a {endDate:yyyy-MM-dd}";

            // Create email body
            var bodyBuilder = new BodyBuilder();
            bodyBuilder.HtmlBody = CreateSingleQuadrantEmailHtml(startDate, endDate, quadrantFiles.Count, quadrantName);
            bodyBuilder.TextBody = CreateSingleQuadrantEmailText(startDate, endDate, quadrantFiles.Count, quadrantName);

            // Add attachments
            if (quadrantFiles.Any() && useZipFormat)
            {
                var zipPath = await PrepareQuadrantAttachmentsAsync(quadrantFiles, new List<string> { quadrantName }, startDate, endDate);
                if (zipPath != null)
                {
                    bodyBuilder.Attachments.Add(zipPath);
                    
                    // Schedule cleanup
                    _ = Task.Delay(TimeSpan.FromMinutes(10)).ContinueWith(_ =>
                    {
                        try { File.Delete(zipPath); } catch { }
                    });
                }
            }
            else if (quadrantFiles.Any())
            {
                // Add individual files (up to 10 to avoid too many attachments)
                var filesToAttach = quadrantFiles.Take(10).ToList();
                foreach (var file in filesToAttach)
                {
                    if (File.Exists(file))
                    {
                        bodyBuilder.Attachments.Add(file);
                    }
                }
            }

            message.Body = bodyBuilder.ToMessageBody();

            // Send email
            return await SendEmailAsync(message);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending single quadrant report for '{quadrantName}': {ex.Message}");
            return false;
        }
    }
    
    private async Task<bool> SendCombinedQuadrantReportAsync(List<string> recipients, DateTime startDate, DateTime endDate, List<string> selectedQuadrants, bool useZipFormat, string subjectPrefix)
    {
        // This is essentially the same as the existing SendQuadrantReportAsync but with different subject
        try
        {
            // Get files from selected quadrants
            var quadrantFiles = new List<string>();
            if (_quadrantService != null)
            {
                quadrantFiles = await _quadrantService.GetQuadrantFilesAsync(selectedQuadrants, startDate, endDate);
            }
            else
            {
                quadrantFiles = await GetQuadrantFilesDirectAsync(selectedQuadrants, startDate, endDate);
            }

            // Create email message
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_config.Email.SenderName, _config.Email.Username));
            
            foreach (var recipient in recipients)
            {
                message.To.Add(new MailboxAddress("", recipient));
            }

            var quadrantList = string.Join(", ", selectedQuadrants);
            message.Subject = $"{subjectPrefix} - {startDate:yyyy-MM-dd} a {endDate:yyyy-MM-dd}";

            // Create email body
            var bodyBuilder = new BodyBuilder();
            bodyBuilder.HtmlBody = CreateQuadrantEmailHtml(startDate, endDate, quadrantFiles.Count, quadrantList);
            bodyBuilder.TextBody = CreateQuadrantEmailText(startDate, endDate, quadrantFiles.Count, quadrantList);

            // Add attachments
            if (quadrantFiles.Any() && useZipFormat)
            {
                var zipPath = await PrepareQuadrantAttachmentsAsync(quadrantFiles, selectedQuadrants, startDate, endDate);
                if (zipPath != null)
                {
                    bodyBuilder.Attachments.Add(zipPath);
                    
                    // Schedule cleanup
                    _ = Task.Delay(TimeSpan.FromMinutes(10)).ContinueWith(_ =>
                    {
                        try { File.Delete(zipPath); } catch { }
                    });
                }
            }
            else if (quadrantFiles.Any())
            {
                // Add individual files (up to 10 to avoid too many attachments)
                var filesToAttach = quadrantFiles.Take(10).ToList();
                foreach (var file in filesToAttach)
                {
                    if (File.Exists(file))
                    {
                        bodyBuilder.Attachments.Add(file);
                    }
                }
            }

            message.Body = bodyBuilder.ToMessageBody();

            // Send email
            return await SendEmailAsync(message);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending combined quadrant report: {ex.Message}");
            return false;
        }
    }
    
    public List<string> GetAvailableQuadrantFolders()
    {
        if (_quadrantService != null)
        {
            return _quadrantService.GetAvailableQuadrantFolders();
        }

        // Fallback: get folders directly
        try
        {
            var quadrantsFolder = Path.Combine(_config.Storage.ScreenshotFolder, "..", "Quadrants");
            if (Directory.Exists(quadrantsFolder))
            {
                return Directory.GetDirectories(quadrantsFolder)
                    .Select(Path.GetFileName)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .ToList()!;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting quadrant folders: {ex.Message}");
        }

        return new List<string>();
    }

    public async Task<bool> SendUnifiedReportAsync(List<string> recipients, ReportPeriod period, List<string> baseScreenshots, string reportType, bool useZipFormat = true)
    {
        try
        {
            await LoadConfigurationAsync();

            if (!baseScreenshots.Any())
            {
                // Send empty report
                return await SendEmptyReportAsync(recipients, period.StartDate, period.EndDate, reportType);
            }

            bool success;
            var config = _config;
            
            Console.WriteLine($"Unified report: {baseScreenshots.Count} base screenshots filtered by period");
            
            // Check if quadrant system is enabled for routine emails
            if (config.Email.QuadrantSettings.UseQuadrantsInRoutineEmails && 
                config.Email.QuadrantSettings.SelectedQuadrants.Any() &&
                _quadrantService != null)
            {
                Console.WriteLine($"Processing quadrants from filtered screenshots for {config.Email.QuadrantSettings.SelectedQuadrants.Count} quadrants");
                
                // Process quadrants from the filtered screenshots first
                if (config.Email.QuadrantSettings.ProcessQuadrantsFirst)
                {
                    success = await ProcessAndSendQuadrantReportAsync(
                        recipients, 
                        baseScreenshots, 
                        period,
                        config.Email.QuadrantSettings.SelectedQuadrants,
                        reportType,
                        useZipFormat,
                        config.Email.QuadrantSettings.SendSeparateEmailPerQuadrant);
                }
                else
                {
                    // Send both regular screenshots AND quadrant files
                    success = await SendCombinedScreenshotAndQuadrantReportAsync(
                        recipients,
                        baseScreenshots,
                        period,
                        config.Email.QuadrantSettings.SelectedQuadrants,
                        reportType,
                        useZipFormat);
                }
            }
            else
            {
                // Standard enhanced report with filtered screenshots
                success = await SendEnhancedReportAsync(
                    recipients, 
                    period.StartDate, 
                    period.EndDate, 
                    baseScreenshots,
                    reportType,
                    useZipFormat);
            }

            return success;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending unified report: {ex.Message}");
            
            EmailSent?.Invoke(this, new EmailSentEventArgs
            {
                Recipients = recipients,
                Success = false,
                FileCount = 0,
                ErrorMessage = ex.Message
            });
            
            return false;
        }
    }

    private async Task<bool> ProcessAndSendQuadrantReportAsync(
        List<string> recipients, 
        List<string> baseScreenshots, 
        ReportPeriod period,
        List<string> selectedQuadrants,
        string reportType,
        bool useZipFormat,
        bool separateEmailPerQuadrant)
    {
        try
        {
            if (_quadrantService == null)
            {
                Console.WriteLine("QuadrantService not available, falling back to regular report");
                return await SendEnhancedReportAsync(recipients, period.StartDate, period.EndDate, baseScreenshots, reportType, useZipFormat);
            }

            // Process the filtered screenshots through quadrant system
            Console.WriteLine($"Processing {baseScreenshots.Count} filtered screenshots through quadrant system");
            
            var processingTask = await _quadrantService.ProcessSpecificImagesAsync(
                baseScreenshots, 
                null, // Use active configuration
                null  // No progress reporting for background task
            );

            if (processingTask.Status != ProcessingStatus.Completed)
            {
                Console.WriteLine($"Quadrant processing failed: {string.Join("; ", processingTask.ErrorMessages)}");
                // Fallback to regular screenshots
                return await SendEnhancedReportAsync(recipients, period.StartDate, period.EndDate, baseScreenshots, reportType, useZipFormat);
            }

            Console.WriteLine($"Quadrant processing completed successfully: {processingTask.ProcessedFiles} files processed");

            // Now get the quadrant files that were processed from our filtered screenshots
            var quadrantFiles = await _quadrantService.GetQuadrantFilesAsync(selectedQuadrants, period.StartDate, period.EndDate);
            
            if (separateEmailPerQuadrant)
            {
                // Send separate email for each quadrant
                bool overallSuccess = true;
                foreach (var quadrant in selectedQuadrants)
                {
                    var quadrantSpecificFiles = await _quadrantService.GetQuadrantFilesAsync(new List<string> { quadrant }, period.StartDate, period.EndDate);
                    
                    var success = await SendEnhancedReportAsync(
                        recipients,
                        period.StartDate,
                        period.EndDate,
                        quadrantSpecificFiles,
                        $"{reportType} - Cuadrante '{quadrant}'",
                        useZipFormat);
                        
                    if (!success) overallSuccess = false;
                }
                return overallSuccess;
            }
            else
            {
                // Send combined quadrant report
                return await SendEnhancedReportAsync(
                    recipients,
                    period.StartDate,
                    period.EndDate,
                    quadrantFiles,
                    $"{reportType} - Cuadrantes",
                    useZipFormat);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in ProcessAndSendQuadrantReportAsync: {ex.Message}");
            // Fallback to regular screenshots
            return await SendEnhancedReportAsync(recipients, period.StartDate, period.EndDate, baseScreenshots, reportType, useZipFormat);
        }
    }

    private async Task<bool> SendCombinedScreenshotAndQuadrantReportAsync(
        List<string> recipients,
        List<string> baseScreenshots,
        ReportPeriod period,
        List<string> selectedQuadrants,
        string reportType,
        bool useZipFormat)
    {
        try
        {
            // Get existing quadrant files (if any) that match the period
            var quadrantFiles = new List<string>();
            if (_quadrantService != null)
            {
                quadrantFiles = await _quadrantService.GetQuadrantFilesAsync(selectedQuadrants, period.StartDate, period.EndDate);
            }

            // Combine both regular screenshots and quadrant files
            var allFiles = new List<string>();
            allFiles.AddRange(baseScreenshots);
            allFiles.AddRange(quadrantFiles);

            Console.WriteLine($"Combined report: {baseScreenshots.Count} screenshots + {quadrantFiles.Count} quadrant files = {allFiles.Count} total");

            return await SendEnhancedReportAsync(
                recipients,
                period.StartDate,
                period.EndDate,
                allFiles.Distinct().ToList(), // Remove duplicates
                $"{reportType} - Completo con Cuadrantes",
                useZipFormat);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in SendCombinedScreenshotAndQuadrantReportAsync: {ex.Message}");
            // Fallback to regular screenshots only
            return await SendEnhancedReportAsync(recipients, period.StartDate, period.EndDate, baseScreenshots, reportType, useZipFormat);
        }
    }

    private async Task<string?> PrepareCustomAttachmentsAsync(List<string> screenshots, DateTime startDate, DateTime endDate, string reportType)
    {
        if (!screenshots.Any()) return null;

        var tempPath = Path.GetTempPath();
        var reportTypeShort = reportType.Replace("Reporte ", "").Replace(" Capturer", "");
        var zipFileName = $"{reportTypeShort}_{startDate:yyyyMMdd}-{endDate:yyyyMMdd}_{DateTime.Now:HHmmss}.zip";
        var zipPath = Path.Combine(tempPath, zipFileName);

        try
        {
            using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
            
            long totalSize = 0;
            var addedFiles = new List<string>();

            foreach (var screenshotPath in screenshots)
            {
                if (!File.Exists(screenshotPath)) continue;

                var fileInfo = new FileInfo(screenshotPath);
                
                // Check size limit
                if (totalSize + fileInfo.Length > MaxAttachmentSize)
                {
                    break; // Stop adding files if we exceed the limit
                }

                archive.CreateEntryFromFile(screenshotPath, Path.GetFileName(screenshotPath));
                totalSize += fileInfo.Length;
                addedFiles.Add(screenshotPath);
            }

            if (!addedFiles.Any())
            {
                File.Delete(zipPath);
                return null;
            }

            Console.WriteLine($"Created custom report ZIP with {addedFiles.Count} filtered screenshots");
            return zipPath;
        }
        catch (Exception ex)
        {
            if (File.Exists(zipPath))
                File.Delete(zipPath);
            
            Console.WriteLine($"Error creating custom report ZIP: {ex.Message}");
            return null;
        }
    }

    private async Task<List<string>> GetQuadrantFilesDirectAsync(List<string> quadrantNames, DateTime startDate, DateTime endDate)
    {
        var files = new List<string>();
        
        try
        {
            var quadrantsBaseFolder = Path.Combine(_config.Storage.ScreenshotFolder, "..", "Quadrants");
            
            if (!Directory.Exists(quadrantsBaseFolder))
                return files;

            await Task.Run(() =>
            {
                foreach (var quadrantName in quadrantNames)
                {
                    var quadrantFolder = Path.Combine(quadrantsBaseFolder, quadrantName);
                    if (!Directory.Exists(quadrantFolder)) continue;

                    var quadrantFiles = Directory.GetFiles(quadrantFolder, "*.png")
                        .Concat(Directory.GetFiles(quadrantFolder, "*.jpg"))
                        .Concat(Directory.GetFiles(quadrantFolder, "*.jpeg"))
                        .Where(file =>
                        {
                            var fileInfo = new FileInfo(file);
                            return fileInfo.CreationTime >= startDate && fileInfo.CreationTime <= endDate;
                        });

                    files.AddRange(quadrantFiles);
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting quadrant files: {ex.Message}");
        }

        return files.OrderByDescending(f => new FileInfo(f).CreationTime).ToList();
    }

    private async Task<string?> PrepareQuadrantAttachmentsAsync(List<string> files, List<string> quadrantNames, DateTime startDate, DateTime endDate)
    {
        if (!files.Any()) return null;

        var tempPath = Path.GetTempPath();
        var quadrantList = string.Join("-", quadrantNames.Take(3)); // Limit for filename length
        var zipFileName = $"cuadrantes_{quadrantList}_{startDate:yyyyMMdd}-{endDate:yyyyMMdd}_{DateTime.Now:HHmmss}.zip";
        var zipPath = Path.Combine(tempPath, zipFileName);

        try
        {
            using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
            
            long totalSize = 0;
            var addedFiles = new Dictionary<string, int>(); // Track files per quadrant

            foreach (var filePath in files)
            {
                if (!File.Exists(filePath)) continue;

                var fileInfo = new FileInfo(filePath);
                
                // Check size limit
                if (totalSize + fileInfo.Length > MaxAttachmentSize)
                {
                    break;
                }

                // Organize files by quadrant in ZIP
                var fileName = Path.GetFileName(filePath);
                var quadrantName = Path.GetFileName(Path.GetDirectoryName(filePath)) ?? "Unknown";
                var entryName = $"{quadrantName}/{fileName}";

                // Track count per quadrant
                if (!addedFiles.ContainsKey(quadrantName))
                    addedFiles[quadrantName] = 0;
                addedFiles[quadrantName]++;

                archive.CreateEntryFromFile(filePath, entryName);
                totalSize += fileInfo.Length;
            }

            if (!addedFiles.Any())
            {
                File.Delete(zipPath);
                return null;
            }

            Console.WriteLine($"Created quadrant ZIP with {addedFiles.Sum(kvp => kvp.Value)} files from {addedFiles.Count} quadrants");
            return zipPath;
        }
        catch (Exception ex)
        {
            if (File.Exists(zipPath))
                File.Delete(zipPath);
            
            Console.WriteLine($"Error creating quadrant ZIP: {ex.Message}");
            return null;
        }
    }

    private string CreateQuadrantEmailHtml(DateTime startDate, DateTime endDate, int count, string quadrantList)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Reporte de Cuadrantes - Capturer</title>
</head>
<body style='font-family: Arial, sans-serif; margin: 20px;'>
    <h2 style='color: #2c3e50;'>📊 Reporte de Cuadrantes - Capturer</h2>
    
    <table style='border-collapse: collapse; width: 100%; margin: 20px 0;'>
        <tr style='background-color: #f8f9fa;'>
            <td style='padding: 10px; border: 1px solid #dee2e6; font-weight: bold;'>Período del reporte:</td>
            <td style='padding: 10px; border: 1px solid #dee2e6;'>{startDate:yyyy-MM-dd} hasta {endDate:yyyy-MM-dd}</td>
        </tr>
        <tr>
            <td style='padding: 10px; border: 1px solid #dee2e6; font-weight: bold;'>Cuadrantes incluidos:</td>
            <td style='padding: 10px; border: 1px solid #dee2e6;'><strong>{quadrantList}</strong></td>
        </tr>
        <tr style='background-color: #f8f9fa;'>
            <td style='padding: 10px; border: 1px solid #dee2e6; font-weight: bold;'>Total de archivos:</td>
            <td style='padding: 10px; border: 1px solid #dee2e6;'>{count}</td>
        </tr>
        <tr>
            <td style='padding: 10px; border: 1px solid #dee2e6; font-weight: bold;'>Computadora:</td>
            <td style='padding: 10px; border: 1px solid #dee2e6;'>{Environment.MachineName}</td>
        </tr>
        <tr style='background-color: #f8f9fa;'>
            <td style='padding: 10px; border: 1px solid #dee2e6; font-weight: bold;'>Generado:</td>
            <td style='padding: 10px; border: 1px solid #dee2e6;'>{DateTime.Now:yyyy-MM-dd HH:mm:ss}</td>
        </tr>
    </table>
    
    {(count > 0 ? "<p>📎 Los archivos de los cuadrantes seleccionados están organizados en el archivo ZIP adjunto.</p>" : "<p>⚠️ No se encontraron archivos de cuadrantes para el período especificado.</p>")}
    
    <div style='background-color: #e8f4fd; padding: 15px; margin: 20px 0; border-left: 4px solid #007bff;'>
        <p style='margin: 0; color: #0056b3;'><strong>💡 Acerca de los Cuadrantes:</strong></p>
        <p style='margin: 5px 0 0 0; color: #0056b3;'>
            Los cuadrantes son secciones específicas de las capturas de pantalla que se procesan automáticamente 
            para facilitar el análisis de áreas particulares de la pantalla.
        </p>
    </div>
    
    <hr style='margin: 30px 0; border: none; border-top: 1px solid #dee2e6;'>
    <p style='color: #6c757d; font-size: 12px;'>
        Este reporte fue generado automáticamente por Capturer - Sistema de Cuadrantes Beta.<br>
        Si tienes alguna pregunta, contacta al administrador del sistema.
    </p>
</body>
</html>";
    }

    private string CreateQuadrantEmailText(DateTime startDate, DateTime endDate, int count, string quadrantList)
    {
        return $@"
REPORTE DE CUADRANTES - CAPTURER
================================

Período del reporte: {startDate:yyyy-MM-dd} hasta {endDate:yyyy-MM-dd}
Cuadrantes incluidos: {quadrantList}
Total de archivos: {count}
Computadora: {Environment.MachineName}
Generado: {DateTime.Now:yyyy-MM-dd HH:mm:ss}

{(count > 0 ? "Los archivos de los cuadrantes seleccionados están organizados en el archivo ZIP adjunto." : "No se encontraron archivos de cuadrantes para el período especificado.")}

Acerca de los Cuadrantes:
Los cuadrantes son secciones específicas de las capturas de pantalla que se procesan
automáticamente para facilitar el análisis de áreas particulares de la pantalla.

---
Este reporte fue generado automáticamente por Capturer - Sistema de Cuadrantes Beta.
Si tienes alguna pregunta, contacta al administrador del sistema.
";
    }
    
    private string CreateSingleQuadrantEmailHtml(DateTime startDate, DateTime endDate, int count, string quadrantName)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Reporte de Cuadrante '{quadrantName}' - Capturer</title>
</head>
<body style='font-family: Arial, sans-serif; margin: 20px;'>
    <h2 style='color: #2c3e50;'>📐 Reporte de Cuadrante '{quadrantName}' - Capturer</h2>
    <div style='background-color: #f8f9fa; padding: 15px; border-left: 4px solid #007bff; margin: 20px 0;'>
        <h3 style='margin-top: 0; color: #495057;'>Detalles del Reporte</h3>
        <p><strong>Período:</strong> {startDate:dd/MM/yyyy} hasta {endDate:dd/MM/yyyy}</p>
        <p><strong>Cuadrante:</strong> {quadrantName}</p>
        <p><strong>Total de archivos:</strong> {count}</p>
        <p><strong>Computadora:</strong> {Environment.MachineName}</p>
        <p><strong>Generado:</strong> {DateTime.Now:dd/MM/yyyy HH:mm:ss}</p>
    </div>
    
    {(count > 0 ? 
        @"<div style='background-color: #d4edda; padding: 10px; border-radius: 5px; margin: 15px 0;'>
            <p style='margin: 0; color: #155724;'>✓ Los archivos del cuadrante están adjuntos en este email.</p>
        </div>" : 
        @"<div style='background-color: #f8d7da; padding: 10px; border-radius: 5px; margin: 15px 0;'>
            <p style='margin: 0; color: #721c24;'>⚠ No se encontraron archivos para este cuadrante en el período especificado.</p>
        </div>")}
    
    <div style='background-color: #e2e3e5; padding: 10px; border-radius: 5px; margin: 20px 0; font-size: 12px;'>
        <h4 style='margin-top: 0; color: #6c757d;'>Acerca de los Cuadrantes</h4>
        <p style='margin: 5px 0; color: #6c757d;'>Los cuadrantes son secciones específicas de las capturas de pantalla que se procesan</p>
        <p style='margin: 5px 0; color: #6c757d;'>automáticamente para facilitar el análisis de áreas particulares de la pantalla.</p>
        <p style='margin: 5px 0; color: #6c757d;'>Este cuadrante ('{quadrantName}') contiene datos procesados específicamente para esta sección.</p>
    </div>
    
    <hr style='margin: 30px 0; border: none; border-top: 1px solid #dee2e6;'>
    <p style='font-size: 12px; color: #6c757d; text-align: center;'>
        Este reporte fue generado automáticamente por Capturer - Sistema de Cuadrantes Beta.<br>
        Si tienes alguna pregunta, contacta al administrador del sistema.
    </p>
</body>
</html>
";
    }
    
    private string CreateSingleQuadrantEmailText(DateTime startDate, DateTime endDate, int count, string quadrantName)
    {
        return $@"
REPORTE DE CUADRANTE '{quadrantName.ToUpper()}' - CAPTURER
======================================================

Período del reporte: {startDate:yyyy-MM-dd} hasta {endDate:yyyy-MM-dd}
Cuadrante: {quadrantName}
Total de archivos: {count}
Computadora: {Environment.MachineName}
Generado: {DateTime.Now:yyyy-MM-dd HH:mm:ss}

{(count > 0 ? "Los archivos del cuadrante están adjuntos en este email." : "No se encontraron archivos para este cuadrante en el período especificado.")}

Acerca de los Cuadrantes:
Los cuadrantes son secciones específicas de las capturas de pantalla que se procesan
automáticamente para facilitar el análisis de áreas particulares de la pantalla.
Este cuadrante ('{quadrantName}') contiene datos procesados específicamente para esta sección.

---
Este reporte fue generado automáticamente por Capturer - Sistema de Cuadrantes Beta.
Si tienes alguna pregunta, contacta al administrador del sistema.
";
    }

    /// <summary>
    /// Envía reporte de Activity Dashboard con archivos HTML personalizados
    /// </summary>
    public async Task<bool> SendActivityDashboardReportAsync(List<string> recipients, string subject, string body, List<string> attachmentFiles, bool useZipFormat = true)
    {
        try
        {
            await LoadConfigurationAsync();

            if (!attachmentFiles.Any())
            {
                Console.WriteLine("No hay archivos para adjuntar en el reporte de Activity Dashboard");
                return false;
            }

            // Crear mensaje de email
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_config.Email.SenderName, _config.Email.Username));
            message.Subject = subject;

            // Añadir destinatarios
            foreach (var recipient in recipients)
            {
                message.To.Add(MailboxAddress.Parse(recipient));
            }

            var bodyBuilder = new BodyBuilder();
            
            // Crear contenido HTML y texto
            bodyBuilder.HtmlBody = CreateActivityDashboardEmailHtml(body, attachmentFiles);
            bodyBuilder.TextBody = body;

            var tempFiles = new List<string>();

            if (useZipFormat && attachmentFiles.Count > 1)
            {
                // Crear ZIP con todos los archivos
                var zipPath = await CreateActivityDashboardZipAsync(attachmentFiles);
                if (!string.IsNullOrEmpty(zipPath))
                {
                    bodyBuilder.Attachments.Add(zipPath);
                    tempFiles.Add(zipPath);
                }
            }
            else
            {
                // Adjuntar archivos individuales
                foreach (var file in attachmentFiles)
                {
                    if (File.Exists(file))
                    {
                        bodyBuilder.Attachments.Add(file);
                    }
                }
            }

            message.Body = bodyBuilder.ToMessageBody();

            // Enviar email
            var success = await SendEmailAsync(message);

            // Limpiar archivos temporales
            foreach (var tempFile in tempFiles)
            {
                try
                {
                    if (File.Exists(tempFile))
                    {
                        File.Delete(tempFile);
                    }
                }
                catch { /* Ignore cleanup errors */ }
            }

            // Disparar evento
            EmailSent?.Invoke(this, new EmailSentEventArgs
            {
                Recipients = recipients,
                AttachmentCount = attachmentFiles.Count,
                SentDate = DateTime.Now,
                Success = success
            });

            return success;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error enviando reporte de Activity Dashboard: {ex.Message}");
            
            EmailSent?.Invoke(this, new EmailSentEventArgs
            {
                Recipients = recipients,
                Success = false,
                ErrorMessage = ex.Message,
                SentDate = DateTime.Now
            });
            
            return false;
        }
    }

    /// <summary>
    /// Crea contenido HTML para email de Activity Dashboard
    /// </summary>
    private string CreateActivityDashboardEmailHtml(string bodyText, List<string> attachmentFiles)
    {
        var fileList = attachmentFiles.Select(f => Path.GetFileNameWithoutExtension(f)).ToList();
        
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Activity Dashboard Report - Capturer</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 20px; background-color: #f8f9fa; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ text-align: center; border-bottom: 3px solid #007bff; padding-bottom: 20px; margin-bottom: 30px; }}
        .title {{ color: #2c3e50; font-size: 24px; margin-bottom: 10px; }}
        .content {{ line-height: 1.6; white-space: pre-line; }}
        .files-section {{ background-color: #e7f3ff; padding: 20px; border-radius: 8px; margin: 20px 0; }}
        .file-item {{ background-color: white; padding: 8px 12px; margin: 5px 0; border-radius: 5px; border-left: 4px solid #28a745; font-family: monospace; }}
        .footer {{ text-align: center; margin-top: 30px; padding-top: 20px; border-top: 1px solid #dee2e6; color: #6c757d; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='title'>📊 Activity Dashboard Report</div>
            <div style='color: #6c757d;'>Sistema de Monitoreo Avanzado - Capturer</div>
        </div>
        
        <div class='content'>
            {bodyText.Replace("\n", "<br>")}
        </div>
        
        <div class='files-section'>
            <h3 style='color: #495057; margin-top: 0;'>📁 Archivos Incluidos ({attachmentFiles.Count})</h3>
            {string.Join("", fileList.Select(file => $"<div class='file-item'>📄 {file}</div>"))}
        </div>
        
        <div class='footer'>
            <p>🤖 Generado automáticamente por <strong>Capturer v2.4</strong></p>
            <p>📧 Activity Dashboard - Reportes Avanzados</p>
        </div>
    </div>
</body>
</html>";
    }

    /// <summary>
    /// Crea ZIP con reportes de Activity Dashboard
    /// </summary>
    private async Task<string> CreateActivityDashboardZipAsync(List<string> files)
    {
        try
        {
            var tempPath = Path.GetTempPath();
            var zipFileName = $"ActivityDashboard_Report_{DateTime.Now:yyyyMMdd_HHmmss}.zip";
            var zipPath = Path.Combine(tempPath, zipFileName);

            using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                foreach (var file in files)
                {
                    if (File.Exists(file))
                    {
                        var entryName = Path.GetFileName(file);
                        archive.CreateEntryFromFile(file, entryName);
                    }
                }
            }

            return zipPath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creando ZIP de Activity Dashboard: {ex.Message}");
            return "";
        }
    }

    #region Activity Report Methods (Consolidated from EmailActivityReportExtension)

    public async Task<bool> SendActivityReportAsync(ActivityReport report, List<string> recipients, ActivityEmailIntegration config)
    {
        try
        {
            Console.WriteLine($"[EmailService] Enviando reporte de actividad: {report.Id}");

            if (_activityReportService == null)
            {
                Console.WriteLine("[EmailService] ActivityReportService no disponible, no se puede enviar reporte de actividad");
                return false;
            }

            var attachments = new List<string>();

            // Export reports in preferred formats
            foreach (var format in config.PreferredFormats)
            {
                try
                {
                    var exportPath = await _activityReportService.ExportReportAsync(report, format);
                    attachments.Add(exportPath);
                    Console.WriteLine($"[EmailService] Reporte exportado en {format}: {exportPath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[EmailService] Error exportando {format}: {ex.Message}");
                }
            }

            // Create the email
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_config.Email.SenderName, _config.Email.Username));
            message.Subject = GenerateActivityReportSubject(report, config);

            // Add recipients to the message
            foreach (var recipient in recipients)
            {
                message.To.Add(MailboxAddress.Parse(recipient));
            }

            // Create body
            var bodyBuilder = new BodyBuilder();
            
            if (config.EmbedSummaryInEmail)
            {
                bodyBuilder.HtmlBody = GenerateActivityReportHtmlBody(report);
                bodyBuilder.TextBody = GenerateActivityReportTextBody(report);
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
                            Console.WriteLine($"[EmailService] Adjunto agregado: {Path.GetFileName(attachment)}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[EmailService] Error agregando adjunto {attachment}: {ex.Message}");
                    }
                }
            }

            message.Body = bodyBuilder.ToMessageBody();

            // Send email using existing infrastructure
            var success = await SendEmailAsync(message);

            // Cleanup temporary files
            CleanupActivityReportFiles(attachments);

            if (success)
            {
                Console.WriteLine($"[EmailService] Reporte de actividad enviado exitosamente a {recipients.Count} destinatarios");
                EmailSent?.Invoke(this, new EmailSentEventArgs { Recipients = recipients, Success = true, AttachmentCount = attachments.Count });
            }

            return success;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EmailService] Error enviando reporte de actividad: {ex.Message}");
            EmailSent?.Invoke(this, new EmailSentEventArgs { Recipients = recipients, Success = false, ErrorMessage = ex.Message });
            return false;
        }
    }

    public async Task<bool> SendManualReportWithActivityAsync(List<string> recipients, DateTime startDate, DateTime endDate, ActivityReport? activityReport = null, bool includeActivityData = false)
    {
        try
        {
            if (!includeActivityData || activityReport == null)
            {
                // Send regular manual report
                return await SendManualReportAsync(recipients, startDate, endDate);
            }

            Console.WriteLine($"[EmailService] Enviando reporte manual con datos de actividad incluidos");

            // Create enhanced email with activity data
            var config = new ActivityEmailIntegration
            {
                AttachDetailedReport = true,
                EmbedSummaryInEmail = true,
                PreferredFormats = new List<string> { "HTML", "CSV" }
            };

            // Send the activity report as part of manual report
            var activitySuccess = await SendActivityReportAsync(activityReport, recipients, config);
            
            // Also send regular screenshots if needed
            var regularSuccess = await SendManualReportAsync(recipients, startDate, endDate);

            return activitySuccess && regularSuccess;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EmailService] Error en reporte manual con actividad: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> SendScheduledReportWithActivityAsync(List<string> recipients, DateTime startDate, DateTime endDate, List<string> screenshots, ActivityReport? activityReport = null, ActivityEmailIntegration? config = null)
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
                return await SendEnhancedReportAsync(recipients, startDate, endDate, screenshots, "Scheduled", true);
            }

            Console.WriteLine($"[EmailService] Enviando reporte programado con datos de actividad");

            // Send activity report first
            var activitySuccess = await SendActivityReportAsync(activityReport, recipients, config);
            
            // Then send regular screenshots report
            var regularSuccess = await SendEnhancedReportAsync(recipients, startDate, endDate, screenshots, "Scheduled+Activity", true);

            return activitySuccess && regularSuccess;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EmailService] Error en reporte programado con actividad: {ex.Message}");
            return false;
        }
    }

    #endregion

    #region Activity Report Helper Methods

    private string GenerateActivityReportSubject(ActivityReport report, ActivityEmailIntegration config)
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

    private string GenerateActivityReportHtmlBody(ActivityReport report)
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

    private string GenerateActivityReportTextBody(ActivityReport report)
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

    private void CleanupActivityReportFiles(List<string> files)
    {
        foreach (var file in files)
        {
            try
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                    Console.WriteLine($"[EmailService] Archivo temporal eliminado: {Path.GetFileName(file)}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EmailService] Error eliminando archivo temporal {file}: {ex.Message}");
            }
        }
    }

    #endregion
}