using System;
using System.Threading;
using System.Threading.Tasks;

using Astrolabed.Api.Services;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;

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
