namespace Astrolabed.Api.Controllers;

using Astrolabed.Data.Pagination;
using Astrolabed.Dns.Cache;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

/// <summary>
/// Strongly typed response model representing the DNS cache count state.
/// </summary>
/// <param name="Count">The total number of active items stored in the DNS cache.</param>
/// <param name="Timestamp">The UTC timestamp when the count was retrieved.</param>
public sealed record CacheCountResponse(int Count, DateTimeOffset Timestamp);

/// <summary>
/// Provides HTTP endpoints for managing and inspecting the in-memory DNS cache.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class CacheController : ControllerBase
{
    private readonly IDnsCache _dnsCache;
    private readonly ILogger<CacheController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheController"/> class.
    /// </summary>
    /// <param name="dnsCache">The DNS cache domain service instance.</param>
    /// <param name="logger">The controller logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when required dependencies are null.</exception>
    public CacheController(
        IDnsCache dnsCache,
        ILogger<CacheController> logger)
    {
        ArgumentNullException.ThrowIfNull(dnsCache);
        ArgumentNullException.ThrowIfNull(logger);

        _dnsCache = dnsCache;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves the current total count of items stored in the DNS cache.
    /// </summary>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/cache/count
    ///
    /// Useful for telemetry, health checks, and capacity monitoring.
    /// </remarks>
    /// <returns>A <see cref="CacheCountResponse"/> containing item count and timestamp.</returns>
    /// <response code="200">Successfully retrieved the cache item count.</response>
    /// <response code="500">An unexpected internal error occurred.</response>
    [HttpGet("count")]
    [ProducesResponseType(typeof(CacheCountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public ActionResult<CacheCountResponse> GetCount()
    {
        _logger.LogDebug("Fetching current DNS cache item count.");

        CacheCountResponse response = new(_dnsCache.Count, DateTimeOffset.UtcNow);
        return Ok(response);
    }

    /// <summary>
    /// Retrieves a paginated list of entries currently held in the DNS cache.
    /// </summary>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/cache?pageNumber=1&amp;pageSize=10
    ///
    /// Note: Page size is constrained between 1 and 100 items.
    /// </remarks>
    /// <param name="pageNumber">1-based page index. Must be greater than or equal to 1. Defaults to 1.</param>
    /// <param name="pageSize">Number of items per page. Clamped between 1 and 100. Defaults to 10.</param>
    /// <returns>A paged result container holding <see cref="CacheEntryView"/> items.</returns>
    /// <response code="200">Successfully retrieved the requested page of cache entries.</response>
    /// <response code="400">The provided page number or page size was invalid.</response>
    /// <response code="500">An unexpected internal error occurred.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CacheEntryView>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public ActionResult<PagedResult<KeyValuePair<string, CacheEntryView>>> GetCachedRecords(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        if (pageNumber < 1)
        {
            _logger.LogWarning("Invalid page number requested: {PageNumber}", pageNumber);
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid Pagination Parameter",
                Detail = "Page number must be greater than or equal to 1."
            });
        }

        int normalizedPageSize = Math.Clamp(pageSize, 1, 100);

        _logger.LogInformation(
            "Retrieving paged cache entries. Page: {PageNumber}, Size: {PageSize}",
            pageNumber,
            normalizedPageSize);

        PagedResult<KeyValuePair<string, CacheEntryView>> result = _dnsCache.ToPagedResult(pageNumber, normalizedPageSize);

        return Ok(result);
    }

    /// <summary>
    /// Purges all entries from the DNS cache.
    /// </summary>
    /// <remarks>
    /// Sample request:
    ///
    ///     DELETE /api/cache
    ///
    /// Performs a thread-safe total clear of all cached DNS resolution records.
    /// </remarks>
    /// <returns>No content on success.</returns>
    /// <response code="204">The cache was successfully cleared.</response>
    /// <response code="500">An unexpected internal error occurred.</response>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult ClearCache()
    {
        int previousCount = _dnsCache.Count;

        _dnsCache.Clear();

        _logger.LogInformation("DNS cache cleared. Evicted {EvictedCount} items.", previousCount);

        return NoContent();
    }
}

