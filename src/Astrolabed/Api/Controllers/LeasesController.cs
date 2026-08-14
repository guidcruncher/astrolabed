using System.Net;

using Astrolabed;
using Astrolabed.Dhcp;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
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
    /// Retrieves all DHCP leases currently in the store.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllLeases([FromQuery] bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching leases from store (activeOnly: {ActiveOnly})", activeOnly);

        var leases = await _leaseReader.GetAllLeasesAsync(cancellationToken);

        if (activeOnly)
        {
            var now = DateTimeOffset.UtcNow;
            leases = leases.Where(l => l.ExpiresAt > now).ToList();
        }

        _logger.LogDebug("Retrieved {Count} leases matching search criteria", leases.Count);
        return Ok(leases);
    }

    /// <summary>
    /// Looks up a specific lease by IP or MAC address string representation.
    /// </summary>
    [HttpGet("{identifier}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
