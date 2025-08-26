using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.IO.Compression;
using Capturer.Models;

namespace Capturer.Services;

public interface IEmailService
{
    Task<bool> SendWeeklyReportAsync(List<string> recipients, DateTime startDate, DateTime endDate);
    Task<bool> SendManualReportAsync(List<string> recipients, DateTime startDate, DateTime endDate, bool useZipFormat = true);
    Task<bool> ValidateEmailConfigAsync();
    Task<bool> TestConnectionAsync();
    event EventHandler<EmailSentEventArgs>? EmailSent;
}

public class EmailService : IEmailService
{
    private readonly IConfigurationManager _configManager;
    private readonly IFileService _fileService;
    private CapturerConfiguration _config = new();
    private const long MaxAttachmentSize = 25 * 1024 * 1024; // 25MB

    public event EventHandler<EmailSentEventArgs>? EmailSent;

    public EmailService(IConfigurationManager configManager, IFileService fileService)
    {
        _configManager = configManager;
        _fileService = fileService;
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
            
            await client.ConnectAsync(_config.Email.SmtpServer, _config.Email.SmtpPort, 
                _config.Email.UseSSL ? SecureSocketOptions.StartTls : SecureSocketOptions.None);

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
            await client.ConnectAsync(_config.Email.SmtpServer, _config.Email.SmtpPort,
                _config.Email.UseSSL ? SecureSocketOptions.StartTls : SecureSocketOptions.None);

            if (!string.IsNullOrEmpty(_config.Email.Username))
            {
                await client.AuthenticateAsync(_config.Email.Username, _config.Email.Password);
            }

            await client.DisconnectAsync(true);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Email connection test failed: {ex.Message}");
            return false;
        }
    }
}