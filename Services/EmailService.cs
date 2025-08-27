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
    Task<bool> SendQuadrantReportAsync(List<string> recipients, DateTime startDate, DateTime endDate, List<string> selectedQuadrants, bool useZipFormat = true);
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
    private CapturerConfiguration _config = new();
    private const long MaxAttachmentSize = 25 * 1024 * 1024; // 25MB

    public event EventHandler<EmailSentEventArgs>? EmailSent;

    public EmailService(IConfigurationManager configManager, IFileService fileService, IQuadrantService? quadrantService = null)
    {
        _configManager = configManager;
        _fileService = fileService;
        _quadrantService = quadrantService;
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
}