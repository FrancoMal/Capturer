using System.Drawing;

namespace Capturer.Models;

public class QuadrantRegion
{
    public string Name { get; set; } = "";
    public Rectangle Bounds { get; set; }
    public Color PreviewColor { get; set; } = Color.Blue;
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public string Description { get; set; } = "";

    public QuadrantRegion() { }

    public QuadrantRegion(string name, Rectangle bounds, Color color)
    {
        Name = name;
        Bounds = bounds;
        PreviewColor = color;
    }

    /// <summary>
    /// Gets the folder name for this quadrant (sanitized for file system)
    /// </summary>
    public string GetFolderName()
    {
        // Sanitize name for file system
        var safeName = Name;
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            safeName = safeName.Replace(c, '_');
        }
        return string.IsNullOrWhiteSpace(safeName) ? $"Quadrant_{Bounds.X}_{Bounds.Y}" : safeName;
    }

    /// <summary>
    /// Validates if the region bounds are within screen bounds
    /// </summary>
    public bool IsValidForScreen(Size screenSize)
    {
        return Bounds.X >= 0 && 
               Bounds.Y >= 0 && 
               Bounds.Right <= screenSize.Width && 
               Bounds.Bottom <= screenSize.Height &&
               Bounds.Width > 0 && 
               Bounds.Height > 0;
    }

    /// <summary>
    /// Gets display string for UI
    /// </summary>
    public override string ToString()
    {
        return $"{Name} ({Bounds.Width}x{Bounds.Height})";
    }
}