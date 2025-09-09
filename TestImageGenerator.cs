using System.Drawing;
using System.Drawing.Imaging;

namespace Capturer.Testing;

public static class TestImageGenerator
{
    public static void CreateTestImages()
    {
        var baseFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Capturer", "Screenshots");
        Directory.CreateDirectory(baseFolder);
        
        // Crear imágenes con diferentes resoluciones
        var resolutions = new[]
        {
            new Size(1920, 1080), // Full HD
            new Size(1366, 768),  // Common laptop
            new Size(1440, 900),  // MacBook
            new Size(2560, 1440), // 2K
            new Size(1680, 1050)  // 16:10
        };
        
        var now = DateTime.Now;
        
        for (int i = 0; i < resolutions.Length; i++)
        {
            var resolution = resolutions[i];
            var fileName = now.AddMinutes(-i * 30).ToString("yyyy-MM-dd_HH-mm-ss") + ".png";
            var filePath = Path.Combine(baseFolder, fileName);
            
            CreateTestImage(resolution, filePath, i);
            Console.WriteLine($"Created test image: {fileName} ({resolution.Width}x{resolution.Height})");
        }
        
        // Crear algunas imágenes adicionales con la resolución más común
        var commonResolution = new Size(1920, 1080);
        for (int i = 0; i < 3; i++)
        {
            var fileName = now.AddMinutes(-((resolutions.Length + i) * 30)).ToString("yyyy-MM-dd_HH-mm-ss") + ".png";
            var filePath = Path.Combine(baseFolder, fileName);
            CreateTestImage(commonResolution, filePath, resolutions.Length + i);
            Console.WriteLine($"Created additional test image: {fileName} ({commonResolution.Width}x{commonResolution.Height})");
        }
    }
    
    private static void CreateTestImage(Size resolution, string filePath, int index)
    {
        using var bitmap = new Bitmap(resolution.Width, resolution.Height, PixelFormat.Format24bppRgb);
        using var graphics = Graphics.FromImage(bitmap);
        
        // Fondo con gradiente
        var colors = new[]
        {
            Color.LightBlue,
            Color.LightGreen,
            Color.LightCoral,
            Color.LightYellow,
            Color.LightPink
        };
        
        var bgColor = colors[index % colors.Length];
        graphics.Clear(bgColor);
        
        // Cuadrícula para identificar cuadrantes fácilmente
        using var gridPen = new Pen(Color.Black, 2);
        
        // Líneas verticales cada 25%
        for (int i = 1; i < 4; i++)
        {
            int x = resolution.Width * i / 4;
            graphics.DrawLine(gridPen, x, 0, x, resolution.Height);
        }
        
        // Líneas horizontales cada 25%
        for (int i = 1; i < 4; i++)
        {
            int y = resolution.Height * i / 4;
            graphics.DrawLine(gridPen, 0, y, resolution.Width, y);
        }
        
        // Texto informativo
        using var font = new Font("Arial", 32, FontStyle.Bold);
        using var textBrush = new SolidBrush(Color.DarkBlue);
        
        var text = $"{resolution.Width}x{resolution.Height}";
        var textSize = graphics.MeasureString(text, font);
        var textX = (resolution.Width - textSize.Width) / 2;
        var textY = (resolution.Height - textSize.Height) / 2;
        
        // Fondo blanco para el texto
        using var bgBrush = new SolidBrush(Color.White);
        graphics.FillRectangle(bgBrush, textX - 10, textY - 10, textSize.Width + 20, textSize.Height + 20);
        graphics.DrawString(text, font, textBrush, textX, textY);
        
        // Números en cada cuadrante
        using var quadrantFont = new Font("Arial", 24, FontStyle.Bold);
        using var quadrantBrush = new SolidBrush(Color.Red);
        
        var quadrantNumbers = new[] { "Q1", "Q2", "Q3", "Q4" };
        var quadrantPositions = new[]
        {
            new Point(resolution.Width / 8, resolution.Height / 8),        // Q1
            new Point(resolution.Width * 5 / 8, resolution.Height / 8),     // Q2
            new Point(resolution.Width / 8, resolution.Height * 5 / 8),     // Q3
            new Point(resolution.Width * 5 / 8, resolution.Height * 5 / 8)  // Q4
        };
        
        for (int i = 0; i < 4; i++)
        {
            graphics.DrawString(quadrantNumbers[i], quadrantFont, quadrantBrush, quadrantPositions[i]);
        }
        
        bitmap.Save(filePath, ImageFormat.Png);
    }
}

// Programa principal para generar imágenes
public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Generando imágenes de prueba...");
        TestImageGenerator.CreateTestImages();
        Console.WriteLine("Imágenes de prueba creadas exitosamente!");
        Console.WriteLine("Presione cualquier tecla para continuar...");
        Console.ReadKey();
    }
}