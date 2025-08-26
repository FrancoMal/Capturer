using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Capturer.Models;

namespace Capturer.Services;

public interface IScreenshotService
{
    Task<ScreenshotInfo?> CaptureScreenshotAsync();
    Task StartAutomaticCaptureAsync(TimeSpan interval);
    Task StopAutomaticCaptureAsync();
    bool IsCapturing { get; }
    DateTime? NextCaptureTime { get; }
    event EventHandler<ScreenshotCapturedEventArgs>? ScreenshotCaptured;
}

public class ScreenshotService : IScreenshotService, IDisposable
{
    private readonly IConfigurationManager _configManager;
    private System.Threading.Timer? _captureTimer;
    private bool _isCapturing = false;
    private CapturerConfiguration _config = new();
    private DateTime? _nextCaptureTime;
    private TimeSpan _captureInterval;

    public bool IsCapturing => _isCapturing;
    public DateTime? NextCaptureTime => _nextCaptureTime;

    public event EventHandler<ScreenshotCapturedEventArgs>? ScreenshotCaptured;

    // Windows API imports
    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr hObject, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hObjectSource, int nXSrc, int nYSrc, int dwRop);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int nWidth, int nHeight);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hDC);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hDC);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

    public ScreenshotService(IConfigurationManager configManager)
    {
        _configManager = configManager;
        LoadConfigurationAsync().ConfigureAwait(false);
    }

    private async Task LoadConfigurationAsync()
    {
        try
        {
            _config = await _configManager.LoadConfigurationAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading configuration: {ex.Message}");
            _config = new CapturerConfiguration(); // Use default if loading fails
        }
    }

    public async Task<ScreenshotInfo?> CaptureScreenshotAsync()
    {
        try
        {
            await LoadConfigurationAsync();
            
            var screenshot = await Task.Run(() => CaptureScreen());
            if (screenshot == null)
                return null;

            var screenshotInfo = await SaveScreenshotAsync(screenshot);
            
            ScreenshotCaptured?.Invoke(this, new ScreenshotCapturedEventArgs 
            { 
                Screenshot = screenshotInfo, 
                Success = true 
            });

            return screenshotInfo;
        }
        catch (Exception ex)
        {
            ScreenshotCaptured?.Invoke(this, new ScreenshotCapturedEventArgs 
            { 
                Success = false, 
                ErrorMessage = ex.Message,
                Screenshot = new ScreenshotInfo()
            });
            return null;
        }
    }

    private Bitmap? CaptureScreen()
    {
        try
        {
            // Get primary screen bounds
            var bounds = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
            
            if (_config.Screenshot.ScreenIndex >= 0 && _config.Screenshot.ScreenIndex < Screen.AllScreens.Length)
            {
                bounds = Screen.AllScreens[_config.Screenshot.ScreenIndex].Bounds;
            }
            else if (_config.Screenshot.ScreenIndex == -1)
            {
                // Capture all screens
                var minX = Screen.AllScreens.Min(s => s.Bounds.X);
                var minY = Screen.AllScreens.Min(s => s.Bounds.Y);
                var maxX = Screen.AllScreens.Max(s => s.Bounds.X + s.Bounds.Width);
                var maxY = Screen.AllScreens.Max(s => s.Bounds.Y + s.Bounds.Height);
                bounds = new Rectangle(minX, minY, maxX - minX, maxY - minY);
            }

            var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
            
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
                
                if (_config.Screenshot.IncludeCursor)
                {
                    // Draw cursor - simplified implementation
                    var cursorPosition = Cursor.Position;
                    if (bounds.Contains(cursorPosition))
                    {
                        var relativeCursor = new Point(cursorPosition.X - bounds.X, cursorPosition.Y - bounds.Y);
                        Cursors.Default.Draw(graphics, new Rectangle(relativeCursor, new Size(32, 32)));
                    }
                }
            }

            return bitmap;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error capturing screen: {ex.Message}");
            return null;
        }
    }

    private async Task<ScreenshotInfo> SaveScreenshotAsync(Bitmap screenshot)
    {
        // Ensure screenshot directory exists
        Directory.CreateDirectory(_config.Storage.ScreenshotFolder);

        var now = DateTime.Now;
        var fileName = $"{now:yyyy-MM-dd_HH-mm-ss}.{_config.Screenshot.ImageFormat.ToLower()}";
        var fullPath = Path.Combine(_config.Storage.ScreenshotFolder, fileName);

        // Get image format
        var format = _config.Screenshot.ImageFormat.ToLower() switch
        {
            "jpg" or "jpeg" => ImageFormat.Jpeg,
            "bmp" => ImageFormat.Bmp,
            "gif" => ImageFormat.Gif,
            "tiff" => ImageFormat.Tiff,
            _ => ImageFormat.Png
        };

        // Save with quality settings for JPEG
        if (format == ImageFormat.Jpeg)
        {
            var encoderParameters = new EncoderParameters(1);
            encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, (long)_config.Screenshot.Quality);
            var jpegEncoder = ImageCodecInfo.GetImageEncoders().First(e => e.FormatID == ImageFormat.Jpeg.Guid);
            screenshot.Save(fullPath, jpegEncoder, encoderParameters);
        }
        else
        {
            screenshot.Save(fullPath, format);
        }

        var fileInfo = new FileInfo(fullPath);

        return new ScreenshotInfo
        {
            FileName = fileName,
            FullPath = fullPath,
            CaptureTime = now,
            FileSize = fileInfo.Length,
            ComputerName = Environment.MachineName
        };
    }

    public async Task StartAutomaticCaptureAsync(TimeSpan interval)
    {
        await StopAutomaticCaptureAsync();
        
        _isCapturing = true;
        _captureInterval = interval;
        _nextCaptureTime = DateTime.Now.Add(interval);
        
        // Use a more reliable timer implementation
        _captureTimer = new System.Threading.Timer(async _ =>
        {
            if (_isCapturing)
            {
                try
                {
                    await CaptureScreenshotAsync();
                    // Update next capture time after successful capture
                    _nextCaptureTime = DateTime.Now.Add(_captureInterval);
                    Console.WriteLine($"Screenshot captured at {DateTime.Now:HH:mm:ss}, next at {_nextCaptureTime:HH:mm:ss}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in timer callback: {ex.Message}");
                }
            }
        }, null, interval, interval);
    }

    public async Task StopAutomaticCaptureAsync()
    {
        _isCapturing = false;
        _nextCaptureTime = null;
        
        if (_captureTimer != null)
        {
            await _captureTimer.DisposeAsync();
            _captureTimer = null;
        }
    }

    public void Dispose()
    {
        StopAutomaticCaptureAsync().Wait();
        _captureTimer?.Dispose();
    }
}