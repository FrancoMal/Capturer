using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace CapturerDashboard.Core.Models;

public class Computer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(100)]
    public string ComputerId { get; set; } = string.Empty; // Hardware-based unique ID
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(255)]
    public string ApiKey { get; set; } = string.Empty;
    
    public string HardwareInfo { get; set; } = "{}"; // JSON string
    
    public DateTime? LastSeenAt { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    [MaxLength(20)]
    public string Status { get; set; } = "Offline"; // Online, Offline, Warning
    
    // Foreign key
    public Guid OrganizationId { get; set; }
    
    // Navigation properties
    [ForeignKey(nameof(OrganizationId))]
    public virtual Organization Organization { get; set; } = null!;
    
    public virtual ICollection<ActivityReport> ActivityReports { get; set; } = new List<ActivityReport>();
    public virtual ICollection<Alert> Alerts { get; set; } = new List<Alert>();
    
    // Helper methods for JSON handling
    [NotMapped]
    public Dictionary<string, object> HardwareInfoData
    {
        get => JsonSerializer.Deserialize<Dictionary<string, object>>(HardwareInfo) ?? new();
        set => HardwareInfo = JsonSerializer.Serialize(value);
    }
    
    // Computed properties
    [NotMapped]
    public bool IsOnline => LastSeenAt.HasValue && LastSeenAt > DateTime.UtcNow.AddMinutes(-5);
    
    [NotMapped]
    public string StatusDisplay => IsOnline ? "Online" : "Offline";
}