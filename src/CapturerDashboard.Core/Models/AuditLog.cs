using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapturerDashboard.Core.Models;

public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty; // Login, Logout, CreateUser, UpdateSettings, etc.
    
    [Required]
    [MaxLength(100)]
    public string EntityType { get; set; } = string.Empty; // User, Computer, Organization, etc.
    
    public Guid? EntityId { get; set; }
    
    [Column(TypeName = "text")]
    public string Details { get; set; } = string.Empty;
    
    [MaxLength(45)]
    public string IpAddress { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string UserAgent { get; set; } = string.Empty;
    
    [Column(TypeName = "text")]
    public string Changes { get; set; } = "{}"; // JSON of before/after values
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Foreign keys
    public Guid UserId { get; set; }
    
    public Guid OrganizationId { get; set; }
    
    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;
    
    [ForeignKey(nameof(OrganizationId))]
    public virtual Organization Organization { get; set; } = null!;
    
    // Computed properties
    [NotMapped]
    public string ActionIcon => Action switch
    {
        "Login" => "🔐",
        "Logout" => "🚪",
        "Create" => "➕",
        "Update" => "✏️",
        "Delete" => "🗑️",
        "View" => "👁️",
        _ => "📋"
    };
}