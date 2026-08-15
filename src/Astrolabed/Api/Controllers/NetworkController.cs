using System.Net.Mime;

using Astrolabed.Api.Services;
using Astrolabed.Utilities;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Astrolabed.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class NetworkController : ControllerBase
{
    private readonly ICrossPlatformLanScannerService _lanScannerService;
    private readonly ILogger<NetworkController> _logger;

    public NetworkController(
        ICrossPlatformLanScannerService lanScannerService,
        ILogger<NetworkController> logger)
    {
        _lanScannerService = lanScannerService ?? throw new ArgumentNullException(nameof(lanScannerService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Scans the local network (LAN) and returns a list of active discovered devices.
    /// </summary>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>A list of discovered LAN devices with IP, MAC address, and hostname.</returns>
    [HttpGet("devices")]
    [ProducesResponseType(typeof(IReadOnlyCollection<DiscoveredLanDeviceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyCollection<DiscoveredLanDeviceDto>>> GetDiscoveredDevicesAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("HTTP GET api/network/devices requested.");

        try
        {
            var devices = await _lanScannerService.ScanLanAsync(cancellationToken);

            var deviceDtos = devices.Select(d => new DiscoveredLanDeviceDto(
                d.IpAddress.ToString(),
                d.MacAddress,
                d.HostName
            )).ToList();

            return Ok(deviceDtos);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Network scan request was canceled by the client.");
            return StatusCode(StatusCodes.Status499ClientClosedRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled error occurred while scanning the network.");
            return Problem(
                detail: "An unexpected error occurred while scanning the local network.",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}

public record DiscoveredLanDeviceDto(string IpAddress, string MacAddress, string? HostName);
