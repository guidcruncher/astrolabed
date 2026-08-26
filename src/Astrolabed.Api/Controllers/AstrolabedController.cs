namespace Astrolabed.Api.Controllers;

using Astrolabed.Api.Services;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

/// <summary>
/// Strongly typed response model representing the Astrolabed module operational status.
/// </summary>
/// <param name="Status">The current operational health status description.</param>
/// <param name="Timestamp">The UTC timestamp when the status was retrieved.</param>
public sealed record AstrolabedStatusResponse(string Status, DateTimeOffset Timestamp);

/// <summary>
/// Handles HTTP requests for Astrolabed API operations and module diagnostics.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class AstrolabedController : ControllerBase
{
    private readonly IAstrolabedService _astrolabedService;
    private readonly ILogger<AstrolabedController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AstrolabedController"/> class.
    /// </summary>
    /// <param name="astrolabedService">The core Astrolabed domain service instance.</param>
    /// <param name="logger">The controller logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when required dependencies are null.</exception>
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
    /// Retrieves current health and operational status for the Astrolabed module.
    /// </summary>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/astrolabed/status
    ///
    /// Returns real-time health indicator and UTC timestamp for telemetry monitoring.
    /// </remarks>
    /// <returns>A strongly typed <see cref="AstrolabedStatusResponse"/> containing status details.</returns>
    /// <response code="200">Successfully retrieved the operational status of the Astrolabed module.</response>
    /// <response code="500">An unexpected internal error occurred while fetching system status.</response>
    [HttpGet("status")]
    [ProducesResponseType(typeof(AstrolabedStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public ActionResult<AstrolabedStatusResponse> GetStatus()
    {
        _logger.LogInformation("Processing GET request for Astrolabed status.");

        string status = _astrolabedService.GetSystemStatus();
        AstrolabedStatusResponse response = new(status, DateTimeOffset.UtcNow);

        return Ok(response);
    }
}
