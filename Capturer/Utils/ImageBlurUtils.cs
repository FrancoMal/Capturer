using System.Drawing;
using System.Drawing.Imaging;
using Capturer.Models;

namespace Capturer.Utils;

/// <summary>
/// Utility class for applying blur effects to screenshots for privacy protection
/// </summary>
public static class ImageBlurUtils
{
    /// <summary>
    /// Applies blur effect to a bitmap based on the specified settings
    /// </summary>
    /// <param name="source">Source bitmap to blur</param>
    /// <param name="intensity">Blur intensity (1-10 scale)</param>
    /// <param name="mode">Blur mode to apply</param>
    /// <returns>New blurred bitmap</returns>
    public static Bitmap ApplyBlur(Bitmap source, int intensity, BlurMode mode)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        
        if (intensity < 1 || intensity > 10)
            throw new ArgumentOutOfRangeException(nameof(intensity), "Blur intensity must be between 1 and 10");

        // Clamp intensity to valid range
        intensity = Math.Max(1, Math.Min(10, intensity));
        
        return mode switch
        {
            BlurMode.Gaussian => ApplyGaussianBlur(source, intensity),
            BlurMode.Box => ApplyBoxBlur(source, intensity),
            BlurMode.Motion => ApplyMotionBlur(source, intensity),
            _ => ApplyGaussianBlur(source, intensity)
        };
    }

    /// <summary>
    /// Applies Gaussian blur - best quality but slower
    /// </summary>
    private static Bitmap ApplyGaussianBlur(Bitmap source, int intensity)
    {
        var result = new Bitmap(source.Width, source.Height, PixelFormat.Format24bppRgb);
        
        // Convert intensity (1-10) to radius (1-15)
        var radius = Math.Max(1, intensity * 1.5);
        
        var rect = new Rectangle(0, 0, source.Width, source.Height);
        var srcData = source.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
        var destData = result.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
        
        try
        {
            unsafe
            {
                var srcPtr = (byte*)srcData.Scan0;
                var destPtr = (byte*)destData.Scan0;
                
                ApplyGaussianBlurUnsafe(srcPtr, destPtr, source.Width, source.Height, 
                                      srcData.Stride, destData.Stride, (int)radius);
            }
        }
        finally
        {
            source.UnlockBits(srcData);
            result.UnlockBits(destData);
        }
        
        return result;
    }

    /// <summary>
    /// Applies box blur - faster but lower quality
    /// </summary>
    private static Bitmap ApplyBoxBlur(Bitmap source, int intensity)
    {
        var result = new Bitmap(source.Width, source.Height, PixelFormat.Format24bppRgb);
        
        // Convert intensity (1-10) to kernel size
        var kernelSize = Math.Max(3, intensity * 2 + 1);
        if (kernelSize % 2 == 0) kernelSize++; // Ensure odd number
        
        using (var graphics = Graphics.FromImage(result))
        {
            graphics.DrawImage(source, 0, 0);
        }
        
        // Apply simple box blur
        for (int pass = 0; pass < Math.Max(1, intensity / 3); pass++)
        {
            result = ApplySimpleBoxBlur(result, kernelSize);
        }
        
        return result;
    }

    /// <summary>
    /// Applies motion blur - simulates camera movement for privacy
    /// </summary>
    private static Bitmap ApplyMotionBlur(Bitmap source, int intensity)
    {
        var result = new Bitmap(source.Width, source.Height, PixelFormat.Format24bppRgb);
        using (var graphics = Graphics.FromImage(result))
        {
            graphics.DrawImage(source, 0, 0);
            
            // Apply horizontal motion blur effect
            var distance = Math.Max(2, intensity * 3);
            var alpha = Math.Max(20, 255 - intensity * 20);
            
            for (int i = 1; i <= distance; i++)
            {
                var colorMatrix = new ColorMatrix();
                colorMatrix.Matrix33 = alpha / 255f / distance; // Transparency
                
                var imageAttributes = new ImageAttributes();
                imageAttributes.SetColorMatrix(colorMatrix);
                
                graphics.DrawImage(source, new Rectangle(i, 0, source.Width - i, source.Height),
                                 0, 0, source.Width - i, source.Height,
                                 GraphicsUnit.Pixel, imageAttributes);
            }
        }
        
        return result;
    }

    /// <summary>
    /// Simple box blur implementation
    /// </summary>
    private static Bitmap ApplySimpleBoxBlur(Bitmap source, int kernelSize)
    {
        var result = new Bitmap(source.Width, source.Height, PixelFormat.Format24bppRgb);
        var radius = kernelSize / 2;
        
        var rect = new Rectangle(0, 0, source.Width, source.Height);
        var srcData = source.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
        var destData = result.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
        
        try
        {
            unsafe
            {
                var srcPtr = (byte*)srcData.Scan0;
                var destPtr = (byte*)destData.Scan0;
                
                for (int y = radius; y < source.Height - radius; y++)
                {
                    for (int x = radius; x < source.Width - radius; x++)
                    {
                        long totalR = 0, totalG = 0, totalB = 0;
                        int count = 0;
                        
                        for (int ky = -radius; ky <= radius; ky++)
                        {
                            for (int kx = -radius; kx <= radius; kx++)
                            {
                                var pixelPtr = srcPtr + (y + ky) * srcData.Stride + (x + kx) * 3;
                                totalB += pixelPtr[0];
                                totalG += pixelPtr[1];
                                totalR += pixelPtr[2];
                                count++;
                            }
                        }
                        
                        var destPixelPtr = destPtr + y * destData.Stride + x * 3;
                        destPixelPtr[0] = (byte)(totalB / count);
                        destPixelPtr[1] = (byte)(totalG / count);
                        destPixelPtr[2] = (byte)(totalR / count);
                    }
                }
            }
        }
        finally
        {
            source.UnlockBits(srcData);
            result.UnlockBits(destData);
        }
        
        return result;
    }

    /// <summary>
    /// Unsafe Gaussian blur implementation for better performance
    /// </summary>
    private static unsafe void ApplyGaussianBlurUnsafe(byte* src, byte* dest, int width, int height, 
                                                      int srcStride, int destStride, int radius)
    {
        // Simplified Gaussian blur - for production, consider using a proper Gaussian kernel
        var kernelSize = radius * 2 + 1;
        var divisor = kernelSize * kernelSize;
        
        for (int y = radius; y < height - radius; y++)
        {
            for (int x = radius; x < width - radius; x++)
            {
                long totalR = 0, totalG = 0, totalB = 0;
                
                for (int ky = -radius; ky <= radius; ky++)
                {
                    for (int kx = -radius; kx <= radius; kx++)
                    {
                        var pixelPtr = src + (y + ky) * srcStride + (x + kx) * 3;
                        totalB += pixelPtr[0];
                        totalG += pixelPtr[1];
                        totalR += pixelPtr[2];
                    }
                }
                
                var destPixelPtr = dest + y * destStride + x * 3;
                destPixelPtr[0] = (byte)(totalB / divisor);
                destPixelPtr[1] = (byte)(totalG / divisor);
                destPixelPtr[2] = (byte)(totalR / divisor);
            }
        }
    }

    /// <summary>
    /// Validates blur settings and provides recommendations
    /// </summary>
    /// <param name="intensity">Blur intensity to validate</param>
    /// <param name="mode">Blur mode to validate</param>
    /// <returns>Tuple with validated values and performance warning</returns>
    public static (int validatedIntensity, BlurMode validatedMode, string? performanceWarning) ValidateBlurSettings(
        int intensity, BlurMode mode)
    {
        var validatedIntensity = Math.Max(1, Math.Min(10, intensity));
        var validatedMode = Enum.IsDefined(typeof(BlurMode), mode) ? mode : BlurMode.Gaussian;
        string? warning = null;
        
        if (validatedIntensity >= 7 && validatedMode == BlurMode.Gaussian)
        {
            warning = "High intensity Gaussian blur may impact screenshot capture performance. Consider Box blur for better performance.";
        }
        else if (validatedIntensity >= 9)
        {
            warning = "Very high blur intensity may significantly impact performance and file sizes.";
        }
        
        return (validatedIntensity, validatedMode, warning);
    }
}