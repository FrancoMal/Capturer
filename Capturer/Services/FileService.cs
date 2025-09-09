using Capturer.Models;

namespace Capturer.Services;

public interface IFileService
{
    Task<List<string>> GetScreenshotsByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<List<ScreenshotInfo>> GetScreenshotInfosByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<List<string>> GetScreenshotsByReportPeriodAsync(ReportPeriod period);
    Task<long> GetFolderSizeAsync();
    Task<int> GetScreenshotCountAsync();
    Task CleanupOldFilesAsync(TimeSpan? retentionPeriod = null);
    Task<bool> EnsureDirectoryExistsAsync();
    Task<List<string>> GetRecentScreenshotsAsync(int count = 10);
    Task<DirectoryInfo> GetScreenshotDirectoryInfoAsync();
}

public class FileService : IFileService
{
    private readonly IConfigurationManager _configManager;
    private CapturerConfiguration _config = new();

    public FileService(IConfigurationManager configManager)
    {
        _configManager = configManager;
        LoadConfigurationAsync().ConfigureAwait(false);
    }

    private async Task LoadConfigurationAsync()
    {
        _config = await _configManager.LoadConfigurationAsync();
    }

    public async Task<List<string>> GetScreenshotsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        await LoadConfigurationAsync();
        await EnsureDirectoryExistsAsync();

        try
        {
            var screenshotDir = new DirectoryInfo(_config.Storage.ScreenshotFolder);
            if (!screenshotDir.Exists)
                return new List<string>();

            var supportedExtensions = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tiff" };
            
            var files = screenshotDir
                .GetFiles("*.*", SearchOption.AllDirectories)
                .Where(f => supportedExtensions.Contains(f.Extension.ToLower()))
                .Where(f => TryParseFilenameDate(f.Name, out var fileDate) && 
                           fileDate >= startDate && fileDate <= endDate.AddDays(1)) // Include end date
                .OrderBy(f => TryParseFilenameDate(f.Name, out var date) ? date : f.CreationTime)
                .Select(f => f.FullName)
                .ToList();

            return files;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting screenshots by date range: {ex.Message}");
            return new List<string>();
        }
    }

    public async Task<List<ScreenshotInfo>> GetScreenshotInfosByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        var filePaths = await GetScreenshotsByDateRangeAsync(startDate, endDate);
        var screenshots = new List<ScreenshotInfo>();

        foreach (var filePath in filePaths)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                if (!fileInfo.Exists) continue;

                var fileName = fileInfo.Name;
                var captureTime = TryParseFilenameDate(fileName, out var parsedDate) ? parsedDate : fileInfo.CreationTime;

                screenshots.Add(new ScreenshotInfo
                {
                    FileName = fileName,
                    FullPath = filePath,
                    CaptureTime = captureTime,
                    FileSize = fileInfo.Length,
                    ComputerName = Environment.MachineName
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing screenshot file {filePath}: {ex.Message}");
            }
        }

        return screenshots.OrderByDescending(s => s.CaptureTime).ToList();
    }

    public async Task<List<string>> GetScreenshotsByReportPeriodAsync(ReportPeriod period)
    {
        await LoadConfigurationAsync();
        await EnsureDirectoryExistsAsync();

        try
        {
            var screenshotDir = new DirectoryInfo(_config.Storage.ScreenshotFolder);
            if (!screenshotDir.Exists)
                return new List<string>();

            var supportedExtensions = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tiff" };
            var allFiles = screenshotDir
                .GetFiles("*.*", SearchOption.AllDirectories)
                .Where(f => supportedExtensions.Contains(f.Extension.ToLower()))
                .ToList();

            var filteredFiles = new List<string>();

            foreach (var file in allFiles)
            {
                try
                {
                    // Usar la fecha del nombre del archivo si es posible, sino usar CreationTime
                    var fileDate = TryParseFilenameDate(file.Name, out var parsedDate) ? parsedDate : file.CreationTime;

                    // Verificar que esté en el rango de fechas
                    if (fileDate < period.StartDate || fileDate > period.EndDate)
                        continue;

                    // Verificar día de la semana si está configurado
                    if (period.ActiveWeekDays.Any() && !period.ActiveWeekDays.Contains(fileDate.DayOfWeek))
                        continue;

                    // Verificar filtros de horario si están configurados
                    if (period.StartTime.HasValue && period.EndTime.HasValue)
                    {
                        var fileTime = fileDate.TimeOfDay;
                        
                        // Manejar casos donde EndTime < StartTime (ej: 23:00 - 08:00)
                        if (period.EndTime.Value < period.StartTime.Value)
                        {
                            // Horario nocturno que cruza medianoche
                            if (!(fileTime >= period.StartTime.Value || fileTime <= period.EndTime.Value))
                                continue;
                        }
                        else
                        {
                            // Horario normal dentro del mismo día
                            if (fileTime < period.StartTime.Value || fileTime > period.EndTime.Value)
                                continue;
                        }
                    }

                    filteredFiles.Add(file.FullName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing file {file.Name}: {ex.Message}");
                }
            }

            return filteredFiles.OrderBy(f => TryParseFilenameDate(Path.GetFileName(f), out var date) ? date : new FileInfo(f).CreationTime).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting screenshots by report period: {ex.Message}");
            return new List<string>();
        }
    }

    public async Task<long> GetFolderSizeAsync()
    {
        await LoadConfigurationAsync();
        
        try
        {
            var directoryInfo = new DirectoryInfo(_config.Storage.ScreenshotFolder);
            if (!directoryInfo.Exists)
                return 0;

            return directoryInfo
                .GetFiles("*.*", SearchOption.AllDirectories)
                .Sum(file => file.Length);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error calculating folder size: {ex.Message}");
            return 0;
        }
    }

    public async Task<int> GetScreenshotCountAsync()
    {
        await LoadConfigurationAsync();

        try
        {
            var directoryInfo = new DirectoryInfo(_config.Storage.ScreenshotFolder);
            if (!directoryInfo.Exists)
                return 0;

            var supportedExtensions = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tiff" };
            
            return directoryInfo
                .GetFiles("*.*", SearchOption.AllDirectories)
                .Count(f => supportedExtensions.Contains(f.Extension.ToLower()));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error counting screenshots: {ex.Message}");
            return 0;
        }
    }

    public async Task CleanupOldFilesAsync(TimeSpan? retentionPeriod = null)
    {
        await LoadConfigurationAsync();

        if (!_config.Storage.AutoCleanup && retentionPeriod == null)
            return;

        var retention = retentionPeriod ?? _config.Storage.RetentionPeriod;
        var cutoffDate = DateTime.Now.Subtract(retention);

        try
        {
            var directoryInfo = new DirectoryInfo(_config.Storage.ScreenshotFolder);
            if (!directoryInfo.Exists)
                return;

            var supportedExtensions = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tiff" };
            var filesToDelete = directoryInfo
                .GetFiles("*.*", SearchOption.AllDirectories)
                .Where(f => supportedExtensions.Contains(f.Extension.ToLower()))
                .Where(f => 
                {
                    if (TryParseFilenameDate(f.Name, out var fileDate))
                        return fileDate < cutoffDate;
                    return f.CreationTime < cutoffDate;
                })
                .ToList();

            var deletedCount = 0;
            var deletedSize = 0L;

            foreach (var file in filesToDelete)
            {
                try
                {
                    deletedSize += file.Length;
                    file.Delete();
                    deletedCount++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting file {file.FullName}: {ex.Message}");
                }
            }

            Console.WriteLine($"Cleanup completed: {deletedCount} files deleted, {deletedSize / 1024 / 1024}MB freed");

            // Also check folder size limit
            await EnforceFolderSizeLimitAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during cleanup: {ex.Message}");
        }
    }

    private async Task EnforceFolderSizeLimitAsync()
    {
        var currentSize = await GetFolderSizeAsync();
        if (currentSize <= _config.Storage.MaxFolderSizeBytes)
            return;

        try
        {
            var directoryInfo = new DirectoryInfo(_config.Storage.ScreenshotFolder);
            var supportedExtensions = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tiff" };
            
            var files = directoryInfo
                .GetFiles("*.*", SearchOption.AllDirectories)
                .Where(f => supportedExtensions.Contains(f.Extension.ToLower()))
                .OrderBy(f => TryParseFilenameDate(f.Name, out var date) ? date : f.CreationTime) // Delete oldest first
                .ToList();

            var deletedSize = 0L;
            var targetSize = (long)(_config.Storage.MaxFolderSizeBytes * 0.8); // Delete until 80% of limit

            foreach (var file in files)
            {
                try
                {
                    deletedSize += file.Length;
                    file.Delete();
                    
                    if (currentSize - deletedSize <= targetSize)
                        break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting file {file.FullName}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error enforcing folder size limit: {ex.Message}");
        }
    }

    public async Task<bool> EnsureDirectoryExistsAsync()
    {
        await LoadConfigurationAsync();

        try
        {
            Directory.CreateDirectory(_config.Storage.ScreenshotFolder);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating directory: {ex.Message}");
            return false;
        }
    }

    public async Task<List<string>> GetRecentScreenshotsAsync(int count = 10)
    {
        await LoadConfigurationAsync();

        try
        {
            var directoryInfo = new DirectoryInfo(_config.Storage.ScreenshotFolder);
            if (!directoryInfo.Exists)
                return new List<string>();

            var supportedExtensions = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tiff" };
            
            var files = directoryInfo
                .GetFiles("*.*", SearchOption.AllDirectories)
                .Where(f => supportedExtensions.Contains(f.Extension.ToLower()))
                .OrderByDescending(f => TryParseFilenameDate(f.Name, out var date) ? date : f.CreationTime)
                .Take(count)
                .Select(f => f.FullName)
                .ToList();

            return files;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting recent screenshots: {ex.Message}");
            return new List<string>();
        }
    }

    public async Task<DirectoryInfo> GetScreenshotDirectoryInfoAsync()
    {
        await LoadConfigurationAsync();
        await EnsureDirectoryExistsAsync();
        return new DirectoryInfo(_config.Storage.ScreenshotFolder);
    }

    private bool TryParseFilenameDate(string filename, out DateTime date)
    {
        date = DateTime.MinValue;

        try
        {
            // Expected format: yyyy-MM-dd_HH-mm-ss.extension
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(filename);
            
            // Split by underscore to separate date and time
            var parts = nameWithoutExtension.Split('_');
            if (parts.Length != 2) return false;

            var datePart = parts[0]; // yyyy-MM-dd
            var timePart = parts[1].Replace('-', ':'); // HH-mm-ss -> HH:mm:ss

            var dateTimeString = $"{datePart} {timePart}";
            
            if (DateTime.TryParseExact(dateTimeString, "yyyy-MM-dd HH:mm:ss", 
                System.Globalization.CultureInfo.InvariantCulture, 
                System.Globalization.DateTimeStyles.None, out date))
            {
                return true;
            }
        }
        catch
        {
            // Ignore parsing errors
        }

        return false;
    }

    public static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }
}