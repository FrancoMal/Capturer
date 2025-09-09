using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace CapturerDashboard.Core.Models;

public class PendingComputerRegistration
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(100)]
    public string ComputerName { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? ComputerId { get; set; }
    
    [Required]
    [MaxLength(128)]
    public string Fingerprint { get; set; } = string.Empty;
    
    public string HardwareInfo { get; set; } = "{}";
    
    [MaxLength(45)] // IPv6 max length
    public string? RequestIP { get; set; }
    
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? ProcessedAt { get; set; }
    
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "PendingApproval"; // PendingApproval, Approved, Denied, Expired
    
    public string? ApprovalNotes { get; set; }
    
    // Foreign keys
    public Guid InvitationId { get; set; }
    public Guid? ProcessedByUserId { get; set; }
    public Guid? CreatedComputerId { get; set; } // Set when approved and computer created
    
    // Navigation properties
    [ForeignKey(nameof(InvitationId))]
    public virtual ComputerInvitation Invitation { get; set; } = null!;
    
    [ForeignKey(nameof(ProcessedByUserId))]
    public virtual User? ProcessedByUser { get; set; }
    
    [ForeignKey(nameof(CreatedComputerId))]
    public virtual Computer? CreatedComputer { get; set; }
    
    // Helper properties
    [NotMapped]
    public Dictionary<string, object> HardwareInfoData
    {
        get => JsonSerializer.Deserialize<Dictionary<string, object>>(HardwareInfo) ?? new();
        set => HardwareInfo = JsonSerializer.Serialize(value);
    }
    
    [NotMapped]
    public bool IsPending => Status == "PendingApproval";
    
    [NotMapped]
    public bool IsExpired => Status == "Expired" || (RequestedAt.AddHours(24) < DateTime.UtcNow && IsPending);
    
    [NotMapped]
    public string StatusDisplay => Status switch
    {
        "PendingApproval" => "⏳ Pending Approval",
        "Approved" => "✅ Approved", 
        "Denied" => "❌ Denied",
        "Expired" => "⏰ Expired",
        _ => "❓ Unknown"
    };
}