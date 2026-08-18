using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Mime;

using Astrolabed;
using Astrolabed.Data;
using Astrolabed.Dhcp;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Authorize]
public class LeasesController : ControllerBase
{
    private readonly IDhcpLeaseReader _leaseReader;
    private readonly DhcpOptions _options;
    private readonly ILogger<LeasesController> _logger;

    public LeasesController(
        IDhcpLeaseReader leaseReader,
        IOptions<DhcpOptions> options,
        ILogger<LeasesController> logger)
    {
        ArgumentNullException.ThrowIfNull(options);

        _leaseReader = leaseReader ?? throw new ArgumentNullException(nameof(leaseReader));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Retrieves a paginated list of DHCP leases currently in the store.
    /// </summary>
    /// <param name="activeOnly">If true, filters to only return non-expired leases.</param>
    /// <param name="pageNumber">Page number index (1-based).</param>
    /// <param name="pageSize">Number of records per page (1-1000).</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>A paginated result set containing DHCP leases.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<DhcpLease>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllLeases(
        [FromQuery] bool activeOnly = true,
        [FromQuery, Range(1, int.MaxValue)] int pageNumber = 1,
        [FromQuery, Range(1, 1000)] int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Fetching leases from store (activeOnly: {ActiveOnly}, pageNumber: {PageNumber}, pageSize: {PageSize})",
            activeOnly,
            pageNumber,
            pageSize);

        try
        {
            var leases = await _leaseReader.GetAllLeasesAsync(cancellationToken);

            if (activeOnly)
            {
                var now = DateTimeOffset.UtcNow;
                leases = leases.Where(l => l.ExpiresAt > now).ToList();
            }

            var leaseList = leases.ToList();
            var totalCount = leaseList.Count;

            var pagedLeases = leaseList
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var pagedResult = new PagedResult<DhcpLease>(
                pagedLeases,
                totalCount,
                pageNumber,
                pageSize);

            _logger.LogDebug("Retrieved {Count} leases out of total {TotalCount}", pagedLeases.Count, totalCount);
            return Ok(pagedResult);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("DHCP leases retrieval request was canceled by the client.");
            return StatusCode(StatusCodes.Status499ClientClosedRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled error occurred while retrieving DHCP leases.");
            return Problem(
                detail: "An unexpected error occurred while retrieving DHCP leases.",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Looks up a specific lease by IP or MAC address string representation.
    /// </summary>
    [HttpGet("{identifier}")]
    [ProducesResponseType(typeof(DhcpLease), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetLeaseByIdentifier([FromRoute] string identifier, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

        if (IPAddress.TryParse(identifier, out var ipAddress))
        {
            _logger.LogInformation("Querying lease record for IP: {IP}", ipAddress);
            var leaseByIp = await _leaseReader.GetLeaseByIpAsync(ipAddress, cancellationToken);

            if (leaseByIp is null)
            {
                _logger.LogWarning("Lease not found for IP: {IP}", ipAddress);
                return NotFound(ProblemDetailsForNotFound($"No lease found matching IP address '{ipAddress}'."));
            }

            return Ok(leaseByIp);
        }

        _logger.LogWarning("Invalid identifier parameter supplied: {Identifier}", identifier);
        return BadRequest(ProblemDetailsForBadRequest($"'{identifier}' is not a valid IP address."));
    }

    private ProblemDetails ProblemDetailsForNotFound(string detail) =>
        new()
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Lease Not Found",
            Detail = detail,
            Instance = HttpContext.Request.Path
        };

    private ProblemDetails ProblemDetailsForBadRequest(string detail) =>
        new()
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Invalid Identifier",
            Detail = detail,
            Instance = HttpContext.Request.Path
        };
}
