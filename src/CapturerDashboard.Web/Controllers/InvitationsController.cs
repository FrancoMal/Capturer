using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using CapturerDashboard.Web.Services;
using CapturerDashboard.Core.Models;

namespace CapturerDashboard.Web.Controllers;

[Authorize]
[Route("api/invitations")]
public class InvitationsController : ControllerBase
{
    private readonly IComputerInvitationService _invitationService;
    private readonly ILogger<InvitationsController> _logger;

    public InvitationsController(
        IComputerInvitationService invitationService,
        ILogger<InvitationsController> logger)
    {
        _invitationService = invitationService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new computer invitation
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateInvitation([FromBody] CreateInvitationApiRequest request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var organizationId = GetCurrentUserOrganizationId();

            var serviceRequest = new CreateInvitationRequest
            {
                OrganizationId = organizationId,
                Description = request.Description,
                ValidFor = request.ValidForHours.HasValue ? TimeSpan.FromHours(request.ValidForHours.Value) : TimeSpan.FromHours(24),
                MaxUses = request.MaxUses ?? 1,
                RequireApproval = request.RequireApproval ?? true,
                AllowedNetworks = request.AllowedNetworks
            };

            var result = await _invitationService.CreateInvitationAsync(serviceRequest, currentUserId);

            if (!result.Success)
            {
                return BadRequest(new { error = result.ErrorMessage });
            }

            return CreatedAtAction(nameof(GetInvitation), new { id = result.InvitationId }, new
            {
                id = result.InvitationId,
                token = result.Token,
                invitationUrl = result.InvitationUrl,
                qrCodeData = result.QRCodeData,
                expiresAt = result.ExpiresAt,
                maxUses = result.MaxUses
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create invitation");
            return StatusCode(500, new { error = "Failed to create invitation" });
        }
    }

    /// <summary>
    /// Gets invitation details
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetInvitation(Guid id)
    {
        try
        {
            var organizationId = GetCurrentUserOrganizationId();
            var invitations = await _invitationService.GetActiveInvitationsAsync(organizationId);
            var invitation = invitations.FirstOrDefault(i => i.Id == id);

            if (invitation == null)
            {
                return NotFound();
            }

            return Ok(new
            {
                invitation.Id,
                invitation.Token,
                invitation.Description,
                invitation.CreatedAt,
                invitation.ExpiresAt,
                invitation.MaxUses,
                invitation.UsedCount,
                invitation.RequireApproval,
                invitation.IsUsable,
                invitation.TimeUntilExpiry,
                createdBy = new { invitation.CreatedByUser.FirstName, invitation.CreatedByUser.LastName },
                invitationUrl = $"{GetBaseUrl()}/invite/{invitation.Token}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get invitation: {InvitationId}", id);
            return StatusCode(500, new { error = "Failed to retrieve invitation" });
        }
    }

    /// <summary>
    /// Gets active invitations for current user's organization
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetActiveInvitations()
    {
        try
        {
            var organizationId = GetCurrentUserOrganizationId();
            var invitations = await _invitationService.GetActiveInvitationsAsync(organizationId);

            var result = invitations.Select(i => new
            {
                i.Id,
                i.Description,
                i.CreatedAt,
                i.ExpiresAt,
                i.MaxUses,
                i.UsedCount,
                i.RequireApproval,
                i.IsUsable,
                timeUntilExpiry = i.TimeUntilExpiry.TotalHours,
                createdBy = $"{i.CreatedByUser.FirstName} {i.CreatedByUser.LastName}",
                invitationUrl = $"{GetBaseUrl()}/invite/{i.Token}"
            }).ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active invitations");
            return StatusCode(500, new { error = "Failed to retrieve invitations" });
        }
    }

    /// <summary>
    /// Gets pending computer registrations awaiting approval
    /// </summary>
    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingApprovals()
    {
        try
        {
            var organizationId = GetCurrentUserOrganizationId();
            var pending = await _invitationService.GetPendingApprovalsAsync(organizationId);

            var result = pending.Select(p => new
            {
                p.Id,
                p.ComputerName,
                p.ComputerId,
                p.RequestedAt,
                p.RequestIP,
                p.StatusDisplay,
                p.HardwareInfoData,
                invitation = new { p.Invitation.Description, p.Invitation.CreatedAt }
            }).ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get pending approvals");
            return StatusCode(500, new { error = "Failed to retrieve pending approvals" });
        }
    }

    /// <summary>
    /// Approves a pending computer registration
    /// </summary>
    [HttpPost("pending/{id}/approve")]
    public async Task<IActionResult> ApproveRegistration(Guid id, [FromBody] ApprovalRequest request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var result = await _invitationService.ApproveRegistrationAsync(id, currentUserId, request.Notes);

            if (!result.Success)
            {
                return BadRequest(new { error = result.ErrorMessage });
            }

            return Ok(new
            {
                computerId = result.ComputerId,
                apiKey = result.ApiKey,
                computerName = result.ComputerName,
                message = "Computer registration approved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to approve registration: {RegistrationId}", id);
            return StatusCode(500, new { error = "Approval failed" });
        }
    }

    /// <summary>
    /// Denies a pending computer registration
    /// </summary>
    [HttpPost("pending/{id}/deny")]
    public async Task<IActionResult> DenyRegistration(Guid id, [FromBody] DenialRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Reason))
            {
                return BadRequest(new { error = "Denial reason is required" });
            }

            var currentUserId = GetCurrentUserId();
            var result = await _invitationService.DenyRegistrationAsync(id, currentUserId, request.Reason);

            if (!result.Success)
            {
                return BadRequest(new { error = result.ErrorMessage });
            }

            return Ok(new { message = "Computer registration denied successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deny registration: {RegistrationId}", id);
            return StatusCode(500, new { error = "Denial failed" });
        }
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        throw new InvalidOperationException("User ID not found in claims");
    }

    private Guid GetCurrentUserOrganizationId()
    {
        var orgIdClaim = User.FindFirst("OrganizationId")?.Value;
        if (Guid.TryParse(orgIdClaim, out var orgId))
        {
            return orgId;
        }
        throw new InvalidOperationException("Organization ID not found in claims");
    }

    private string GetBaseUrl()
    {
        return $"{Request.Scheme}://{Request.Host}";
    }
}

// Additional controller for handling invitation processing (no auth required)
[Route("invite")]
public class InvitationProcessController : ControllerBase
{
    private readonly IComputerInvitationService _invitationService;
    private readonly ILogger<InvitationProcessController> _logger;

    public InvitationProcessController(
        IComputerInvitationService invitationService,
        ILogger<InvitationProcessController> logger)
    {
        _invitationService = invitationService;
        _logger = logger;
    }

    /// <summary>
    /// Validates an invitation token (called by Capturer app)
    /// </summary>
    [HttpGet("{token}")]
    public async Task<IActionResult> ValidateInvitation(string token)
    {
        try
        {
            var result = await _invitationService.ValidateInvitationAsync(token);

            if (!result.IsValid)
            {
                return BadRequest(new { error = result.ErrorMessage });
            }

            return Ok(new
            {
                valid = true,
                organizationName = result.Organization?.Name,
                organizationSlug = result.Organization?.Slug,
                requireApproval = result.RequireApproval,
                message = "Invitation is valid and ready to process"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate invitation token: {Token}", token[..8] + "...");
            return StatusCode(500, new { error = "Validation failed" });
        }
    }

    /// <summary>
    /// Processes computer registration with invitation token (called by Capturer app)
    /// </summary>
    [HttpPost("{token}/register")]
    public async Task<IActionResult> ProcessInvitation(string token, [FromBody] ProcessInvitationApiRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.ComputerName))
            {
                return BadRequest(new { error = "Computer name is required" });
            }

            var serviceRequest = new ProcessInvitationRequest
            {
                ComputerName = request.ComputerName,
                ComputerId = request.ComputerId,
                HardwareInfo = request.HardwareInfo ?? new Dictionary<string, object>(),
                RequestIP = HttpContext.Connection.RemoteIpAddress?.ToString()
            };

            var result = await _invitationService.ProcessInvitationAsync(token, serviceRequest);

            if (!result.Success)
            {
                return BadRequest(new { error = result.ErrorMessage });
            }

            var response = new
            {
                success = true,
                status = result.Status,
                computerId = result.ComputerId,
                apiKey = result.ApiKey,
                computerName = result.ComputerName,
                message = result.Message ?? "Registration processed successfully",
                isExistingComputer = result.IsExistingComputer,
                dashboardUrl = $"{Request.Scheme}://{Request.Host}",
                syncSettings = new
                {
                    syncEnabled = true,
                    syncIntervalSeconds = 300,
                    realtimeUpdates = true
                }
            };

            return result.Status == "Approved" ? 
                CreatedAtAction(nameof(GetRegistrationStatus), new { id = result.ComputerId }, response) :
                Accepted(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process invitation: {Token}", token[..8] + "...");
            return StatusCode(500, new { error = "Registration processing failed" });
        }
    }

    /// <summary>
    /// Gets registration status (for polling while waiting approval)
    /// </summary>
    [HttpGet("status/{id}")]
    public IActionResult GetRegistrationStatus(Guid id)
    {
        // This would check if the computer has been approved
        // For now, return a simple status
        return Ok(new { status = "pending", message = "Registration is being processed" });
    }
}

// Request DTOs
public class CreateInvitationApiRequest
{
    public string? Description { get; set; }
    public int? ValidForHours { get; set; } = 24;
    public int? MaxUses { get; set; } = 1;
    public bool? RequireApproval { get; set; } = true;
    public string? AllowedNetworks { get; set; }
}

public class ProcessInvitationApiRequest
{
    public string ComputerName { get; set; } = string.Empty;
    public string? ComputerId { get; set; }
    public Dictionary<string, object>? HardwareInfo { get; set; }
}

public class ApprovalRequest
{
    public string? Notes { get; set; }
}

public class DenialRequest
{
    public string Reason { get; set; } = string.Empty;
}