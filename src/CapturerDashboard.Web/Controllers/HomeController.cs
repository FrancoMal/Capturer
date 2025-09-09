using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace CapturerDashboard.Web.Controllers;

[Route("")]
[Authorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Main dashboard page
    /// </summary>
    [HttpGet("")]
    [HttpGet("dashboard")]
    public IActionResult Index()
    {
        return Redirect("/index.html");
    }

    /// <summary>
    /// Computer detail page
    /// </summary>
    [HttpGet("computers/{id}")]
    public IActionResult ComputerDetail(Guid id)
    {
        ViewBag.ComputerId = id;
        return View("ComputerDetail");
    }

    /// <summary>
    /// Computer invitations management page
    /// </summary>
    [HttpGet("invitations")]
    public IActionResult Invitations()
    {
        return View();
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            version = "1.0.0"
        });
    }
}