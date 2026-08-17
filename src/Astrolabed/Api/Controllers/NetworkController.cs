using System.ComponentModel.DataAnnotations;
using System.Net.Mime;

using Astrolabed.Api.Services;
using Astrolabed.Data;
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
    /// Scans the local network (LAN) and returns a paginated list of active discovered devices.
    /// </summary>
    /// <param name="pageNumber">Page number index (1-based).</param>
    /// <param name="pageSize">Number of records per page (1-1000).</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>A paginated result set containing discovered LAN devices.</returns>
    [HttpGet("devices")]
    [ProducesResponseType(typeof(PagedResult<DiscoveredLanDeviceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDiscoveredDevicesAsync(
        [FromQuery, Range(1, int.MaxValue)] int pageNumber = 1,
        [FromQuery, Range(1, 1000)] int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "HTTP GET api/network/devices requested with PageNumber: {PageNumber}, PageSize: {PageSize}.",
            pageNumber,
            pageSize);

        try
        {
            var devices = await _lanScannerService.ScanLanAsync(cancellationToken);
            var deviceList = devices.ToList();

            var totalCount = deviceList.Count;

            var pageds = deviceList
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new DiscoveredLanDeviceDto(
                    d.IpAddress.ToString(),
                    d.MacAddress,
                    d.HostName
                ))
                .ToList();

            var pagedResult = new PagedResult<DiscoveredLanDeviceDto>(
                pageds,
                totalCount,
                pageNumber,
                pageSize);

            return Ok(pagedResult);
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
