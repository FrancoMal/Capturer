using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using CapturerDashboard.Core.Models;
using CapturerDashboard.Data.Context;

namespace CapturerDashboard.Web.Services;

public interface IComputerInvitationService
{
    Task<InvitationResult> CreateInvitationAsync(CreateInvitationRequest request, Guid createdByUserId);
    Task<InvitationProcessResult> ProcessInvitationAsync(string token, ProcessInvitationRequest request);
    Task<ValidateInvitationResult> ValidateInvitationAsync(string token);
    Task<List<ComputerInvitation>> GetActiveInvitationsAsync(Guid organizationId);
    Task<List<PendingComputerRegistration>> GetPendingApprovalsAsync(Guid organizationId);
    Task<ApprovalResult> ApproveRegistrationAsync(Guid registrationId, Guid approvedByUserId, string? notes = null);
    Task<ApprovalResult> DenyRegistrationAsync(Guid registrationId, Guid deniedByUserId, string notes);
}

public class ComputerInvitationService : IComputerInvitationService
{
    private readonly DashboardDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ComputerInvitationService> _logger;
    private readonly string _hmacSecretKey;

    public ComputerInvitationService(
        DashboardDbContext context,
        IConfiguration configuration,
        ILogger<ComputerInvitationService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _hmacSecretKey = _configuration["Security:HMACSecretKey"] ?? "default-secret-key-change-in-production";
    }

    public async Task<InvitationResult> CreateInvitationAsync(CreateInvitationRequest request, Guid createdByUserId)
    {
        try
        {
            // Generate secure token and signature
            var token = GenerateSecureToken(32);
            var signature = ComputeHMAC(token, _hmacSecretKey);

            var invitation = new ComputerInvitation
            {
                Token = token,
                Signature = signature,
                Description = request.Description ?? $"Invitation created at {DateTime.UtcNow:yyyy-MM-dd HH:mm}",
                OrganizationId = request.OrganizationId,
                CreatedByUserId = createdByUserId,
                ExpiresAt = DateTime.UtcNow.Add(request.ValidFor ?? TimeSpan.FromHours(24)),
                MaxUses = request.MaxUses ?? 1,
                RequireApproval = request.RequireApproval ?? true,
                AllowedNetworks = request.AllowedNetworks
            };

            _context.ComputerInvitations.Add(invitation);
            await _context.SaveChangesAsync();

            var baseUrl = GetDashboardBaseUrl();
            var invitationUrl = $"{baseUrl}/invite/{token}";

            _logger.LogInformation("Invitation created: {InvitationId} by user {UserId} for organization {OrgId}", 
                invitation.Id, createdByUserId, request.OrganizationId);

            return new InvitationResult
            {
                Success = true,
                InvitationId = invitation.Id,
                Token = token,
                InvitationUrl = invitationUrl,
                QRCodeData = GenerateQRCodeData(invitationUrl),
                ExpiresAt = invitation.ExpiresAt,
                MaxUses = invitation.MaxUses
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create invitation for organization {OrgId}", request.OrganizationId);
            return new InvitationResult
            {
                Success = false,
                ErrorMessage = "Failed to create invitation"
            };
        }
    }

    public async Task<ValidateInvitationResult> ValidateInvitationAsync(string token)
    {
        try
        {
            var invitation = await _context.ComputerInvitations
                .Include(i => i.Organization)
                .Include(i => i.CreatedByUser)
                .FirstOrDefaultAsync(i => i.Token == token);

            if (invitation == null)
            {
                return new ValidateInvitationResult { IsValid = false, ErrorMessage = "Invitation not found" };
            }

            // Verify signature
            var expectedSignature = ComputeHMAC(token, _hmacSecretKey);
            if (invitation.Signature != expectedSignature)
            {
                _logger.LogWarning("Invalid signature for invitation token: {Token}", token[..8] + "...");
                return new ValidateInvitationResult { IsValid = false, ErrorMessage = "Invalid invitation signature" };
            }

            // Check if expired
            if (invitation.IsExpired)
            {
                return new ValidateInvitationResult { IsValid = false, ErrorMessage = "Invitation has expired" };
            }

            // Check if max uses reached
            if (invitation.IsMaxUsesReached)
            {
                return new ValidateInvitationResult { IsValid = false, ErrorMessage = "Invitation has reached maximum uses" };
            }

            // Check if still active
            if (!invitation.IsActive)
            {
                return new ValidateInvitationResult { IsValid = false, ErrorMessage = "Invitation has been deactivated" };
            }

            return new ValidateInvitationResult
            {
                IsValid = true,
                Invitation = invitation,
                Organization = invitation.Organization,
                RequireApproval = invitation.RequireApproval
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating invitation token: {Token}", token[..8] + "...");
            return new ValidateInvitationResult { IsValid = false, ErrorMessage = "Validation error occurred" };
        }
    }

    public async Task<InvitationProcessResult> ProcessInvitationAsync(string token, ProcessInvitationRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Validate invitation
            var validationResult = await ValidateInvitationAsync(token);
            if (!validationResult.IsValid)
            {
                return new InvitationProcessResult
                {
                    Success = false,
                    ErrorMessage = validationResult.ErrorMessage
                };
            }

            var invitation = validationResult.Invitation!;

            // Generate computer fingerprint
            var fingerprint = ComputeComputerFingerprint(request.HardwareInfo);

            // Check for existing registration with same fingerprint
            var existingRegistration = await _context.PendingComputerRegistrations
                .FirstOrDefaultAsync(r => r.Fingerprint == fingerprint && r.Status == "Approved");

            if (existingRegistration != null)
            {
                var existingComputer = await _context.Computers
                    .FirstOrDefaultAsync(c => c.Id == existingRegistration.CreatedComputerId);

                if (existingComputer != null)
                {
                    return new InvitationProcessResult
                    {
                        Success = true,
                        IsExistingComputer = true,
                        ComputerId = existingComputer.Id,
                        ApiKey = existingComputer.ApiKey,
                        ComputerName = existingComputer.Name,
                        Status = "AlreadyRegistered"
                    };
                }
            }

            // Create pending registration
            var pendingRegistration = new PendingComputerRegistration
            {
                InvitationId = invitation.Id,
                ComputerName = request.ComputerName,
                ComputerId = request.ComputerId,
                Fingerprint = fingerprint,
                HardwareInfo = JsonSerializer.Serialize(request.HardwareInfo),
                RequestIP = request.RequestIP,
                Status = invitation.RequireApproval ? "PendingApproval" : "Approved"
            };

            _context.PendingComputerRegistrations.Add(pendingRegistration);

            // Update invitation usage
            invitation.UsedCount++;
            invitation.LastUsedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // If auto-approval, create computer immediately
            if (!invitation.RequireApproval)
            {
                var computer = await CreateComputerFromRegistrationAsync(pendingRegistration);
                await transaction.CommitAsync();

                return new InvitationProcessResult
                {
                    Success = true,
                    ComputerId = computer.Id,
                    ApiKey = computer.ApiKey,
                    ComputerName = computer.Name,
                    Status = "Approved",
                    PendingRegistrationId = pendingRegistration.Id
                };
            }

            await transaction.CommitAsync();

            _logger.LogInformation("Pending registration created: {RegistrationId} for invitation {InvitationId}", 
                pendingRegistration.Id, invitation.Id);

            return new InvitationProcessResult
            {
                Success = true,
                Status = "PendingApproval",
                PendingRegistrationId = pendingRegistration.Id,
                Message = "Registration submitted and pending approval"
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error processing invitation: {Token}", token[..8] + "...");
            
            return new InvitationProcessResult
            {
                Success = false,
                ErrorMessage = "Failed to process invitation"
            };
        }
    }

    public async Task<ApprovalResult> ApproveRegistrationAsync(Guid registrationId, Guid approvedByUserId, string? notes = null)
    {
        try
        {
            var registration = await _context.PendingComputerRegistrations
                .Include(r => r.Invitation)
                .ThenInclude(i => i.Organization)
                .FirstOrDefaultAsync(r => r.Id == registrationId && r.Status == "PendingApproval");

            if (registration == null)
            {
                return new ApprovalResult { Success = false, ErrorMessage = "Registration not found or not pending" };
            }

            // Check organization computer limit
            var computerCount = await _context.Computers
                .CountAsync(c => c.OrganizationId == registration.Invitation.OrganizationId && c.IsActive);

            if (computerCount >= registration.Invitation.Organization.MaxComputers)
            {
                return new ApprovalResult 
                { 
                    Success = false, 
                    ErrorMessage = $"Organization has reached maximum computers limit ({registration.Invitation.Organization.MaxComputers})" 
                };
            }

            // Create computer
            var computer = await CreateComputerFromRegistrationAsync(registration);

            // Update registration
            registration.Status = "Approved";
            registration.ProcessedAt = DateTime.UtcNow;
            registration.ProcessedByUserId = approvedByUserId;
            registration.CreatedComputerId = computer.Id;
            registration.ApprovalNotes = notes;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Computer registration approved: {RegistrationId} → Computer: {ComputerId}", 
                registrationId, computer.Id);

            return new ApprovalResult
            {
                Success = true,
                ComputerId = computer.Id,
                ApiKey = computer.ApiKey,
                ComputerName = computer.Name
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to approve registration: {RegistrationId}", registrationId);
            return new ApprovalResult { Success = false, ErrorMessage = "Approval failed" };
        }
    }

    public async Task<ApprovalResult> DenyRegistrationAsync(Guid registrationId, Guid deniedByUserId, string notes)
    {
        try
        {
            var registration = await _context.PendingComputerRegistrations
                .FirstOrDefaultAsync(r => r.Id == registrationId && r.Status == "PendingApproval");

            if (registration == null)
            {
                return new ApprovalResult { Success = false, ErrorMessage = "Registration not found or not pending" };
            }

            registration.Status = "Denied";
            registration.ProcessedAt = DateTime.UtcNow;
            registration.ProcessedByUserId = deniedByUserId;
            registration.ApprovalNotes = notes;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Computer registration denied: {RegistrationId} by user {UserId}", 
                registrationId, deniedByUserId);

            return new ApprovalResult { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deny registration: {RegistrationId}", registrationId);
            return new ApprovalResult { Success = false, ErrorMessage = "Denial failed" };
        }
    }

    public async Task<List<ComputerInvitation>> GetActiveInvitationsAsync(Guid organizationId)
    {
        var currentTime = DateTime.UtcNow;
        return await _context.ComputerInvitations
            .Include(i => i.CreatedByUser)
            .Where(i => i.OrganizationId == organizationId && 
                       i.IsActive && 
                       i.ExpiresAt > currentTime)
            .OrderByDescending(i => i.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<PendingComputerRegistration>> GetPendingApprovalsAsync(Guid organizationId)
    {
        return await _context.PendingComputerRegistrations
            .Include(r => r.Invitation)
            .Where(r => r.Invitation.OrganizationId == organizationId && r.Status == "PendingApproval")
            .OrderBy(r => r.RequestedAt)
            .ToListAsync();
    }

    private async Task<Computer> CreateComputerFromRegistrationAsync(PendingComputerRegistration registration)
    {
        var apiKey = GenerateSecureApiKey(registration.Invitation.OrganizationId);

        var computer = new Computer
        {
            ComputerId = registration.ComputerId ?? registration.Fingerprint[..20], // Fallback to truncated fingerprint
            Name = registration.ComputerName,
            ApiKey = apiKey,
            OrganizationId = registration.Invitation.OrganizationId,
            HardwareInfo = registration.HardwareInfo,
            Status = "Online",
            LastSeenAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.Computers.Add(computer);
        await _context.SaveChangesAsync();

        return computer;
    }

    private string GenerateSecureToken(int length)
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[length];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    private string GenerateSecureApiKey(Guid organizationId)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var random = GenerateSecureToken(16);
        var payload = $"{organizationId}:{timestamp}:{random}";
        var signature = ComputeHMAC(payload, _hmacSecretKey)[..16]; // Truncate signature
        
        return $"cap_{signature}_{random}";
    }

    private string ComputeHMAC(string data, string key)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hash)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    private string ComputeComputerFingerprint(Dictionary<string, object> hardwareInfo)
    {
        // Create a stable fingerprint from hardware info
        var serializedHw = JsonSerializer.Serialize(hardwareInfo, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(serializedHw));
        return Convert.ToBase64String(hash)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    private string GetDashboardBaseUrl()
    {
        // In development, use localhost. In production, get from config
        var baseUrl = _configuration["Dashboard:BaseUrl"];
        if (string.IsNullOrEmpty(baseUrl))
        {
            // Fallback for development
            baseUrl = "http://localhost:5229";
        }
        return baseUrl;
    }

    private string GenerateQRCodeData(string url)
    {
        // For now, return the URL. Later we can implement actual QR code generation
        return url;
    }
}

// DTOs for service operations
public class CreateInvitationRequest
{
    public Guid OrganizationId { get; set; }
    public string? Description { get; set; }
    public TimeSpan? ValidFor { get; set; } = TimeSpan.FromHours(24);
    public int? MaxUses { get; set; } = 1;
    public bool? RequireApproval { get; set; } = true;
    public string? AllowedNetworks { get; set; }
}

public class ProcessInvitationRequest
{
    public string ComputerName { get; set; } = string.Empty;
    public string? ComputerId { get; set; }
    public Dictionary<string, object> HardwareInfo { get; set; } = new();
    public string? RequestIP { get; set; }
}

public class InvitationResult
{
    public bool Success { get; set; }
    public Guid? InvitationId { get; set; }
    public string? Token { get; set; }
    public string? InvitationUrl { get; set; }
    public string? QRCodeData { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int? MaxUses { get; set; }
    public string? ErrorMessage { get; set; }
}

public class ValidateInvitationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public ComputerInvitation? Invitation { get; set; }
    public Organization? Organization { get; set; }
    public bool RequireApproval { get; set; }
}

public class InvitationProcessResult
{
    public bool Success { get; set; }
    public string Status { get; set; } = string.Empty; // "Approved", "PendingApproval", "AlreadyRegistered"
    public bool IsExistingComputer { get; set; }
    public Guid? ComputerId { get; set; }
    public Guid? PendingRegistrationId { get; set; }
    public string? ApiKey { get; set; }
    public string? ComputerName { get; set; }
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }
}

public class ApprovalResult
{
    public bool Success { get; set; }
    public Guid? ComputerId { get; set; }
    public string? ApiKey { get; set; }
    public string? ComputerName { get; set; }
    public string? ErrorMessage { get; set; }
}