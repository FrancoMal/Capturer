using System.Drawing;
using System.Drawing.Imaging;
using Capturer.Models;

namespace Capturer.Utils;

public static class ImageCropUtils
{
    /// <summary>
    /// Crops an image to a specific quadrant region
    /// </summary>
    public static Bitmap? CropImage(Bitmap source, QuadrantRegion quadrant)
    {
        if (source == null || !quadrant.IsValidForScreen(source.Size))
            return null;

        try
        {
            var cropRect = quadrant.Bounds;
            
            // Ensure crop rectangle is within source bounds
            cropRect = Rectangle.Intersect(cropRect, new Rectangle(0, 0, source.Width, source.Height));
            
            if (cropRect.IsEmpty)
                return null;

            var croppedImage = new Bitmap(cropRect.Width, cropRect.Height, PixelFormat.Format32bppArgb);
            
            using (var graphics = Graphics.FromImage(croppedImage))
            {
                graphics.DrawImage(source, 
                    new Rectangle(0, 0, cropRect.Width, cropRect.Height),
                    cropRect, 
                    GraphicsUnit.Pixel);
            }
            
            return croppedImage;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error cropping image for quadrant {quadrant.Name}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Crops image from file path
    /// </summary>
    public static Bitmap? CropImageFromFile(string imagePath, QuadrantRegion quadrant)
    {
        if (!File.Exists(imagePath))
            return null;

        try
        {
            using var sourceImage = new Bitmap(imagePath);
            return CropImage(sourceImage, quadrant);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading image from {imagePath}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Saves a cropped image with proper naming
    /// </summary>
    public static string? SaveCroppedImage(Bitmap croppedImage, string originalFileName, QuadrantRegion quadrant, string outputFolder)
    {
        if (croppedImage == null)
            return null;

        try
        {
            // Create output directory if it doesn't exist
            Directory.CreateDirectory(outputFolder);
            
            // Generate new filename: originalName_QuadrantName.extension
            var originalName = Path.GetFileNameWithoutExtension(originalFileName);
            var extension = Path.GetExtension(originalFileName);
            var quadrantFolderName = quadrant.GetFolderName();
            var newFileName = $"{originalName}_{quadrantFolderName}{extension}";
            var outputPath = Path.Combine(outputFolder, newFileName);

            // Check if file already exists
            if (File.Exists(outputPath))
            {
                return null; // Skip existing files
            }

            // Save with appropriate format
            var format = GetImageFormat(extension);
            if (format == ImageFormat.Jpeg)
            {
                SaveJpegWithQuality(croppedImage, outputPath, 90);
            }
            else
            {
                croppedImage.Save(outputPath, format);
            }

            return outputPath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving cropped image: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Batch processes multiple images for all quadrants
    /// </summary>
    public static async Task<ProcessingTask> ProcessImagesAsync(
        List<string> imagePaths, 
        QuadrantConfiguration configuration,
        string baseOutputFolder,
        IProgress<ProcessingProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[ImageCropUtils] Iniciando procesamiento de {imagePaths.Count} imágenes");
        
        var task = new ProcessingTask(DateTime.Now.AddDays(-30), DateTime.Now, configuration.Name);
        var enabledQuadrants = configuration.GetEnabledQuadrants();
        task.TotalFiles = imagePaths.Count * enabledQuadrants.Count;
        task.Status = ProcessingStatus.Running;

        Console.WriteLine($"[ImageCropUtils] Cuadrantes activos: {enabledQuadrants.Count}");
        Console.WriteLine($"[ImageCropUtils] Total operaciones: {task.TotalFiles}");

        try
        {
            var processedFiles = 0;
            var imageIndex = 0;

            foreach (var imagePath in imagePaths)
            {
                imageIndex++;
                
                if (cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine("[ImageCropUtils] Cancelación solicitada");
                    task.WasCancelled = true;
                    break;
                }

                var originalFileName = Path.GetFileName(imagePath);
                Console.WriteLine($"[ImageCropUtils] Procesando imagen {imageIndex}/{imagePaths.Count}: {originalFileName}");
                
                // Check if file exists and is accessible
                if (!File.Exists(imagePath))
                {
                    Console.WriteLine($"[ImageCropUtils] ERROR: Archivo no existe: {imagePath}");
                    task.AddError(originalFileName, "Archivo no encontrado");
                    continue;
                }

                try
                {
                    // Report current file processing
                    progress?.Report(new ProcessingProgress
                    {
                        CurrentFile = processedFiles,
                        TotalFiles = task.TotalFiles,
                        CurrentFileName = originalFileName,
                        CurrentOperation = $"Cargando imagen ({imageIndex}/{imagePaths.Count})..."
                    });

                    using var sourceImage = new Bitmap(imagePath);
                    Console.WriteLine($"[ImageCropUtils] Imagen cargada: {sourceImage.Width}x{sourceImage.Height}");

                    var quadrantIndex = 0;
                    foreach (var quadrant in enabledQuadrants)
                    {
                        quadrantIndex++;
                        
                        if (cancellationToken.IsCancellationRequested)
                        {
                            Console.WriteLine("[ImageCropUtils] Cancelación solicitada durante procesamiento de cuadrante");
                            task.WasCancelled = true;
                            break;
                        }

                        try
                        {
                            Console.WriteLine($"[ImageCropUtils] Procesando cuadrante {quadrantIndex}/{enabledQuadrants.Count}: {quadrant.Name}");
                            
                            // Report quadrant processing
                            progress?.Report(new ProcessingProgress
                            {
                                CurrentFile = processedFiles,
                                TotalFiles = task.TotalFiles,
                                CurrentFileName = originalFileName,
                                CurrentOperation = $"Procesando cuadrante {quadrant.Name} ({quadrantIndex}/{enabledQuadrants.Count})"
                            });

                            var quadrantFolder = Path.Combine(baseOutputFolder, quadrant.GetFolderName());
                            Console.WriteLine($"[ImageCropUtils] Carpeta destino: {quadrantFolder}");
                            
                            // Create quadrant folder if needed
                            Directory.CreateDirectory(quadrantFolder);
                            
                            using var croppedImage = CropImage(sourceImage, quadrant);
                            
                            if (croppedImage == null)
                            {
                                Console.WriteLine($"[ImageCropUtils] ERROR: No se pudo recortar la imagen para {quadrant.Name}");
                                task.AddError(originalFileName, $"No se pudo recortar {quadrant.Name}");
                            }
                            else
                            {
                                var savedPath = SaveCroppedImage(croppedImage, originalFileName, quadrant, quadrantFolder);
                                
                                if (savedPath == null)
                                {
                                    Console.WriteLine($"[ImageCropUtils] SKIP: Ya existe recorte para {quadrant.Name}");
                                    task.SkipFile(originalFileName, $"Ya existe recorte para {quadrant.Name}");
                                }
                                else
                                {
                                    Console.WriteLine($"[ImageCropUtils] GUARDADO: {savedPath}");
                                    task.ProcessFile();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[ImageCropUtils] ERROR procesando cuadrante {quadrant.Name}: {ex.Message}");
                            task.AddError(originalFileName, $"Error procesando cuadrante {quadrant.Name}: {ex.Message}");
                        }

                        processedFiles++;
                        
                        // Update progress more frequently
                        progress?.Report(new ProcessingProgress
                        {
                            CurrentFile = processedFiles,
                            TotalFiles = task.TotalFiles,
                            CurrentFileName = originalFileName,
                            CurrentOperation = $"Completado cuadrante {quadrant.Name}"
                        });
                        
                        // Small delay to keep UI responsive
                        if (processedFiles % 5 == 0)
                        {
                            await Task.Delay(1, cancellationToken);
                        }
                    }

                    if (task.WasCancelled) break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ImageCropUtils] ERROR cargando imagen {originalFileName}: {ex.Message}");
                    task.AddError(originalFileName, $"Error cargando imagen: {ex.Message}");
                }
            }

            if (task.WasCancelled)
            {
                Console.WriteLine("[ImageCropUtils] Procesamiento cancelado");
                // Don't call Complete() for cancelled tasks
            }
            else
            {
                Console.WriteLine("[ImageCropUtils] Procesamiento completado");
                task.Complete();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ImageCropUtils] ERROR CRÍTICO: {ex.Message}");
            Console.WriteLine($"[ImageCropUtils] Stack trace: {ex.StackTrace}");
            task.Fail($"Error general durante el procesamiento: {ex.Message}");
        }

        Console.WriteLine($"[ImageCropUtils] Resultado final: Status={task.Status}, Procesados={task.ProcessedFiles}, Errores={task.ErrorMessages.Count}");
        return task;
    }

    /// <summary>
    /// Gets the appropriate image format from file extension
    /// </summary>
    private static ImageFormat GetImageFormat(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => ImageFormat.Jpeg,
            ".bmp" => ImageFormat.Bmp,
            ".gif" => ImageFormat.Gif,
            ".tiff" => ImageFormat.Tiff,
            _ => ImageFormat.Png
        };
    }

    /// <summary>
    /// Saves JPEG with specific quality
    /// </summary>
    private static void SaveJpegWithQuality(Bitmap image, string path, int quality)
    {
        var encoderParameters = new EncoderParameters(1);
        encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, (long)quality);
        var jpegEncoder = ImageCodecInfo.GetImageEncoders().First(e => e.FormatID == ImageFormat.Jpeg.Guid);
        image.Save(path, jpegEncoder, encoderParameters);
    }

    /// <summary>
    /// Creates a preview image showing quadrant overlays
    /// </summary>
    public static Bitmap CreatePreviewImage(Bitmap source, QuadrantConfiguration configuration, bool showLabels = true)
    {
        var preview = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
        
        using var graphics = Graphics.FromImage(preview);
        graphics.DrawImage(source, 0, 0);
        
        var font = new Font("Arial", 12, FontStyle.Bold);
        
        foreach (var quadrant in configuration.GetEnabledQuadrants())
        {
            // Draw quadrant boundary
            using var pen = new Pen(quadrant.PreviewColor, 2);
            graphics.DrawRectangle(pen, quadrant.Bounds);
            
            // Fill with semi-transparent color
            using var brush = new SolidBrush(Color.FromArgb(30, quadrant.PreviewColor));
            graphics.FillRectangle(brush, quadrant.Bounds);
            
            if (showLabels)
            {
                // Draw label
                using var textBrush = new SolidBrush(quadrant.PreviewColor);
                var labelPos = new PointF(quadrant.Bounds.X + 5, quadrant.Bounds.Y + 5);
                graphics.DrawString(quadrant.Name, font, textBrush, labelPos);
                
                // Draw size info
                var sizeText = $"{quadrant.Bounds.Width}x{quadrant.Bounds.Height}";
                var sizePos = new PointF(quadrant.Bounds.X + 5, quadrant.Bounds.Y + 25);
                graphics.DrawString(sizeText, new Font("Arial", 9), textBrush, sizePos);
            }
        }
        
        font.Dispose();
        return preview;
    }
}