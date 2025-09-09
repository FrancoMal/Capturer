using System.ComponentModel.DataAnnotations;

namespace CapturerDashboard.Core.Models;

public class Organization
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string Slug { get; set; } = string.Empty;
    
    public int MaxComputers { get; set; } = 10;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public virtual ICollection<User> Users { get; set; } = new List<User>();
    public virtual ICollection<Computer> Computers { get; set; } = new List<Computer>();
}