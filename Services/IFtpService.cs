using Capturer.Models;

namespace Capturer.Services;

public interface IFtpService
{
    Task<bool> UploadFileAsync(string localFilePath, string remoteFileName, FtpSettings settings);
    Task<bool> UploadFilesAsync(List<string> localFilePaths, FtpSettings settings);
    Task<bool> TestConnectionAsync(FtpSettings settings);
    Task<bool> CreateDirectoryAsync(string remoteDirectory, FtpSettings settings);
    Task<List<string>> ListFilesAsync(string remoteDirectory, FtpSettings settings);
    Task<bool> DeleteFileAsync(string remoteFilePath, FtpSettings settings);
    event EventHandler<FtpUploadEventArgs>? FileUploaded;
}

public class FtpUploadEventArgs : EventArgs
{
    public string LocalFilePath { get; set; } = "";
    public string RemoteFilePath { get; set; } = "";
    public bool Success { get; set; }
    public long FileSize { get; set; }
    public TimeSpan UploadTime { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime UploadDate { get; set; } = DateTime.Now;
}