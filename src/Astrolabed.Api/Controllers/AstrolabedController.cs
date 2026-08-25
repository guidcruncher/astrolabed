using Astrolabed.Api.Services;

using Microsoft.AspNetCore.Mvc;

namespace Astrolabed.Api.Controllers;

/// <summary>
/// Handles HTTP requests for Astrolabed API endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class AstrolabedController : ControllerBase
{
    private readonly IAstrolabedService _astrolabedService;
    private readonly ILogger<AstrolabedController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AstrolabedController"/> class.
    /// </summary>
    /// <param name="astrolabedService">The core Astrolabed domain service.</param>
    /// <param name="logger">The controller logger instance.</param>
    public AstrolabedController(
        IAstrolabedService astrolabedService,
        ILogger<AstrolabedController> logger)
    {
        ArgumentNullException.ThrowIfNull(astrolabedService);
        ArgumentNullException.ThrowIfNull(logger);

        _astrolabedService = astrolabedService;
        _logger = logger;
    }

    /// <summary>
    /// Gets current health and operational status for the Astrolabed module.
    /// </summary>
    /// <returns>An <see cref="IActionResult"/> containing the status description.</returns>
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        _logger.LogInformation("Processing GET request for Astrolabed status.");
        var status = _astrolabedService.GetSystemStatus();
        return Ok(new { Status = status, Timestamp = DateTime.UtcNow });
    }
}
