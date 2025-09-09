using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace CapturerDashboard.Core.Models;

public class QuadrantActivity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(100)]
    public string QuadrantName { get; set; } = string.Empty;
    
    public long TotalComparisons { get; set; }
    
    public long ActivityDetectionCount { get; set; }
    
    [Column(TypeName = "decimal(5,2)")]
    public decimal ActivityRate { get; set; }
    
    [Column(TypeName = "text")]
    public string Timeline { get; set; } = "[]"; // JSON array of timeline data
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Foreign key
    public Guid ReportId { get; set; }
    
    // Navigation properties
    [ForeignKey(nameof(ReportId))]
    public virtual ActivityReport Report { get; set; } = null!;
    
    // Helper methods for JSON handling
    [NotMapped]
    public List<TimelinePoint> TimelineData
    {
        get
        {
            try
            {
                return JsonSerializer.Deserialize<List<TimelinePoint>>(Timeline) ?? new List<TimelinePoint>();
            }
            catch
            {
                return new List<TimelinePoint>();
            }
        }
        set => Timeline = JsonSerializer.Serialize(value);
    }
    
    // Computed properties
    [NotMapped]
    public double ActivityPercentage => (double)ActivityRate;
    
    [NotMapped]
    public bool IsHighActivity => ActivityRate >= 80;
    
    [NotMapped]
    public bool IsLowActivity => ActivityRate < 30;
}

public class TimelinePoint
{
    public DateTime Timestamp { get; set; }
    public double ActivityLevel { get; set; }
    public int Screenshots { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}