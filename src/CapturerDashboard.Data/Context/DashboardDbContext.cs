using Microsoft.EntityFrameworkCore;
using CapturerDashboard.Core.Models;

namespace CapturerDashboard.Data.Context;

public class DashboardDbContext : DbContext
{
    public DashboardDbContext(DbContextOptions<DashboardDbContext> options) : base(options)
    {
    }

    // DbSets
    public DbSet<Organization> Organizations { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Computer> Computers { get; set; } = null!;
    public DbSet<ActivityReport> ActivityReports { get; set; } = null!;
    public DbSet<QuadrantActivity> QuadrantActivities { get; set; } = null!;
    public DbSet<Alert> Alerts { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;
    public DbSet<ComputerInvitation> ComputerInvitations { get; set; } = null!;
    public DbSet<PendingComputerRegistration> PendingComputerRegistrations { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Organization
        modelBuilder.Entity<Organization>(entity =>
        {
            entity.ToTable("Organizations");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Slug).IsRequired().HasMaxLength(50);
        });

        // Configure User
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Role).IsRequired().HasMaxLength(20);

            entity.HasOne(e => e.Organization)
                  .WithMany(o => o.Users)
                  .HasForeignKey(e => e.OrganizationId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Computer
        modelBuilder.Entity<Computer>(entity =>
        {
            entity.ToTable("Computers");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ComputerId).IsUnique();
            entity.HasIndex(e => e.ApiKey).IsUnique();
            entity.Property(e => e.ComputerId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ApiKey).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Status).HasMaxLength(20);

            entity.HasOne(e => e.Organization)
                  .WithMany(o => o.Computers)
                  .HasForeignKey(e => e.OrganizationId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure ActivityReport
        modelBuilder.Entity<ActivityReport>(entity =>
        {
            entity.ToTable("ActivityReports");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ComputerId, e.ReportDate });
            entity.Property(e => e.ReportData).HasColumnType("TEXT");
            entity.Property(e => e.AverageActivityRate).HasColumnType("decimal(5,2)");

            entity.HasOne(e => e.Computer)
                  .WithMany(c => c.ActivityReports)
                  .HasForeignKey(e => e.ComputerId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure QuadrantActivity
        modelBuilder.Entity<QuadrantActivity>(entity =>
        {
            entity.ToTable("QuadrantActivities");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ReportId, e.QuadrantName });
            entity.Property(e => e.QuadrantName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ActivityRate).HasColumnType("decimal(5,2)");
            entity.Property(e => e.Timeline).HasColumnType("TEXT");

            entity.HasOne(e => e.Report)
                  .WithMany(r => r.Quadrants)
                  .HasForeignKey(e => e.ReportId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Alert
        modelBuilder.Entity<Alert>(entity =>
        {
            entity.ToTable("Alerts");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.OrganizationId, e.IsAcknowledged });
            entity.Property(e => e.Type).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Severity).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Description).HasColumnType("TEXT");
            entity.Property(e => e.Metadata).HasColumnType("TEXT");

            entity.HasOne(e => e.Computer)
                  .WithMany(c => c.Alerts)
                  .HasForeignKey(e => e.ComputerId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Organization)
                  .WithMany()
                  .HasForeignKey(e => e.OrganizationId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.AcknowledgedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.AcknowledgedBy)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure AuditLog
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.OrganizationId, e.CreatedAt });
            entity.HasIndex(e => new { e.UserId, e.CreatedAt });
            entity.Property(e => e.Action).IsRequired().HasMaxLength(50);
            entity.Property(e => e.EntityType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Details).HasColumnType("TEXT");
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.Changes).HasColumnType("TEXT");

            entity.HasOne(e => e.User)
                  .WithMany(u => u.AuditLogs)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Organization)
                  .WithMany()
                  .HasForeignKey(e => e.OrganizationId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure ComputerInvitation
        modelBuilder.Entity<ComputerInvitation>(entity =>
        {
            entity.ToTable("ComputerInvitations");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Token).IsUnique();
            entity.Property(e => e.Token).IsRequired().HasMaxLength(128);
            entity.Property(e => e.Signature).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.AllowedNetworks).HasMaxLength(100);

            entity.HasOne(e => e.Organization)
                  .WithMany()
                  .HasForeignKey(e => e.OrganizationId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.CreatedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure PendingComputerRegistration
        modelBuilder.Entity<PendingComputerRegistration>(entity =>
        {
            entity.ToTable("PendingComputerRegistrations");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Fingerprint).IsUnique();
            entity.HasIndex(e => new { e.InvitationId, e.Status });
            entity.Property(e => e.ComputerName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ComputerId).HasMaxLength(100);
            entity.Property(e => e.Fingerprint).IsRequired().HasMaxLength(128);
            entity.Property(e => e.HardwareInfo).HasColumnType("TEXT");
            entity.Property(e => e.RequestIP).HasMaxLength(45);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
            entity.Property(e => e.ApprovalNotes).HasColumnType("TEXT");

            entity.HasOne(e => e.Invitation)
                  .WithMany(i => i.PendingRegistrations)
                  .HasForeignKey(e => e.InvitationId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ProcessedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.ProcessedByUserId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.CreatedComputer)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedComputerId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Seed data
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // Seed default organization
        var defaultOrgId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        modelBuilder.Entity<Organization>().HasData(
            new Organization
            {
                Id = defaultOrgId,
                Name = "Default Organization",
                Slug = "default",
                MaxComputers = 10,
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true
            }
        );

        // Note: Admin user will be created programmatically on first run
    }
}