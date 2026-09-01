// File: src/Astrolabed.Api/Controllers/DnsAnalyticsController.cs
using System.ComponentModel.DataAnnotations;

using Astrolabed.Data.Repositories;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Astrolabed.Api.Controllers;

/// <summary>
/// Provides HTTP API endpoints for retrieving DNS ad-blocking analytics, network metrics, and query anomaly detections.
/// </summary>
/// <param name="analyticsRepository">The repository handling DNS analytics data access queries.</param>
/// <param name="logger">The structured logger instance for controller diagnostics.</param>
[ApiController]
[Route("api/v1/dns/analytics")]
[Produces("application/json")]
public sealed partial class DnsAnalyticsController(
    IDnsAnalyticsRepository analyticsRepository,
    ILogger<DnsAnalyticsController> logger) : ControllerBase
{
    private readonly IDnsAnalyticsRepository _analyticsRepository = analyticsRepository ?? throw new ArgumentNullException(nameof(analyticsRepository));
    private readonly ILogger<DnsAnalyticsController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Retrieves the network-wide ad-blocking rate percentage calculated from queries recorded on or after a specified UTC timestamp.
    /// </summary>
    /// <param name="startTimeUtc">The starting Unix epoch timestamp in milliseconds.</param>
    /// <param name="cancellationToken">Cancellation token for observing request aborts.</param>
    /// <returns>The calculated block rate percentage between 0.00 and 100.00.</returns>
    /// <response code="200">Returns the calculated block rate percentage.</response>
    /// <response code="400">If the provided start time is invalid or negative.</response>
    [HttpGet("block-rate")]
    [ProducesResponseType(typeof(BlockRateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BlockRateResponse>> GetBlockRate(
        [FromQuery, Range(0, long.MaxValue, ErrorMessage = "Start time must be a non-negative Unix epoch timestamp in milliseconds.")] long startTimeUtc,
        CancellationToken cancellationToken)
    {
        LogFetchingBlockRate(_logger, startTimeUtc);

        double rate = await _analyticsRepository.GetBlockRateAsync(startTimeUtc, cancellationToken).ConfigureAwait(false);

        return Ok(new BlockRateResponse(rate, startTimeUtc));
    }

    /// <summary>
    /// Identifies client endpoints exhibiting excessive blocked query volumes within a given timeframe, signaling potential retry storms.
    /// </summary>
    /// <param name="startTimeUtc">The starting Unix epoch timestamp in milliseconds.</param>
    /// <param name="limit">The minimum query count threshold required to classify a sequence as a retry storm. Defaults to 50.</param>
    /// <param name="cancellationToken">Cancellation token for observing request aborts.</param>
    /// <returns>A list of detected retry storm occurrences ordered by query count descending.</returns>
    /// <response code="200">Returns the collection of detected client retry storm results.</response>
    /// <response code="400">If query parameters fail validation limits.</response>
    [HttpGet("retry-storms")]
    [ProducesResponseType(typeof(IEnumerable<RetryStormResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<RetryStormResult>>> GetRetryStorms(
        [FromQuery, Range(0, long.MaxValue, ErrorMessage = "Start time must be a non-negative Unix epoch timestamp in milliseconds.")] long startTimeUtc,
        [FromQuery, Range(1, 10000, ErrorMessage = "Threshold limit must be between 1 and 10,000.")] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        LogFetchingRetryStorms(_logger, startTimeUtc, limit);

        IEnumerable<RetryStormResult> results = await _analyticsRepository
            .GetRetryStormsAsync(startTimeUtc, limit, cancellationToken)
            .ConfigureAwait(false);

        return Ok(results);
    }

    /// <summary>
    /// Detects potential CNAME cloaking patterns where unblocked domain requests resolve to known third-party tracking target aliases.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for observing request aborts.</param>
    /// <returns>A list of detected CNAME cloaking matches.</returns>
    /// <response code="200">Returns the collection of detected CNAME cloaking matches.</response>
    [HttpGet("cname-cloaking")]
    [ProducesResponseType(typeof(IEnumerable<CnameCloakingResult>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CnameCloakingResult>>> GetCnameCloakingMatches(CancellationToken cancellationToken)
    {
        LogFetchingCnameCloaking(_logger);

        IEnumerable<CnameCloakingResult> results = await _analyticsRepository
            .GetCnameCloakingMatchesAsync(cancellationToken)
            .ConfigureAwait(false);

        return Ok(results);
    }

    [LoggerMessage(
        EventId = 301,
        Level = LogLevel.Information,
        Message = "Calculating DNS block rate for requests starting from UTC epoch {StartTimeUtc}ms.")]
    private static partial void LogFetchingBlockRate(ILogger logger, long startTimeUtc);

    [LoggerMessage(
        EventId = 302,
        Level = LogLevel.Information,
        Message = "Querying DNS retry storms starting from UTC epoch {StartTimeUtc}ms with threshold limit {Limit}.")]
    private static partial void LogFetchingRetryStorms(ILogger logger, long startTimeUtc, int limit);

    [LoggerMessage(
        EventId = 303,
        Level = LogLevel.Information,
        Message = "Querying CNAME cloaking analytics matches.")]
    private static partial void LogFetchingCnameCloaking(ILogger logger);
}

/// <summary>
/// Represents the API response wrapper for network block rate calculations.
/// </summary>
/// <param name="BlockRatePercentage">The calculated block rate percentage between 0.00 and 100.00.</param>
/// <param name="StartTimeUtc">The starting Unix epoch timestamp in milliseconds used for the query.</param>
public sealed record BlockRateResponse(double BlockRatePercentage, long StartTimeUtc);
