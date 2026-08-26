namespace Astrolabed.Api.Controllers;

using System.Net;

using Astrolabed.Data.Models;
using Astrolabed.Data.Pagination;
using Astrolabed.Data.Repositories;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

/// <summary>
/// Data transfer object representing a request to purge stale LAN device records.
/// </summary>
/// <param name="Cutoff">The UTC cutoff timestamp. Records observed prior to this date will be purged.</param>
public sealed record CleanupStaleDevicesRequest(
    DateTimeOffset Cutoff);

/// <summary>
/// Data transfer object representing a discovered LAN device.
/// </summary>
/// <param name="MacAddress">The hardware MAC address of the device.</param>
/// <param name="IpAddress">The IP address string of the device.</param>
/// <param name="HostName">The host name or DNS name of the device.</param>
/// <param name="LastSeen">The timestamp when the device was last observed.</param>
public sealed record DiscoveredLanDeviceDto(
    string MacAddress,
    string IpAddress,
    string? HostName,
    DateTimeOffset LastSeen);

/// <summary>
/// Provides REST API endpoints for LAN device discovery operations, network sweeps,
/// and device lifecycle management.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class NetworkController : ControllerBase
{
    private readonly IDiscoveredLanDeviceRepository _repository;
    private readonly ILogger<NetworkController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NetworkController"/> class.
    /// </summary>
    /// <param name="repository">The LAN device discovery repository instance.</param>
    /// <param name="logger">The controller logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when required dependencies are null.</exception>
    public NetworkController(
        IDiscoveredLanDeviceRepository repository,
        ILogger<NetworkController> logger)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(logger);

        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a paginated list of discovered LAN devices.
    /// </summary>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/network/devices?pageNumber=1&amp;pageSize=20
    ///
    /// </remarks>
    /// <param name="pageNumber">The 1-based page index to retrieve. Defaults to 1.</param>
    /// <param name="pageSize">The maximum number of items per page. Defaults to 10.</param>
    /// <param name="cancellationToken">Cancellation token for cancelling the operation.</param>
    /// <returns>A <see cref="PagedResult{DiscoveredLanDeviceDto}"/> containing the matching device page.</returns>
    /// <response code="200">The paginated device list was successfully retrieved.</response>
    /// <response code="400">The pageNumber or pageSize parameter was invalid (less than 1).</response>
    /// <response code="500">An unexpected error occurred while querying the repository.</response>
    [HttpGet("devices")]
    [ProducesResponseType(typeof(PagedResult<DiscoveredLanDeviceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResult<DiscoveredLanDeviceDto>>> GetPagedDevices(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1)
        {
            _logger.LogWarning("GetPagedDevices called with invalid pageNumber: {PageNumber}", pageNumber);
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid Pagination Parameter",
                Detail = "The 'pageNumber' query parameter must be greater than or equal to 1."
            });
        }

        if (pageSize < 1)
        {
            _logger.LogWarning("GetPagedDevices called with invalid pageSize: {PageSize}", pageSize);
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid Pagination Parameter",
                Detail = "The 'pageSize' query parameter must be greater than or equal to 1."
            });
        }

        _logger.LogDebug("Retrieving paged LAN devices. PageNumber: {PageNumber}, PageSize: {PageSize}", pageNumber, pageSize);

        PagedResult<DiscoveredLanDevice> pagedDevices = await _repository
            .GetPagedAsync(pageNumber, pageSize, cancellationToken)
            .ConfigureAwait(false);

        return Ok(pagedDevices);
    }

    /// <summary>
    /// Retrieves a discovered LAN device record by its hardware MAC address.
    /// </summary>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/network/devices/mac/00:11:22:33:44:55
    ///
    /// </remarks>
    /// <param name="macAddress">The hardware MAC address of the device.</param>
    /// <param name="cancellationToken">Cancellation token for cancelling the operation.</param>
    /// <returns>The matching <see cref="DiscoveredLanDeviceDto"/> record.</returns>
    /// <response code="200">The matching LAN device record was found and returned.</response>
    /// <response code="400">The provided MAC address was null or whitespace.</response>
    /// <response code="404">No LAN device was found for the specified MAC address.</response>
    /// <response code="500">An unexpected error occurred while querying the repository.</response>
    [HttpGet("devices/mac/{macAddress}")]
    [ProducesResponseType(typeof(DiscoveredLanDeviceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DiscoveredLanDeviceDto>> GetByMacAddress(
        string macAddress,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(macAddress))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid MAC Address",
                Detail = "The 'macAddress' path parameter cannot be null or empty."
            });
        }

        _logger.LogDebug("Retrieving LAN device by MAC address: {MacAddress}", macAddress);

        DiscoveredLanDevice? device = await _repository
            .GetByMacAddressAsync(macAddress, cancellationToken)
            .ConfigureAwait(false);

        if (device is null)
        {
            _logger.LogInformation("No LAN device found for MAC address: {MacAddress}", macAddress);
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Device Not Found",
                Detail = $"No discovered LAN device was found matching MAC address '{macAddress}'."
            });
        }

        return Ok(MapToDto(device));
    }

    /// <summary>
    /// Retrieves a discovered LAN device record by its assigned IP address.
    /// </summary>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/network/devices/ip/192.168.1.100
    ///
    /// </remarks>
    /// <param name="ipAddress">The IP address string (IPv4 or IPv6).</param>
    /// <param name="cancellationToken">Cancellation token for cancelling the operation.</param>
    /// <returns>The matching <see cref="DiscoveredLanDeviceDto"/> record.</returns>
    /// <response code="200">The matching LAN device record was found and returned.</response>
    /// <response code="400">The provided string is not a valid IP address.</response>
    /// <response code="404">No LAN device was found for the specified IP address.</response>
    /// <response code="500">An unexpected error occurred while querying the repository.</response>
    [HttpGet("devices/ip/{ipAddress}")]
    [ProducesResponseType(typeof(DiscoveredLanDeviceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DiscoveredLanDeviceDto>> GetByIpAddress(
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (!IPAddress.TryParse(ipAddress, out IPAddress? parsedIp))
        {
            _logger.LogWarning("Invalid IP address provided: {IpAddress}", ipAddress);
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid IP Address",
                Detail = $"The string '{ipAddress}' is not a valid IPv4 or IPv6 address."
            });
        }

        _logger.LogDebug("Retrieving LAN device by IP address: {IpAddress}", parsedIp);

        DiscoveredLanDevice? device = await _repository
            .GetByIpAddressAsync(parsedIp, cancellationToken)
            .ConfigureAwait(false);

        if (device is null)
        {
            _logger.LogInformation("No LAN device found for IP address: {IpAddress}", parsedIp);
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Device Not Found",
                Detail = $"No discovered LAN device was found matching IP address '{ipAddress}'."
            });
        }

        return Ok(MapToDto(device));
    }

    /// <summary>
    /// Retrieves a discovered LAN device record by its Reverse DNS PTR domain name string.
    /// </summary>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/network/devices/ptr?ptrAddress=100.1.168.192.in-addr.arpa
    ///
    /// </remarks>
    /// <param name="ptrAddress">The PTR domain name string.</param>
    /// <param name="cancellationToken">Cancellation token for cancelling the operation.</param>
    /// <returns>The matching <see cref="DiscoveredLanDeviceDto"/> record.</returns>
    /// <response code="200">The matching LAN device record was found and returned.</response>
    /// <response code="400">The provided PTR address was null or empty.</response>
    /// <response code="404">No LAN device was found for the specified PTR address.</response>
    /// <response code="500">An unexpected error occurred while querying the repository.</response>
    [HttpGet("devices/ptr")]
    [ProducesResponseType(typeof(DiscoveredLanDeviceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DiscoveredLanDeviceDto>> GetByPtrAddress(
        [FromQuery] string ptrAddress,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ptrAddress))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid PTR Address",
                Detail = "The 'ptrAddress' query parameter cannot be null or empty."
            });
        }

        _logger.LogDebug("Retrieving LAN device by PTR address: {PtrAddress}", ptrAddress);

        DiscoveredLanDevice? device = await _repository
            .GetByPtrAddressAsync(ptrAddress, cancellationToken)
            .ConfigureAwait(false);

        if (device is null)
        {
            _logger.LogInformation("No LAN device found for PTR address: {PtrAddress}", ptrAddress);
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Device Not Found",
                Detail = $"No discovered LAN device was found matching PTR address '{ptrAddress}'."
            });
        }

        return Ok(MapToDto(device));
    }

    /// <summary>
    /// Removes stale LAN device records that have not been observed since the specified cutoff timestamp.
    /// </summary>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/network/devices/cleanup
    ///     {
    ///         "cutoff": "2026-08-20T00:00:00Z"
    ///     }
    ///
    /// </remarks>
    /// <param name="request">The cleanup parameters containing the cutoff timestamp threshold.</param>
    /// <param name="cancellationToken">Cancellation token for cancelling the operation.</param>
    /// <returns>No content on successful cleanup completion.</returns>
    /// <response code="204">The cleanup operation completed successfully.</response>
    /// <response code="400">The request payload was null or invalid.</response>
    /// <response code="500">An unexpected error occurred during the cleanup operation.</response>
    [HttpPost("devices/cleanup")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CleanOldData(
        [FromBody] CleanupStaleDevicesRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation("Initiating LAN device cleanup for records older than {Cutoff}", request.Cutoff);

        await _repository
            .CleanOldDataAsync(request.Cutoff, cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation("Successfully completed LAN device cleanup for cutoff {Cutoff}", request.Cutoff);

        return NoContent();
    }

    private static DiscoveredLanDeviceDto MapToDto(DiscoveredLanDevice device)
    {
        return new DiscoveredLanDeviceDto(
            device.MacAddress,
            device.IpAddress?.ToString() ?? string.Empty,
            device.HostName,
            device.LastSeen);
    }
}
