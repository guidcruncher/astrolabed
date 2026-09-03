// File: src/Astrolabed.Dns.Api/Controllers/DomainMatchEngineController.cs
using System.ComponentModel.DataAnnotations;
using System.Net.Mime;

using Astrolabed.Dns.Filtering;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Astrolabed.Dns.Api.Controllers;

/// <summary>
/// Options for configuring API behavior for domain match controls.
/// </summary>
public sealed class DomainMatchEngineControllerOptions
{
    /// <summary>
    /// Gets or sets the maximum allowed duration when disabling DNS filtering via the API.
    /// Default is set to 24 hours.
    /// </summary>
    public TimeSpan MaxDisableDuration { get; set; } = TimeSpan.FromHours(24);
}

/// <summary>
/// Represents a request body to disable domain filtering for a specific duration in seconds.
/// </summary>
public sealed class DisableBlockingRequest
{
    /// <summary>
    /// Gets or sets the duration in seconds for which domain blocking should be suspended.
    /// Must be greater than zero.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "DurationSeconds must be greater than 0.")]
    public int DurationSeconds { get; set; }
}

/// <summary>
/// Represents a response containing the status of DNS domain blocking.
/// </summary>
public sealed class BlockingStatusResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether domain blocking is currently active.
    /// </summary>
    public bool IsBlockingEnabled { get; set; }

    /// <summary>
    /// Gets or sets the timestamp until which domain blocking remains suspended, if set.
    /// </summary>
    public DateTimeOffset? DisableBlockingUntil { get; set; }
}

/// <summary>
/// Represents the result of matching a domain against configured allow/block lists.
/// </summary>
public sealed class DomainMatchResponse
{
    /// <summary>
    /// Gets or sets the domain that was queried.
    /// </summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether a matching rule was found.
    /// </summary>
    public bool IsMatched { get; set; }

    /// <summary>
    /// Gets or sets the rule details if a match occurred; otherwise <c>null</c>.
    /// </summary>
    public FilterRule? MatchedRule { get; set; }
}

/// <summary>
/// Controller providing API endpoints to manage DNS domain matching and temporary rule suspensions.
/// </summary>
[Authorize]
[ApiController]
[Route("api/dns/match-engine")]
[Produces(MediaTypeNames.Application.Json)]
public class DomainMatchEngineController : ControllerBase
{
    private readonly IDomainMatchEngine _matchEngine;
    private readonly ILogger<DomainMatchEngineController> _logger;
    private readonly DomainMatchEngineControllerOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainMatchEngineController"/> class.
    /// </summary>
    /// <param name="matchEngine">The domain match engine instance.</param>
    /// <param name="logger">The logger service.</param>
    /// <param name="options">Configuration options for the domain match controller.</param>
    public DomainMatchEngineController(
        IDomainMatchEngine matchEngine,
        ILogger<DomainMatchEngineController> logger,
        IOptions<DomainMatchEngineControllerOptions> options)
    {
        _matchEngine = matchEngine ?? throw new ArgumentNullException(nameof(matchEngine));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Retrieves the current status of DNS domain blocking operations.
    /// </summary>
    /// <returns>The status showing whether blocking is active and any expiration timestamp.</returns>
    /// <response code="200">Returns the current domain blocking status.</response>
    [HttpGet("status")]
    [ProducesResponseType(typeof(BlockingStatusResponse), StatusCodes.Status200OK)]
    public IActionResult GetStatus()
    {
        DateTimeOffset? disableUntil = _matchEngine.DisableBlockingUntil;
        bool isBlockingEnabled = !disableUntil.HasValue || DateTimeOffset.UtcNow >= disableUntil.Value;

        var response = new BlockingStatusResponse
        {
            IsBlockingEnabled = isBlockingEnabled,
            DisableBlockingUntil = disableUntil
        };

        return Ok(response);
    }

    /// <summary>
    /// Temporarily suspends DNS domain blocking for the specified duration.
    /// </summary>
    /// <param name="request">The request payload containing the duration in seconds.</param>
    /// <returns>The updated blocking status.</returns>
    /// <response code="200">Filtering suspended successfully.</response>
    /// <response code="400">Request validation failed or duration exceeds maximum threshold.</response>
    [HttpPost("disable")]
    [ProducesResponseType(typeof(BlockingStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult DisableBlocking([FromBody] DisableBlockingRequest request)
    {
        if (request.DurationSeconds <= 0)
        {
            return BadRequest("Duration must be greater than zero seconds.");
        }

        TimeSpan duration = TimeSpan.FromSeconds(request.DurationSeconds);

        if (duration > _options.MaxDisableDuration)
        {
            _logger.LogWarning(
                "Disable blocking request for {RequestedDuration} exceeded maximum allowed duration {MaxDuration}",
                duration,
                _options.MaxDisableDuration);

            return BadRequest($"Duration cannot exceed {_options.MaxDisableDuration.TotalHours} hours.");
        }

        _matchEngine.DisableBlocking(duration);

        _logger.LogInformation(
            "DNS domain blocking suspended for {Duration}. Resuming at {ResumeTime}",
            duration,
            _matchEngine.DisableBlockingUntil);

        var response = new BlockingStatusResponse
        {
            IsBlockingEnabled = false,
            DisableBlockingUntil = _matchEngine.DisableBlockingUntil
        };

        return Ok(response);
    }

    /// <summary>
    /// Resumes DNS domain blocking operations immediately.
    /// </summary>
    /// <returns>The updated blocking status showing blocking active.</returns>
    /// <response code="200">Filtering resumed successfully.</response>
    [HttpPost("resume")]
    [ProducesResponseType(typeof(BlockingStatusResponse), StatusCodes.Status200OK)]
    public IActionResult ResumeBlocking()
    {
        _matchEngine.ResumeBlocking();

        _logger.LogInformation("DNS domain blocking manually resumed");

        var response = new BlockingStatusResponse
        {
            IsBlockingEnabled = true,
            DisableBlockingUntil = null
        };

        return Ok(response);
    }

    /// <summary>
    /// Evaluates a domain against the active allow/block filter collections.
    /// </summary>
    /// <param name="domain">The fully qualified domain name to check.</param>
    /// <returns>The match evaluation result including any matching rule.</returns>
    /// <response code="200">Domain check executed successfully.</response>
    /// <response code="400">Domain parameter is missing or invalid.</response>
    [HttpGet("match")]
    [ProducesResponseType(typeof(DomainMatchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult MatchDomain([FromQuery][Required] string domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
        {
            return BadRequest("Domain parameter is required.");
        }

        bool isMatched = _matchEngine.TryMatch(domain, out FilterRule? matchedRule);

        _logger.LogDebug(
            "Evaluated domain match for {Domain}. Matched: {IsMatched}, ListId: {ListId}",
            domain,
            isMatched,
            matchedRule?.ListId);

        var response = new DomainMatchResponse
        {
            Domain = domain,
            IsMatched = isMatched,
            MatchedRule = matchedRule
        };

        return Ok(response);
    }
}
