using System.Net;
using System.Net.NetworkInformation;

using Astrolabed;
using Astrolabed.Dhcp;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authorization;

namespace Astrolabed.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class ReservationsController : ControllerBase
{
    private readonly DhcpOptions _options;
    private readonly ILogger<ReservationsController> _logger;

    public ReservationsController(
        IOptions<DhcpOptions> options,
        ILogger<ReservationsController> logger)
    {
        ArgumentNullException.ThrowIfNull(options);

        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Creates a new static MAC-to-IP binding reservation.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult CreateReservation([FromBody] CreateReservationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!PhysicalAddress.TryParse(request.Mac, out var mac))
        {
            _logger.LogWarning("Failed to create reservation due to invalid MAC: {Mac}", request.Mac);
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid Request",
                Detail = $"The provided MAC address '{request.Mac}' is invalid."
            });
        }

        if (!IPAddress.TryParse(request.Ip, out var ip))
        {
            _logger.LogWarning("Failed to create reservation due to invalid IP: {Ip}", request.Ip);
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid Request",
                Detail = $"The provided IP address '{request.Ip}' is invalid."
            });
        }

        _logger.LogInformation(
            "Creating static reservation: MAC {Mac} -> IP {Ip} ({Hostname}.{Domain})",
            mac, ip, request.ClientName, _options.DomainName);

        var createdReservation = new StaticReservationResponse
        {
            Mac = mac.ToString(),
            Ip = ip.ToString(),
            ClientName = request.ClientName,
            FullyQualifiedDomainName = $"{request.ClientName}.{_options.DomainName}",
            CreatedAt = DateTimeOffset.UtcNow
        };

        return CreatedAtAction(
            nameof(GetReservationByMac),
            new { mac = createdReservation.Mac },
            createdReservation);
    }

    /// <summary>
    /// Fetches a specific static reservation by MAC address.
    /// </summary>
    [HttpGet("{mac}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetReservationByMac([FromRoute] string mac)
    {
        if (!PhysicalAddress.TryParse(mac, out var parsedMac))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid MAC Address",
                Detail = $"'{mac}' is not a valid hardware address format."
            });
        }

        _logger.LogDebug("Querying static reservation store for MAC: {Mac}", parsedMac);

        // Simulation placeholder - would be backed by static reservation repository
        return NotFound(new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Reservation Not Found",
            Detail = $"No static reservation found for MAC address '{parsedMac}'."
        });
    }
}

public sealed class CreateReservationRequest
{
    public required string Mac { get; set; }
    public required string Ip { get; set; }
    public required string ClientName { get; set; }
}

public sealed class StaticReservationResponse
{
    public required string Mac { get; set; }
    public required string Ip { get; set; }
    public required string ClientName { get; set; }
    public required string FullyQualifiedDomainName { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
}
