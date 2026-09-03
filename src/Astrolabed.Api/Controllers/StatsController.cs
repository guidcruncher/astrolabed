namespace Astrolabed.Api.Controllers;

using System.Net.Mime;

using Astrolabed.Data.Models;
using Astrolabed.Data.Repositories;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

/// <summary>
/// Provides API endpoints for querying system statistics and analytical metrics.
/// </summary>
/// <param name="statsRepository">The repository instance for retrieving statistical data.</param>
/// <param name="logger">Structured logger instance for diagnostic output.</param>
[Authorize]
[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public sealed partial class StatsController(
    IStatsRepository statsRepository,
    ILogger<StatsController> logger) : ControllerBase
{
    private readonly IStatsRepository _statsRepository = statsRepository ?? throw new ArgumentNullException(nameof(statsRepository));
    private readonly ILogger<StatsController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Retrieves DNS response event counts aggregated into 24 hourly records (0–23 UTC).
    /// </summary>
    /// <remarks>
    /// Returns a list of 24 items representing each hour of the day (0–23 UTC). Each item contains the 
    /// count of blocked and allowed events for that hour. Any hours missing database records 
    /// are explicitly populated with zero counts.
    /// </remarks>
    /// <param name="startEpoch">Start Epoch to filter by.</param>
    /// <param name="endEpoch">End Epoch to filter by.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>A collection of 24 hourly DNS summary records.</returns>
    /// <response code="200">Hourly DNS response event summaries retrieved successfully.</response>
    /// <response code="500">An unhandled server error occurred while retrieving event statistics.</response>
    [HttpGet("dns/hourly")]
    [EndpointSummary("Get Hourly DNS Response Metrics")]
    [EndpointDescription("Fetches DNS response event totals grouped across 24 UTC hours into single hourly records containing blocked and allowed counts.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<DnsHourlyEventSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyCollection<DnsHourlyEventSummary>>> GetHourlyDnsEventSummariesAsync(
        [FromQuery] long startEpoch = 0,
        [FromQuery] long endEpoch = 0,
        CancellationToken cancellationToken = default)
    {
        LogExecutingGetHourlyDnsEventSummaries(_logger);
        long endEpochValue = (endEpoch == 0 ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() : endEpoch);

        IReadOnlyCollection<DnsHourlyEventSummary> summaries =
            await _statsRepository.GetHourlyEventSummariesAsync(startEpoch, endEpochValue, cancellationToken);

        LogFetchedHourlyDnsEventSummaries(_logger, summaries.Count);

        return Ok(summaries);
    }

    /// <summary>
    /// Retrieves a breakdown of DNS queries aggregated by their question type.
    /// </summary>
    /// <remarks>
    /// Returns a collection of records indicating total query volume categorized by individual DNS record types (for example: A, AAAA, MX, CNAME).
    /// </remarks> 
    /// <param name="startEpoch">Start Epoch to filter by.</param>
    /// <param name="endEpoch">End Epoch to filter by.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>A collection of DNS question type summary records.</returns>
    /// <response code="200">DNS question type summaries retrieved successfully.</response>
    /// <response code="500">An unhandled server error occurred while retrieving question type statistics.</response>
    [HttpGet("dns/question-types")]
    [EndpointSummary("Get DNS Question Type Metrics")]
    [EndpointDescription("Fetches aggregate DNS query counts grouped by their question type.")]
    [ProducesResponseType(typeof(IEnumerable<DnsQuestionTypeSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<DnsQuestionTypeSummary>>> GetQuestionTypeSummariesAsync(
        [FromQuery] long startEpoch = 0,
        [FromQuery] long endEpoch = 0,
        CancellationToken cancellationToken = default)
    {
        LogExecutingGetQuestionTypeSummaries(_logger);
        long endEpochValue = (endEpoch == 0 ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() : endEpoch);

        IEnumerable<DnsQuestionTypeSummary> summaries =
            await _statsRepository.GetQuestionTypeSummary(startEpoch, endEpochValue, cancellationToken);

        LogFetchedQuestionTypeSummaries(_logger);

        return Ok(summaries);
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Received request to fetch hourly DNS event summaries.")]
    private static partial void LogExecutingGetHourlyDnsEventSummaries(ILogger logger);

    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "Successfully retrieved {Count} hourly DNS event summary records.")]
    private static partial void LogFetchedHourlyDnsEventSummaries(ILogger logger, int count);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Received request to fetch DNS question type summaries.")]
    private static partial void LogExecutingGetQuestionTypeSummaries(ILogger logger);

    [LoggerMessage(EventId = 4, Level = LogLevel.Debug, Message = "Successfully retrieved DNS question type summary records.")]
    private static partial void LogFetchedQuestionTypeSummaries(ILogger logger);
}
