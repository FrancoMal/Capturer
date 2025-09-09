using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Capturer.Models;
using Capturer.Utils;

namespace Capturer.Services;

public interface IScreenshotService
{
    Task<ScreenshotInfo?> CaptureScreenshotAsync();
    Task StartAutomaticCaptureAsync(TimeSpan interval);
    Task StopAutomaticCaptureAsync();
    bool IsCapturing { get; }
    DateTime? NextCaptureTime { get; }
    List<ScreenInfo> GetAvailableScreens();
    Task RefreshScreenConfigurationAsync();
    event EventHandler<ScreenshotCapturedEventArgs>? ScreenshotCaptured;
    
    // Integración con monitoreo de actividad
    void SetQuadrantService(QuadrantService quadrantService);
}

public class ScreenshotService : IScreenshotService, IDisposable
{
    private readonly IConfigurationManager _configManager;
    private System.Threading.Timer? _captureTimer;
    private bool _isCapturing = false;
    private CapturerConfiguration _config = new();
    private DateTime? _nextCaptureTime;
    private TimeSpan _captureInterval;
    
    // Integración con monitoreo de actividad
    private QuadrantService? _quadrantService;

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

    public List<ScreenInfo> GetAvailableScreens()
    {
        var screens = new List<ScreenInfo>();
        var allScreens = Screen.AllScreens;
        
        for (int i = 0; i < allScreens.Length; i++)
        {
            var screen = allScreens[i];
            screens.Add(new ScreenInfo
            {
                Index = i,
                DeviceName = screen.DeviceName,
                DisplayName = $"Monitor {i + 1}",
                Width = screen.Bounds.Width,
                Height = screen.Bounds.Height,
                X = screen.Bounds.X,
                Y = screen.Bounds.Y,
                IsPrimary = screen.Primary
            });
        }
        
        return screens;
    }

    public async Task RefreshScreenConfigurationAsync()
    {
        try
        {
            await LoadConfigurationAsync();
            _config.Screenshot.AvailableScreens = GetAvailableScreens();
            await _configManager.SaveConfigurationAsync(_config);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error refreshing screen configuration: {ex.Message}");
        }
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
            
            // Procesar monitoreo de actividad si está habilitado
            List<QuadrantActivityResult>? activityResults = null;
            if (_quadrantService != null)
            {
                try
                {
                    activityResults = await _quadrantService.ProcessScreenshotForActivityAsync(screenshot);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error en monitoreo de actividad: {ex.Message}");
                }
            }
            
            ScreenshotCaptured?.Invoke(this, new ScreenshotCapturedEventArgs 
            { 
                Screenshot = screenshotInfo, 
                Success = true,
                ActivityResults = activityResults
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
            var bounds = GetCaptureBounds();
            if (bounds == Rectangle.Empty)
            {
                Console.WriteLine("No valid screen bounds found for capture");
                return null;
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

    private Rectangle GetCaptureBounds()
    {
        var allScreens = Screen.AllScreens;
        if (allScreens.Length == 0)
            return new Rectangle(0, 0, 1920, 1080); // Fallback

        switch (_config.Screenshot.CaptureMode)
        {
            case ScreenCaptureMode.PrimaryScreen:
                return Screen.PrimaryScreen?.Bounds ?? allScreens[0].Bounds;

            case ScreenCaptureMode.SingleScreen:
                var screenIndex = _config.Screenshot.SelectedScreenIndex;
                if (screenIndex >= 0 && screenIndex < allScreens.Length)
                {
                    return allScreens[screenIndex].Bounds;
                }
                // Fallback to primary if invalid index
                Console.WriteLine($"Invalid screen index {screenIndex}, falling back to primary screen");
                return Screen.PrimaryScreen?.Bounds ?? allScreens[0].Bounds;

            case ScreenCaptureMode.AllScreens:
            default:
                // Capture all screens as one large image
                var minX = allScreens.Min(s => s.Bounds.X);
                var minY = allScreens.Min(s => s.Bounds.Y);
                var maxX = allScreens.Max(s => s.Bounds.X + s.Bounds.Width);
                var maxY = allScreens.Max(s => s.Bounds.Y + s.Bounds.Height);
                return new Rectangle(minX, minY, maxX - minX, maxY - minY);
        }
    }

    private async Task<ScreenshotInfo> SaveScreenshotAsync(Bitmap screenshot)
    {
        // Ensure screenshot directory exists
        Directory.CreateDirectory(_config.Storage.ScreenshotFolder);

        var now = DateTime.Now;
        var fileName = $"{now:yyyy-MM-dd_HH-mm-ss}.{_config.Screenshot.ImageFormat.ToLower()}";
        var fullPath = Path.Combine(_config.Storage.ScreenshotFolder, fileName);

        // Apply privacy blur if enabled
        Bitmap finalScreenshot = screenshot;
        try
        {
            if (_config.Screenshot.EnablePrivacyBlur)
            {
                Console.WriteLine($"Applying privacy blur - Intensity: {_config.Screenshot.BlurIntensity}, Mode: {_config.Screenshot.BlurMode}");
                
                // Validate blur settings
                var (validatedIntensity, validatedMode, warning) = ImageBlurUtils.ValidateBlurSettings(
                    _config.Screenshot.BlurIntensity, _config.Screenshot.BlurMode);
                
                if (!string.IsNullOrEmpty(warning))
                {
                    Console.WriteLine($"Blur performance warning: {warning}");
                }
                
                finalScreenshot = await Task.Run(() => 
                    ImageBlurUtils.ApplyBlur(screenshot, validatedIntensity, validatedMode));
                
                Console.WriteLine($"Privacy blur applied successfully");
            }

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
                finalScreenshot.Save(fullPath, jpegEncoder, encoderParameters);
            }
            else
            {
                finalScreenshot.Save(fullPath, format);
            }
        }
        finally
        {
            // Dispose blur result if it was created (but not the original screenshot)
            if (finalScreenshot != screenshot)
            {
                finalScreenshot?.Dispose();
            }
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

    public void SetQuadrantService(QuadrantService quadrantService)
    {
        _quadrantService = quadrantService;
        Console.WriteLine("[ScreenshotService] Monitoreo de actividad de cuadrantes habilitado");
    }

    public void Dispose()
    {
        StopAutomaticCaptureAsync().Wait();
        _captureTimer?.Dispose();
    }
}