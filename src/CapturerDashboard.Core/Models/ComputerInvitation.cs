using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapturerDashboard.Core.Models;

public class ComputerInvitation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(128)]
    public string Token { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(256)]
    public string Signature { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime ExpiresAt { get; set; }
    
    public int MaxUses { get; set; } = 1;
    
    public int UsedCount { get; set; } = 0;
    
    public bool IsActive { get; set; } = true;
    
    public DateTime? LastUsedAt { get; set; }
    
    [MaxLength(100)]
    public string? AllowedNetworks { get; set; } // Comma-separated IP ranges
    
    public bool RequireApproval { get; set; } = true;
    
    // Foreign keys
    public Guid OrganizationId { get; set; }
    public Guid CreatedByUserId { get; set; }
    
    // Navigation properties
    [ForeignKey(nameof(OrganizationId))]
    public virtual Organization Organization { get; set; } = null!;
    
    [ForeignKey(nameof(CreatedByUserId))]
    public virtual User CreatedByUser { get; set; } = null!;
    
    public virtual ICollection<PendingComputerRegistration> PendingRegistrations { get; set; } = new List<PendingComputerRegistration>();
    
    // Helper properties
    [NotMapped]
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    
    [NotMapped]
    public bool IsMaxUsesReached => UsedCount >= MaxUses;
    
    [NotMapped]
    public bool IsUsable => IsActive && !IsExpired && !IsMaxUsesReached;
    
    [NotMapped]
    public TimeSpan TimeUntilExpiry => IsExpired ? TimeSpan.Zero : ExpiresAt - DateTime.UtcNow;
}