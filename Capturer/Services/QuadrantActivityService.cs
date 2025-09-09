using System.Drawing;
using System.Drawing.Imaging;
using Capturer.Models;

namespace Capturer.Services;

/// <summary>
/// Servicio para monitorear actividad en cuadrantes comparando pixel por pixel
/// </summary>
public class QuadrantActivityService : IDisposable
{
    private readonly Dictionary<string, Bitmap> _lastQuadrantImages = new();
    private readonly Dictionary<string, QuadrantActivityStats> _activityStats = new();
    private readonly object _lockObject = new object();

    public event EventHandler<QuadrantActivityChangedEventArgs>? ActivityChanged;

    /// <summary>
    /// Configuración de sensibilidad para la detección de cambios
    /// </summary>
    public QuadrantActivityConfiguration Configuration { get; set; } = new();

    /// <summary>
    /// Compara un cuadrante actual con el anterior y determina si hay actividad
    /// </summary>
    /// <param name="currentImage">Imagen actual del cuadrante</param>
    /// <param name="quadrantName">Nombre del cuadrante</param>
    /// <returns>Resultado de la comparación con porcentaje de cambio</returns>
    public QuadrantActivityResult CompareQuadrant(Bitmap currentImage, string quadrantName)
    {
        if (currentImage == null)
            throw new ArgumentNullException(nameof(currentImage));

        lock (_lockObject)
        {
            // Inicializar estadísticas si es la primera vez
            if (!_activityStats.ContainsKey(quadrantName))
            {
                _activityStats[quadrantName] = new QuadrantActivityStats(quadrantName);
            }

            var stats = _activityStats[quadrantName];
            var result = new QuadrantActivityResult
            {
                QuadrantName = quadrantName,
                Timestamp = DateTime.Now,
                IsFirstCapture = !_lastQuadrantImages.ContainsKey(quadrantName)
            };

            // Si es la primera captura, solo guardamos la imagen
            if (result.IsFirstCapture)
            {
                _lastQuadrantImages[quadrantName] = new Bitmap(currentImage);
                result.ChangePercentage = 0.0;
                result.HasActivity = false;
                
                stats.TotalComparisons = 1;
                stats.LastActivityTime = DateTime.Now;
                
                return result;
            }

            // Comparar con la imagen anterior
            var previousImage = _lastQuadrantImages[quadrantName];
            var changePercentage = CompareImagesPixelByPixel(previousImage, currentImage);
            
            result.ChangePercentage = changePercentage;
            result.HasActivity = changePercentage >= Configuration.ActivityThresholdPercentage;

            // Actualizar estadísticas
            stats.TotalComparisons++;
            if (result.HasActivity)
            {
                stats.ActivityDetectionCount++;
                stats.LastActivityTime = DateTime.Now;
                stats.TotalChangePercentage += changePercentage;
            }

            // Reemplazar imagen anterior con la actual
            previousImage.Dispose();
            _lastQuadrantImages[quadrantName] = new Bitmap(currentImage);

            // Disparar evento de cambio de actividad
            ActivityChanged?.Invoke(this, new QuadrantActivityChangedEventArgs
            {
                Result = result,
                Stats = stats
            });

            Console.WriteLine($"[QuadrantActivity] {quadrantName}: {changePercentage:F2}% cambio, Actividad: {result.HasActivity}");

            return result;
        }
    }

    /// <summary>
    /// Compara dos imágenes pixel por pixel y retorna el porcentaje de diferencia
    /// </summary>
    private unsafe double CompareImagesPixelByPixel(Bitmap image1, Bitmap image2)
    {
        if (image1.Width != image2.Width || image1.Height != image2.Height)
        {
            Console.WriteLine($"[QuadrantActivity] Tamaños diferentes: {image1.Width}x{image1.Height} vs {image2.Width}x{image2.Height}");
            return 100.0; // Consideramos 100% diferente si los tamaños no coinciden
        }

        var width = image1.Width;
        var height = image1.Height;
        var totalPixels = width * height;
        var differentPixels = 0;

        // Bloquear las imágenes para acceso directo a memoria
        var rect = new Rectangle(0, 0, width, height);
        var bmpData1 = image1.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
        var bmpData2 = image2.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

        try
        {
            var ptr1 = (byte*)bmpData1.Scan0;
            var ptr2 = (byte*)bmpData2.Scan0;
            var stride1 = bmpData1.Stride;
            var stride2 = bmpData2.Stride;

            // Comparar pixel por pixel
            for (int y = 0; y < height; y++)
            {
                var row1 = ptr1 + (y * stride1);
                var row2 = ptr2 + (y * stride2);

                for (int x = 0; x < width; x++)
                {
                    var offset = x * 3; // 3 bytes por pixel (BGR)
                    
                    var b1 = row1[offset];
                    var g1 = row1[offset + 1];
                    var r1 = row1[offset + 2];
                    
                    var b2 = row2[offset];
                    var g2 = row2[offset + 1];
                    var r2 = row2[offset + 2];

                    // Calcular diferencia considerando el threshold de tolerancia
                    var diffR = Math.Abs(r1 - r2);
                    var diffG = Math.Abs(g1 - g2);
                    var diffB = Math.Abs(b1 - b2);
                    
                    // Si cualquier componente supera la tolerancia, consideramos el pixel como diferente
                    if (diffR > Configuration.PixelToleranceThreshold || 
                        diffG > Configuration.PixelToleranceThreshold || 
                        diffB > Configuration.PixelToleranceThreshold)
                    {
                        differentPixels++;
                    }
                }
            }
        }
        finally
        {
            image1.UnlockBits(bmpData1);
            image2.UnlockBits(bmpData2);
        }

        // Calcular porcentaje de diferencia
        var changePercentage = (double)differentPixels / totalPixels * 100.0;
        return Math.Round(changePercentage, 2);
    }

    /// <summary>
    /// Obtiene las estadísticas de actividad de todos los cuadrantes
    /// </summary>
    public Dictionary<string, QuadrantActivityStats> GetAllActivityStats()
    {
        lock (_lockObject)
        {
            return new Dictionary<string, QuadrantActivityStats>(_activityStats);
        }
    }

    /// <summary>
    /// Obtiene las estadísticas de un cuadrante específico
    /// </summary>
    public QuadrantActivityStats? GetActivityStats(string quadrantName)
    {
        lock (_lockObject)
        {
            return _activityStats.TryGetValue(quadrantName, out var stats) ? stats : null;
        }
    }

    /// <summary>
    /// Resetea las estadísticas de un cuadrante específico
    /// </summary>
    public void ResetActivityStats(string quadrantName)
    {
        lock (_lockObject)
        {
            if (_activityStats.ContainsKey(quadrantName))
            {
                _activityStats[quadrantName] = new QuadrantActivityStats(quadrantName);
            }
        }
    }

    /// <summary>
    /// Resetea todas las estadísticas
    /// </summary>
    public void ResetAllActivityStats()
    {
        lock (_lockObject)
        {
            var quadrantNames = _activityStats.Keys.ToList();
            foreach (var name in quadrantNames)
            {
                _activityStats[name] = new QuadrantActivityStats(name);
            }
        }
    }

    public void Dispose()
    {
        lock (_lockObject)
        {
            // Liberar todas las imágenes almacenadas en memoria
            foreach (var image in _lastQuadrantImages.Values)
            {
                image?.Dispose();
            }
            _lastQuadrantImages.Clear();
        }
    }
}

/// <summary>
/// Configuración para la detección de actividad en cuadrantes
/// </summary>
public class QuadrantActivityConfiguration
{
    /// <summary>
    /// Porcentaje mínimo de cambio para considerar que hay actividad (0.1% - 50%)
    /// </summary>
    public double ActivityThresholdPercentage { get; set; } = 1.0; // 1% por defecto

    /// <summary>
    /// Tolerancia por pixel para considerar un cambio (0-255)
    /// </summary>
    public int PixelToleranceThreshold { get; set; } = 10; // Tolerancia de 10 en escala RGB

    /// <summary>
    /// Validar y corregir la configuración
    /// </summary>
    public void Validate()
    {
        ActivityThresholdPercentage = Math.Max(0.1, Math.Min(50.0, ActivityThresholdPercentage));
        PixelToleranceThreshold = Math.Max(0, Math.Min(255, PixelToleranceThreshold));
    }
}

/// <summary>
/// Resultado de la comparación de actividad de un cuadrante
/// </summary>
public class QuadrantActivityResult
{
    public string QuadrantName { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public bool HasActivity { get; set; }
    public double ChangePercentage { get; set; }
    public bool IsFirstCapture { get; set; }
}

/// <summary>
/// Estadísticas de actividad de un cuadrante
/// </summary>
public class QuadrantActivityStats
{
    public string QuadrantName { get; set; }
    public int TotalComparisons { get; set; }
    public int ActivityDetectionCount { get; set; }
    public DateTime LastActivityTime { get; set; }
    public double TotalChangePercentage { get; set; }
    public DateTime SessionStartTime { get; set; }

    public QuadrantActivityStats(string quadrantName)
    {
        QuadrantName = quadrantName;
        SessionStartTime = DateTime.Now;
        LastActivityTime = DateTime.Now;
    }

    /// <summary>
    /// Porcentaje de tiempo con actividad
    /// </summary>
    public double ActivityRate => TotalComparisons > 0 ? (double)ActivityDetectionCount / TotalComparisons * 100.0 : 0.0;

    /// <summary>
    /// Promedio de cambio cuando hay actividad
    /// </summary>
    public double AverageChangePercentage => ActivityDetectionCount > 0 ? TotalChangePercentage / ActivityDetectionCount : 0.0;

    /// <summary>
    /// Tiempo transcurrido desde la última actividad
    /// </summary>
    public TimeSpan TimeSinceLastActivity => DateTime.Now - LastActivityTime;

    /// <summary>
    /// Duración de la sesión
    /// </summary>
    public TimeSpan SessionDuration => DateTime.Now - SessionStartTime;
}

/// <summary>
/// Argumentos del evento de cambio de actividad
/// </summary>
public class QuadrantActivityChangedEventArgs : EventArgs
{
    public QuadrantActivityResult Result { get; set; } = new();
    public QuadrantActivityStats Stats { get; set; } = new("");
}