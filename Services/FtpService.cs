using System.Net;
using System.IO.Compression;
using Capturer.Models;
using Renci.SshNet;

namespace Capturer.Services;

public class FtpService : IFtpService
{
    private readonly IConfigurationManager _configManager;
    private const long MaxFileSize = 100 * 1024 * 1024; // 100MB

    public event EventHandler<FtpUploadEventArgs>? FileUploaded;

    public FtpService(IConfigurationManager configManager)
    {
        _configManager = configManager;
    }

    public async Task<bool> UploadFileAsync(string localFilePath, string remoteFileName, FtpSettings settings)
    {
        var startTime = DateTime.Now;
        
        try
        {
            if (!File.Exists(localFilePath))
            {
                Console.WriteLine($"FTP Upload: Local file not found: {localFilePath}");
                return false;
            }

            var fileInfo = new FileInfo(localFilePath);
            
            // Check file size
            if (fileInfo.Length > MaxFileSize)
            {
                Console.WriteLine($"FTP Upload: File too large: {fileInfo.Length} bytes (max: {MaxFileSize})");
                return false;
            }

            // Prepare file path
            string finalFilePath = localFilePath;
            bool isTemporaryFile = false;
            
            // Compress if enabled
            if (settings.CompressBeforeUpload && Path.GetExtension(localFilePath).ToLower() != ".zip")
            {
                finalFilePath = await CompressFileForUploadAsync(localFilePath);
                isTemporaryFile = true;
                remoteFileName = Path.GetFileNameWithoutExtension(remoteFileName) + ".zip";
            }

            var remoteFilePath = $"{settings.RemoteDirectory.TrimEnd('/')}/{remoteFileName}";
            bool success;

            try
            {
                if (settings.UseSftp)
                {
                    success = await UploadViaSftpAsync(finalFilePath, remoteFilePath, settings);
                }
                else
                {
                    success = await UploadViaFtpAsync(finalFilePath, remoteFilePath, settings);
                }
            }
            finally
            {
                // Clean up temporary compressed file
                if (isTemporaryFile && File.Exists(finalFilePath))
                {
                    try { File.Delete(finalFilePath); } catch { /* ignore */ }
                }
            }

            var uploadTime = DateTime.Now - startTime;

            // Fire event
            FileUploaded?.Invoke(this, new FtpUploadEventArgs
            {
                LocalFilePath = localFilePath,
                RemoteFilePath = remoteFilePath,
                Success = success,
                FileSize = fileInfo.Length,
                UploadTime = uploadTime,
                UploadDate = startTime
            });

            Console.WriteLine($"FTP Upload {(success ? "successful" : "failed")}: {localFilePath} -> {remoteFilePath} ({uploadTime.TotalSeconds:F2}s)");
            return success;
        }
        catch (Exception ex)
        {
            var uploadTime = DateTime.Now - startTime;
            
            FileUploaded?.Invoke(this, new FtpUploadEventArgs
            {
                LocalFilePath = localFilePath,
                RemoteFilePath = $"{settings.RemoteDirectory}/{remoteFileName}",
                Success = false,
                FileSize = File.Exists(localFilePath) ? new FileInfo(localFilePath).Length : 0,
                UploadTime = uploadTime,
                ErrorMessage = ex.Message,
                UploadDate = startTime
            });

            Console.WriteLine($"FTP Upload error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> UploadFilesAsync(List<string> localFilePaths, FtpSettings settings)
    {
        if (!settings.Enabled || !localFilePaths.Any())
            return false;

        Console.WriteLine($"FTP Batch Upload: Starting upload of {localFilePaths.Count} files");
        
        bool overallSuccess = true;
        int successCount = 0;

        foreach (var filePath in localFilePaths)
        {
            var fileName = Path.GetFileName(filePath);
            var success = await UploadFileAsync(filePath, fileName, settings);
            
            if (success)
            {
                successCount++;
            }
            else
            {
                overallSuccess = false;
            }

            // Small delay between uploads to avoid overwhelming server
            await Task.Delay(100);
        }

        Console.WriteLine($"FTP Batch Upload completed: {successCount}/{localFilePaths.Count} files uploaded successfully");
        return overallSuccess;
    }

    public async Task<bool> TestConnectionAsync(FtpSettings settings)
    {
        try
        {
            if (!settings.Enabled)
                return false;

            if (settings.UseSftp)
            {
                return await TestSftpConnectionAsync(settings);
            }
            else
            {
                return await TestFtpConnectionAsync(settings);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FTP Connection test failed: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> CreateDirectoryAsync(string remoteDirectory, FtpSettings settings)
    {
        try
        {
            if (settings.UseSftp)
            {
                return await CreateSftpDirectoryAsync(remoteDirectory, settings);
            }
            else
            {
                return await CreateFtpDirectoryAsync(remoteDirectory, settings);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FTP Create directory error: {ex.Message}");
            return false;
        }
    }

    public async Task<List<string>> ListFilesAsync(string remoteDirectory, FtpSettings settings)
    {
        try
        {
            if (settings.UseSftp)
            {
                return await ListSftpFilesAsync(remoteDirectory, settings);
            }
            else
            {
                return await ListFtpFilesAsync(remoteDirectory, settings);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FTP List files error: {ex.Message}");
            return new List<string>();
        }
    }

    public async Task<bool> DeleteFileAsync(string remoteFilePath, FtpSettings settings)
    {
        try
        {
            if (settings.UseSftp)
            {
                return await DeleteSftpFileAsync(remoteFilePath, settings);
            }
            else
            {
                return await DeleteFtpFileAsync(remoteFilePath, settings);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FTP Delete file error: {ex.Message}");
            return false;
        }
    }

    #region FTP Implementation

    private async Task<bool> UploadViaFtpAsync(string localFilePath, string remoteFilePath, FtpSettings settings)
    {
        return await Task.Run(() =>
        {
            try
            {
                var request = (FtpWebRequest)WebRequest.Create($"ftp://{settings.Server}:{settings.Port}{remoteFilePath}");
                request.Method = WebRequestMethods.Ftp.UploadFile;
                request.Credentials = new NetworkCredential(settings.Username, settings.Password);
                request.UsePassive = settings.UsePassiveMode;
                request.Timeout = settings.TimeoutSeconds * 1000;

                using var fileStream = File.OpenRead(localFilePath);
                using var ftpStream = request.GetRequestStream();
                fileStream.CopyTo(ftpStream);

                using var response = (FtpWebResponse)request.GetResponse();
                return response.StatusCode == FtpStatusCode.ClosingData || response.StatusCode == FtpStatusCode.FileActionOK;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FTP upload error: {ex.Message}");
                return false;
            }
        });
    }

    private async Task<bool> TestFtpConnectionAsync(FtpSettings settings)
    {
        return await Task.Run(() =>
        {
            try
            {
                var request = (FtpWebRequest)WebRequest.Create($"ftp://{settings.Server}:{settings.Port}/");
                request.Method = WebRequestMethods.Ftp.ListDirectory;
                request.Credentials = new NetworkCredential(settings.Username, settings.Password);
                request.UsePassive = settings.UsePassiveMode;
                request.Timeout = settings.TimeoutSeconds * 1000;

                using var response = (FtpWebResponse)request.GetResponse();
                return response.StatusCode == FtpStatusCode.OpeningData || response.StatusCode == FtpStatusCode.DataAlreadyOpen;
            }
            catch
            {
                return false;
            }
        });
    }

    private async Task<bool> CreateFtpDirectoryAsync(string remoteDirectory, FtpSettings settings)
    {
        return await Task.Run(() =>
        {
            try
            {
                var request = (FtpWebRequest)WebRequest.Create($"ftp://{settings.Server}:{settings.Port}{remoteDirectory}");
                request.Method = WebRequestMethods.Ftp.MakeDirectory;
                request.Credentials = new NetworkCredential(settings.Username, settings.Password);
                request.UsePassive = settings.UsePassiveMode;

                using var response = (FtpWebResponse)request.GetResponse();
                return response.StatusCode == FtpStatusCode.PathnameCreated;
            }
            catch
            {
                return false; // Directory might already exist
            }
        });
    }

    private async Task<List<string>> ListFtpFilesAsync(string remoteDirectory, FtpSettings settings)
    {
        return await Task.Run(() =>
        {
            try
            {
                var request = (FtpWebRequest)WebRequest.Create($"ftp://{settings.Server}:{settings.Port}{remoteDirectory}");
                request.Method = WebRequestMethods.Ftp.ListDirectory;
                request.Credentials = new NetworkCredential(settings.Username, settings.Password);
                request.UsePassive = settings.UsePassiveMode;

                using var response = (FtpWebResponse)request.GetResponse();
                using var reader = new StreamReader(response.GetResponseStream());
                
                var files = new List<string>();
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    files.Add(line);
                }
                return files;
            }
            catch
            {
                return new List<string>();
            }
        });
    }

    private async Task<bool> DeleteFtpFileAsync(string remoteFilePath, FtpSettings settings)
    {
        return await Task.Run(() =>
        {
            try
            {
                var request = (FtpWebRequest)WebRequest.Create($"ftp://{settings.Server}:{settings.Port}{remoteFilePath}");
                request.Method = WebRequestMethods.Ftp.DeleteFile;
                request.Credentials = new NetworkCredential(settings.Username, settings.Password);
                request.UsePassive = settings.UsePassiveMode;

                using var response = (FtpWebResponse)request.GetResponse();
                return response.StatusCode == FtpStatusCode.FileActionOK;
            }
            catch
            {
                return false;
            }
        });
    }

    #endregion

    #region SFTP Implementation

    private async Task<bool> UploadViaSftpAsync(string localFilePath, string remoteFilePath, FtpSettings settings)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var client = new SftpClient(settings.Server, settings.Port, settings.Username, settings.Password);
                client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
                
                client.Connect();
                
                using var fileStream = File.OpenRead(localFilePath);
                client.UploadFile(fileStream, remoteFilePath, true);
                
                client.Disconnect();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SFTP upload error: {ex.Message}");
                return false;
            }
        });
    }

    private async Task<bool> TestSftpConnectionAsync(FtpSettings settings)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var client = new SftpClient(settings.Server, settings.Port, settings.Username, settings.Password);
                client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
                
                client.Connect();
                
                // Test by listing root directory
                var files = client.ListDirectory("/");
                
                client.Disconnect();
                return true;
            }
            catch
            {
                return false;
            }
        });
    }

    private async Task<bool> CreateSftpDirectoryAsync(string remoteDirectory, FtpSettings settings)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var client = new SftpClient(settings.Server, settings.Port, settings.Username, settings.Password);
                client.Connect();
                
                if (!client.Exists(remoteDirectory))
                {
                    client.CreateDirectory(remoteDirectory);
                }
                
                client.Disconnect();
                return true;
            }
            catch
            {
                return false;
            }
        });
    }

    private async Task<List<string>> ListSftpFilesAsync(string remoteDirectory, FtpSettings settings)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var client = new SftpClient(settings.Server, settings.Port, settings.Username, settings.Password);
                client.Connect();
                
                var files = client.ListDirectory(remoteDirectory)
                    .Where(f => !f.IsDirectory)
                    .Select(f => f.Name)
                    .ToList();
                
                client.Disconnect();
                return files;
            }
            catch
            {
                return new List<string>();
            }
        });
    }

    private async Task<bool> DeleteSftpFileAsync(string remoteFilePath, FtpSettings settings)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var client = new SftpClient(settings.Server, settings.Port, settings.Username, settings.Password);
                client.Connect();
                
                if (client.Exists(remoteFilePath))
                {
                    client.DeleteFile(remoteFilePath);
                }
                
                client.Disconnect();
                return true;
            }
            catch
            {
                return false;
            }
        });
    }

    #endregion

    #region Helper Methods

    private async Task<string> CompressFileForUploadAsync(string filePath)
    {
        var tempZipPath = Path.GetTempFileName().Replace(".tmp", ".zip");
        
        try
        {
            using var archive = ZipFile.Open(tempZipPath, ZipArchiveMode.Create);
            archive.CreateEntryFromFile(filePath, Path.GetFileName(filePath));
            return tempZipPath;
        }
        catch
        {
            if (File.Exists(tempZipPath))
                File.Delete(tempZipPath);
            throw;
        }
    }

    #endregion
}