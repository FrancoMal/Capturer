using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapturerDashboard.Core.Models;

public class Alert
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(20)]
    public string Type { get; set; } = string.Empty; // ComputerOffline, LowActivity, HighActivity, SystemError
    
    [Required]
    [MaxLength(20)]
    public string Severity { get; set; } = string.Empty; // Info, Warning, Critical
    
    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;
    
    [Column(TypeName = "text")]
    public string Description { get; set; } = string.Empty;
    
    [Column(TypeName = "text")]
    public string Metadata { get; set; } = "{}"; // JSON for additional alert data
    
    public bool IsAcknowledged { get; set; } = false;
    
    public Guid? AcknowledgedBy { get; set; }
    
    public DateTime? AcknowledgedAt { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? ResolvedAt { get; set; }
    
    // Foreign keys
    public Guid ComputerId { get; set; }
    
    public Guid OrganizationId { get; set; }
    
    // Navigation properties
    [ForeignKey(nameof(ComputerId))]
    public virtual Computer Computer { get; set; } = null!;
    
    [ForeignKey(nameof(OrganizationId))]
    public virtual Organization Organization { get; set; } = null!;
    
    [ForeignKey(nameof(AcknowledgedBy))]
    public virtual User? AcknowledgedByUser { get; set; }
    
    // Computed properties
    [NotMapped]
    public bool IsResolved => ResolvedAt.HasValue;
    
    [NotMapped]
    public TimeSpan? Duration => ResolvedAt.HasValue ? ResolvedAt - CreatedAt : DateTime.UtcNow - CreatedAt;
    
    [NotMapped]
    public string SeverityIcon => Severity switch
    {
        "Critical" => "🚨",
        "Warning" => "⚠️",
        "Info" => "ℹ️",
        _ => "📋"
    };
}