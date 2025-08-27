using System.Drawing;

namespace Capturer.Models;

public class QuadrantConfiguration
{
    public string Name { get; set; } = "Default";
    public string Description { get; set; } = "";
    public Size ScreenResolution { get; set; }
    public List<QuadrantRegion> Quadrants { get; set; } = new();
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime LastModified { get; set; } = DateTime.Now;
    public bool IsActive { get; set; } = true;

    public QuadrantConfiguration() { }

    public QuadrantConfiguration(string name, Size screenResolution)
    {
        Name = name;
        ScreenResolution = screenResolution;
    }

    /// <summary>
    /// Adds a new quadrant to the configuration
    /// </summary>
    public void AddQuadrant(QuadrantRegion quadrant)
    {
        if (quadrant != null && quadrant.IsValidForScreen(ScreenResolution))
        {
            Quadrants.Add(quadrant);
            LastModified = DateTime.Now;
        }
    }

    /// <summary>
    /// Removes a quadrant by name
    /// </summary>
    public bool RemoveQuadrant(string name)
    {
        var quadrant = Quadrants.FirstOrDefault(q => q.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (quadrant != null)
        {
            Quadrants.Remove(quadrant);
            LastModified = DateTime.Now;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets all enabled quadrants
    /// </summary>
    public List<QuadrantRegion> GetEnabledQuadrants()
    {
        return Quadrants.Where(q => q.IsEnabled).ToList();
    }

    /// <summary>
    /// Validates the entire configuration
    /// </summary>
    public bool IsValid()
    {
        return Quadrants.Count > 0 && 
               Quadrants.All(q => q.IsValidForScreen(ScreenResolution)) &&
               Quadrants.Select(q => q.Name).Distinct().Count() == Quadrants.Count; // Unique names
    }

    /// <summary>
    /// Creates a 2x2 default configuration
    /// </summary>
    public static QuadrantConfiguration CreateDefault(Size screenResolution)
    {
        var config = new QuadrantConfiguration("2x2 Grid", screenResolution);
        
        var halfWidth = screenResolution.Width / 2;
        var halfHeight = screenResolution.Height / 2;

        config.AddQuadrant(new QuadrantRegion("TopLeft", 
            new Rectangle(0, 0, halfWidth, halfHeight), Color.Red));
        config.AddQuadrant(new QuadrantRegion("TopRight", 
            new Rectangle(halfWidth, 0, halfWidth, halfHeight), Color.Green));
        config.AddQuadrant(new QuadrantRegion("BottomLeft", 
            new Rectangle(0, halfHeight, halfWidth, halfHeight), Color.Blue));
        config.AddQuadrant(new QuadrantRegion("BottomRight", 
            new Rectangle(halfWidth, halfHeight, halfWidth, halfHeight), Color.Orange));

        return config;
    }
}