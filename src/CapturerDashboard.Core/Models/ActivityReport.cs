using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace CapturerDashboard.Core.Models;

public class ActivityReport
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public DateOnly ReportDate { get; set; }
    
    public TimeOnly StartTime { get; set; }
    
    public TimeOnly EndTime { get; set; }
    
    [Column(TypeName = "text")]
    public string ReportData { get; set; } = "{}"; // Full JSON report from Capturer
    
    // Aggregated data for quick queries
    public int TotalQuadrants { get; set; }
    
    public long TotalComparisons { get; set; }
    
    public long TotalActivities { get; set; }
    
    [Column(TypeName = "decimal(5,2)")]
    public decimal AverageActivityRate { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Foreign key
    public Guid ComputerId { get; set; }
    
    // Navigation properties
    [ForeignKey(nameof(ComputerId))]
    public virtual Computer Computer { get; set; } = null!;
    
    public virtual ICollection<QuadrantActivity> Quadrants { get; set; } = new List<QuadrantActivity>();
    
    // Helper methods for JSON handling
    [NotMapped]
    public ActivityReportDto? ReportDataObject
    {
        get
        {
            try
            {
                return JsonSerializer.Deserialize<ActivityReportDto>(ReportData);
            }
            catch
            {
                return null;
            }
        }
        set => ReportData = value != null ? JsonSerializer.Serialize(value) : "{}";
    }
    
    // Computed properties
    [NotMapped]
    public TimeSpan Duration => EndTime.ToTimeSpan() - StartTime.ToTimeSpan();
    
    [NotMapped]
    public double DurationHours => Duration.TotalHours;
}

// DTO for JSON serialization (matches the format from Integration Guide)
public class ActivityReportDto
{
    public string Version { get; set; } = "4.0.0";
    public DateTime Timestamp { get; set; }
    public string ComputerId { get; set; } = string.Empty;
    public string ComputerName { get; set; } = string.Empty;
    public ReportDto Report { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class ReportDto
{
    public string Date { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public List<QuadrantDto> Quadrants { get; set; } = new();
}

public class QuadrantDto
{
    public string Name { get; set; } = string.Empty;
    public long Comparisons { get; set; }
    public long Activities { get; set; }
    public double ActivityRate { get; set; }
    public List<object> Timeline { get; set; } = new();
}