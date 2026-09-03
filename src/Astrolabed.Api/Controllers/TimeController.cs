namespace Astrolabed.Api.Controllers;

using System;
using System.Threading;
using System.Threading.Tasks;

using Astrolabed.Ntp.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

/// <summary>
/// Provides API endpoints for retrieving synchronized network time information.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class TimeController : ControllerBase
{
    private readonly ITimeResolver _resolver;
    private readonly ILogger<TimeController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TimeController"/> class.
    /// </summary>
    /// <param name="resolver">The service responsible for resolving current time information.</param>
    /// <param name="logger">The logger instance for service diagnostics and telemetry.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="resolver"/> or <paramref name="logger"/> is <c>null</c>.</exception>
    public TimeController(
        ITimeResolver resolver,
        ILogger<TimeController> logger)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        ArgumentNullException.ThrowIfNull(logger);
        _resolver = resolver;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves the current synchronized network time offset.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for operation cancellation requests.</param>
    /// <returns>An <see cref="ActionResult{T}"/> containing the resolved <see cref="DateTimeOffset"/>.</returns>
    /// <response code="200">Successfully retrieved current time.</response>
    /// <response code="400">The request was invalid or malformed.</response>
    /// <response code="500">An internal error occurred while attempting to resolve current time.</response>
    [HttpGet]
    [EndpointSummary("Get Current Time")]
    [EndpointDescription("Resolves and returns the current synchronized date and time offset from the configured time provider.")]
    [ProducesResponseType(typeof(DateTimeOffset), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DateTimeOffset>> GetTime(
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset value = await _resolver.GetCurrentTimeAsync(cancellationToken);
        return Ok(value);
    }
}
