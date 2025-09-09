using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CapturerDashboard.Data.Context;
using CapturerDashboard.Core.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CapturerDashboard.Web.Controllers;

[ApiController]
[Route("api/computers")]
public class ComputersApiController : ControllerBase
{
    private readonly DashboardDbContext _context;
    private readonly ILogger<ComputersApiController> _logger;

    public ComputersApiController(DashboardDbContext context, ILogger<ComputersApiController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Registers a new computer with the dashboard
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> RegisterComputer([FromBody] RegisterComputerRequest request)
    {
        try
        {
            // Validate request
            if (string.IsNullOrEmpty(request.ComputerId) || 
                string.IsNullOrEmpty(request.ComputerName) ||
                string.IsNullOrEmpty(request.OrganizationCode))
            {
                return BadRequest(new { error = "ComputerId, ComputerName, and OrganizationCode are required" });
            }

            // Find or create organization
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Slug == request.OrganizationCode);

            if (organization == null)
            {
                // Create new organization if it doesn't exist
                organization = new Organization
                {
                    Name = request.OrganizationCode,
                    Slug = request.OrganizationCode,
                    MaxComputers = 50 // Default limit for new organizations
                };
                _context.Organizations.Add(organization);
                await _context.SaveChangesAsync();
            }

            // Check if computer already exists
            var existingComputer = await _context.Computers
                .FirstOrDefaultAsync(c => c.ComputerId == request.ComputerId);

            if (existingComputer != null)
            {
                if (existingComputer.OrganizationId != organization.Id)
                {
                    return BadRequest(new { error = "Computer is already registered with different organization" });
                }

                // Return existing computer's API key
                return Ok(new
                {
                    apiKey = existingComputer.ApiKey,
                    dashboardUrl = GetDashboardUrl(),
                    syncInterval = 300,
                    computerId = existingComputer.Id,
                    message = "Computer already registered"
                });
            }

            // Check organization computer limit
            var computerCount = await _context.Computers
                .CountAsync(c => c.OrganizationId == organization.Id && c.IsActive);

            if (computerCount >= organization.MaxComputers)
            {
                return BadRequest(new { 
                    error = "Organization has reached maximum number of computers",
                    maxComputers = organization.MaxComputers
                });
            }

            // Generate API key
            var apiKey = GenerateApiKey();

            // Create new computer
            var computer = new Computer
            {
                ComputerId = request.ComputerId,
                Name = request.ComputerName,
                ApiKey = apiKey,
                OrganizationId = organization.Id,
                HardwareInfo = JsonSerializer.Serialize(request.HardwareInfo ?? new Dictionary<string, object>()),
                Status = "Online",
                LastSeenAt = DateTime.UtcNow
            };

            _context.Computers.Add(computer);
            await _context.SaveChangesAsync();

            _logger.LogInformation("New computer registered: {ComputerName} ({ComputerId}) for organization {OrgName}", 
                request.ComputerName, request.ComputerId, organization.Name);

            return CreatedAtAction(
                nameof(GetComputer),
                new { id = computer.Id },
                new
                {
                    apiKey = computer.ApiKey,
                    dashboardUrl = GetDashboardUrl(),
                    syncInterval = 300,
                    computerId = computer.Id,
                    message = "Computer registered successfully"
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering computer");
            return StatusCode(500, new { error = "Registration failed" });
        }
    }

    /// <summary>
    /// Gets computer configuration
    /// </summary>
    [HttpGet("{id}/config")]
    public async Task<IActionResult> GetComputerConfig(Guid id)
    {
        var apiKey = Request.Headers["X-API-Key"].FirstOrDefault();
        if (string.IsNullOrEmpty(apiKey))
        {
            return Unauthorized(new { error = "API Key is required" });
        }

        var computer = await _context.Computers
            .Include(c => c.Organization)
            .FirstOrDefaultAsync(c => c.Id == id && c.ApiKey == apiKey);

        if (computer == null)
        {
            return NotFound();
        }

        return Ok(new
        {
            captureInterval = 1800, // 30 minutes
            quadrants = new[]
            {
                new { name = "Trabajo", region = new { x = 0, y = 0, width = 50, height = 50 } },
                new { name = "Dashboard", region = new { x = 50, y = 0, width = 50, height = 50 } },
                new { name = "Email", region = new { x = 0, y = 50, width = 50, height = 50 } },
                new { name = "Otros", region = new { x = 50, y = 50, width = 50, height = 50 } }
            },
            emailSettings = new
            {
                enabled = false,
                recipients = new string[] { },
                schedule = "daily"
            },
            features = new
            {
                activityDashboard = true,
                autoReports = true,
                realTimeSync = true
            }
        });
    }

    /// <summary>
    /// Gets computer details
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetComputer(Guid id)
    {
        var computer = await _context.Computers
            .Include(c => c.Organization)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (computer == null)
        {
            return NotFound();
        }

        return Ok(new
        {
            computer.Id,
            computer.ComputerId,
            computer.Name,
            computer.Status,
            computer.LastSeenAt,
            computer.IsOnline,
            Organization = new { computer.Organization.Name, computer.Organization.Slug },
            computer.CreatedAt
        });
    }

    /// <summary>
    /// Gets all computers for an organization
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetComputers([FromQuery] string? organizationId = null)
    {
        var query = _context.Computers.Include(c => c.Organization).AsQueryable();

        if (!string.IsNullOrEmpty(organizationId))
        {
            if (Guid.TryParse(organizationId, out var orgId))
            {
                query = query.Where(c => c.OrganizationId == orgId);
            }
        }

        var computers = await query
            .OrderBy(c => c.Name)
            .Select(c => new
            {
                c.Id,
                c.ComputerId,
                c.Name,
                c.Status,
                c.LastSeenAt,
                c.IsOnline,
                Organization = new { c.Organization.Name, c.Organization.Slug },
                c.CreatedAt
            })
            .ToListAsync();

        return Ok(computers);
    }

    private string GenerateApiKey()
    {
        // Generate a secure API key with prefix
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        var key = Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
        
        return $"cap_{key}";
    }

    private string GetDashboardUrl()
    {
        var request = HttpContext.Request;
        return $"{request.Scheme}://{request.Host}";
    }
}

public class RegisterComputerRequest
{
    public string ComputerId { get; set; } = string.Empty;
    public string ComputerName { get; set; } = string.Empty;
    public string OrganizationCode { get; set; } = string.Empty;
    public Dictionary<string, object>? HardwareInfo { get; set; }
}