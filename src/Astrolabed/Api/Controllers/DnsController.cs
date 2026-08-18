using System;
using System.Threading;
using System.Threading.Tasks;

using Astrolabed.Api.Services;
using Astrolabed.Data;
using Astrolabed.Dns;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Astrolabed.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class DnsController : ControllerBase
{
    private readonly IDnsService _dnsService;
    private readonly ILogger<DnsController> _logger;

    public DnsController(IDnsService dnsService, ILogger<DnsController> logger)
    {
        _dnsService = dnsService ?? throw new ArgumentNullException(nameof(dnsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Flushes the DNS Cache
    /// </summary>
    [HttpDelete("cache")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> FlushCache()
    {
        _dnsService.FlushCache();
        return Ok();
    }

    /// <summary>
    /// Retrieves paged results from the DNS response cache.
    /// </summary>
    /// <param name="pageNumber">Target page number (defaults to 1)</param>
    /// <param name="pageSize">Number of items per page (defaults to 10)</param>
    /// <param name="search">Optional search filter string for domain or query type</param>
    [HttpGet("cache")]
    [ProducesResponseType(typeof(PagedResult<DnsResponse>), StatusCodes.Status200OK)]
    public IActionResult GetCachedResponses(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        _logger.LogInformation("Retrieving paged cached DNS responses. PageNumber={PageNumber}, PageSize={PageSize}, Search={Search}", pageNumber, pageSize, search);

        var result = _dnsService.GetCachedResponsesPaged(pageNumber, pageSize, search);
        return Ok(result);
    }

    /// <summary>
    /// Performs a DNS lookup for a record name and type.
    /// </summary>
    /// <param name="name">Domain or hostname to resolve (e.g. "gateway.home.arpa")</param>
    /// <param name="type">Record type (e.g. "A", "AAAA", "MX", "TXT", "PTR"). Default: "A"</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("query")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Query(
        [FromQuery] string name,
        [FromQuery] string type = "A",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Missing Parameter",
                Detail = "The 'name' query parameter is required."
            });
        }

        _logger.LogInformation("Processing DNS query request for Name={Name}, Type={Type}", name, type);

        try
        {
            var response = await _dnsService.QueryAsync(name, type, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete DNS query for Name={Name}", name);

            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "DNS Resolution Error",
                Detail = "An error occurred while resolving the requested DNS record.",
                Instance = HttpContext.Request.Path
            });
        }
    }
}
