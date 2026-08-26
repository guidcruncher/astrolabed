using Astrolabed.Data.Pagination;
using Astrolabed.Dns.Cache;

using Microsoft.AspNetCore.Mvc;

namespace Astrolabed.Api.Controllers;

/// <summary>
/// Handles HTTP requests for Astrolabed API endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class CacheController : ControllerBase
{
    private readonly IDnsCache _dnsCache;
    private readonly ILogger<CacheController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheController"/> class.
    /// </summary>
    /// <param name="dnsCache">The Cache domain service.</param>
    /// <param name="logger">The controller logger instance.</param>
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
    /// Gets current Cache item count
    /// </summary>
    /// <returns>An <see cref="IActionResult"/> containing the status description.</returns>
    [HttpGet("count")]
    public IActionResult GetCount()
    {
        return Ok(new { Count = _dnsCache.Count, Timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Retrieves a paginated list of cache items
    /// </summary>
    /// <param name="pageNumber">1-based page index. Defaults to 1.</param>
    /// <param name="pageSize">Items per page. Defaults to configured value.</param>
    /// <returns>A paged result container containing the cached records.</returns>
    [HttpGet()]
    [ProducesResponseType(typeof(PagedResult<CacheEntry>), StatusCodes.Status200OK)]
    public ActionResult<PagedResult<CacheEntry>> GetCachedRecords(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        if (pageNumber < 1)
        {
            return BadRequest("Page number must be greater than or equal to 1.");
        }

        int normalizedPageSize = Math.Clamp(pageSize, 1, 100);

        PagedResult<CacheEntry> result = _dnsCache.ToPagedResult(pageNumber, normalizedPageSize);

        return Ok(result);
    }

    [HttpDelete()]
    public ActionResult ClearCache()
    {
        _dnsCache.Clear();
        return Ok();
    }

}
