namespace Astrolabed.Api.Controllers;

using System.Net;

using Astrolabed.Data.Models;
using Astrolabed.Data.Pagination;
using Astrolabed.Data.Repositories;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

/// <summary>
/// Data transfer object representing an allocation or renewal request for a DHCP lease.
/// </summary>
/// <param name="ClientId">The unique client identifier (e.g., DUID or Option 61 value).</param>
/// <param name="ClientName">The host name announced by the client.</param>
/// <param name="MacAddress">The hardware MAC address of the requesting client interface.</param>
/// <param name="RequestedIp">The requested or assigned IP address string (IPv4 or IPv6).</param>
/// <param name="DurationInSeconds">The requested lease duration in seconds.</param>
public sealed record AllocateOrUpdateDhcpLeaseRequest(
    string ClientId,
    string ClientName,
    string MacAddress,
    string RequestedIp,
    long DurationInSeconds);

/// <summary>
/// Data transfer object representing a DHCP lease release request.
/// </summary>
/// <param name="ClientId">The unique client identifier releasing the lease.</param>
/// <param name="MacAddress">The hardware MAC address releasing the lease.</param>
public sealed record ReleaseDhcpLeaseRequest(
    string ClientId,
    string MacAddress);

/// <summary>
/// Data transfer object representing a paginated response collection.
/// </summary>
/// <typeparam name="T">The type of item contained within the page.</typeparam>
/// <param name="Items">The items for the current page.</param>
/// <param name="TotalCount">The total number of items across all pages.</param>
/// <param name="PageNumber">The current 1-based page number.</param>
/// <param name="PageSize">The page size used for pagination.</param>
/// <param name="TotalPages">The total number of available pages.</param>
/// <param name="HasPreviousPage">Indicates whether a previous page exists.</param>
/// <param name="HasNextPage">Indicates whether a next page exists.</param>
public sealed record PagedResultDto<T>(
    IReadOnlyCollection<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage);

/// <summary>
/// Provides HTTP REST endpoints for inspecting, listing, allocating, checking, and releasing DHCP leases.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class DhcpController : ControllerBase
{
    private readonly IDhcpLeaseRepository _repository;
    private readonly ILogger<DhcpController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DhcpController"/> class.
    /// </summary>
    /// <param name="repository">The DHCP lease repository instance.</param>
    /// <param name="logger">The controller logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when required dependencies are null.</exception>
    public DhcpController(
        IDhcpLeaseRepository repository,
        ILogger<DhcpController> logger)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(logger);

        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a paginated list of all DHCP leases.
    /// </summary>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/dhcp/leases?pageNumber=1&amp;pageSize=20
    ///
    /// </remarks>
    /// <param name="pageNumber">The 1-based page number to retrieve. Defaults to 1.</param>
    /// <param name="pageSize">The maximum number of records per page. Defaults to 10.</param>
    /// <param name="cancellationToken">Cancellation token for cancelling the operation.</param>
    /// <returns>A <see cref="PagedResultDto{DhcpLease}"/> containing the requested page of DHCP leases.</returns>
    /// <response code="200">The paginated lease records were retrieved successfully.</response>
    /// <response code="400">The pageNumber or pageSize parameter was invalid (less than 1).</response>
    /// <response code="500">An unexpected error occurred while querying the repository.</response>
    [HttpGet("leases")]
    [ProducesResponseType(typeof(PagedResultDto<DhcpLease>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResultDto<DhcpLease>>> GetLeases(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1)
        {
            _logger.LogWarning("GetLeases called with invalid pageNumber: {PageNumber}", pageNumber);
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid Pagination Parameter",
                Detail = "The 'pageNumber' query parameter must be greater than or equal to 1."
            });
        }

        if (pageSize < 1)
        {
            _logger.LogWarning("GetLeases called with invalid pageSize: {PageSize}", pageSize);
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid Pagination Parameter",
                Detail = "The 'pageSize' query parameter must be greater than or equal to 1."
            });
        }

        _logger.LogDebug("Retrieving paged DHCP leases. PageNumber: {PageNumber}, PageSize: {PageSize}", pageNumber, pageSize);

        PagedResult<DhcpLease> pagedLeases = await _repository
            .GetLeasesAsync(pageNumber, pageSize, cancellationToken)
            .ConfigureAwait(false);

        return Ok(pagedLeases);
    }

    /// <summary>
    /// Retrieves a DHCP lease matching either a client identifier or a hardware MAC address.
    /// </summary>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/dhcp/lease?clientId=duid-12345&amp;macAddress=00:11:22:33:44:55
    ///
    /// At least one of <paramref name="clientId"/> or <paramref name="macAddress"/> must be provided.
    /// </remarks>
    /// <param name="clientId">Optional client identifier (Option 61 / DUID).</param>
    /// <param name="macAddress">Optional hardware MAC address.</param>
    /// <param name="cancellationToken">Cancellation token for cancelling the operation.</param>
    /// <returns>The matching <see cref="DhcpLease"/> record.</returns>
    /// <response code="200">The matching lease record was found and returned.</response>
    /// <response code="400">Neither clientId nor macAddress was provided.</response>
    /// <response code="404">No matching DHCP lease was found.</response>
    /// <response code="500">An unexpected error occurred while querying the repository.</response>
    [HttpGet("lease")]
    [ProducesResponseType(typeof(DhcpLease), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DhcpLease>> GetLeaseByClientIdOrMac(
        [FromQuery] string? clientId = null,
        [FromQuery] string? macAddress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clientId) && string.IsNullOrWhiteSpace(macAddress))
        {
            _logger.LogWarning("GetLeaseByClientIdOrMac called without clientId or macAddress.");
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Missing Query Parameter",
                Detail = "At least one query parameter ('clientId' or 'macAddress') must be provided."
            });
        }

        string searchClientId = clientId ?? string.Empty;
        string searchMacAddress = macAddress ?? string.Empty;

        _logger.LogDebug(
            "Searching for DHCP lease by ClientId: {ClientId}, MAC: {MacAddress}",
            searchClientId,
            searchMacAddress);

        DhcpLease? lease = await _repository
            .GetLeaseByClientIdOrMacAsync(searchClientId, searchMacAddress, cancellationToken)
            .ConfigureAwait(false);

        if (lease is null)
        {
            _logger.LogInformation(
                "No DHCP lease found for ClientId: {ClientId}, MAC: {MacAddress}",
                searchClientId,
                searchMacAddress);

            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Lease Not Found",
                Detail = "No DHCP lease was found matching the specified client identifier or MAC address."
            });
        }

        return Ok(lease);
    }

    /// <summary>
    /// Retrieves an active DHCP lease assigned to a PTR domain name address.
    /// </summary>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/dhcp/lease/ptr?ptrAddress=50.1.168.192.in-addr.arpa
    ///
    /// </remarks>
    /// <param name="ptrAddress">The PTR domain address to query.</param>
    /// <param name="cancellationToken">Cancellation token for cancelling the operation.</param>
    /// <returns>The matching active <see cref="DhcpLease"/> record.</returns>
    /// <response code="200">An active DHCP lease matching the PTR address was found.</response>
    /// <response code="400">The provided PTR address was null or empty.</response>
    /// <response code="404">No active lease was found for the specified PTR address.</response>
    /// <response code="500">An unexpected error occurred while querying the repository.</response>
    [HttpGet("lease/ptr")]
    [ProducesResponseType(typeof(DhcpLease), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DhcpLease>> GetLeaseByPtrAddress(
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

        _logger.LogDebug("Retrieving DHCP lease by PTR address: {PtrAddress}", ptrAddress);

        DhcpLease? lease = await _repository
            .GetLeaseByPtrAddressAsync(ptrAddress, cancellationToken)
            .ConfigureAwait(false);

        if (lease is null)
        {
            _logger.LogInformation("No active DHCP lease found for PTR address: {PtrAddress}", ptrAddress);
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Lease Not Found",
                Detail = $"No active DHCP lease was found matching PTR address '{ptrAddress}'."
            });
        }

        return Ok(lease);
    }

    /// <summary>
    /// Retrieves an active DHCP lease assigned to a specific IP address.
    /// </summary>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/dhcp/lease/ip/192.168.1.50
    ///
    /// </remarks>
    /// <param name="ipAddress">The IP address string (IPv4 or IPv6).</param>
    /// <param name="cancellationToken">Cancellation token for cancelling the operation.</param>
    /// <returns>The matching active <see cref="DhcpLease"/> record.</returns>
    /// <response code="200">An active DHCP lease matching the IP address was found.</response>
    /// <response code="400">The provided string is not a valid IP address.</response>
    /// <response code="404">No active lease was found for the specified IP address.</response>
    /// <response code="500">An unexpected error occurred while querying the repository.</response>
    [HttpGet("lease/ip/{ipAddress}")]
    [ProducesResponseType(typeof(DhcpLease), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DhcpLease>> GetLeaseByIp(
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

        _logger.LogDebug("Retrieving active DHCP lease for IP: {IpAddress}", parsedIp);

        DhcpLease? lease = await _repository
            .GetLeaseByIpAsync(parsedIp, cancellationToken)
            .ConfigureAwait(false);

        if (lease is null)
        {
            _logger.LogInformation("No active DHCP lease found for IP: {IpAddress}", parsedIp);
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Lease Not Found",
                Detail = $"No active DHCP lease was found for IP address '{ipAddress}'."
            });
        }

        return Ok(lease);
    }

    /// <summary>
    /// Checks whether a specific IP address is available for allocation to a client.
    /// </summary>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/dhcp/availability?ipAddress=192.168.1.50&amp;clientId=duid-12345
    ///
    /// Returns true if the IP is unassigned or currently assigned to the requesting client.
    /// </remarks>
    /// <param name="ipAddress">The IP address to check.</param>
    /// <param name="clientId">The client identifier requesting allocation.</param>
    /// <param name="cancellationToken">Cancellation token for cancelling the operation.</param>
    /// <returns>A boolean indicating whether the IP address is available.</returns>
    /// <response code="200">Check completed successfully.</response>
    /// <response code="400">The IP address or client ID parameter was missing or invalid.</response>
    /// <response code="500">An unexpected error occurred while checking availability.</response>
    [HttpGet("availability")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<bool>> IsIpAvailable(
        [FromQuery] string ipAddress,
        [FromQuery] string clientId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid Client Identifier",
                Detail = "The 'clientId' query parameter cannot be null or empty."
            });
        }

        if (!IPAddress.TryParse(ipAddress, out IPAddress? parsedIp))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid IP Address",
                Detail = $"The string '{ipAddress}' is not a valid IPv4 or IPv6 address."
            });
        }

        _logger.LogDebug(
            "Checking availability of IP: {IpAddress} for ClientId: {ClientId}",
            parsedIp,
            clientId);

        bool isAvailable = await _repository
            .IsIpAvailableAsync(parsedIp, clientId, cancellationToken)
            .ConfigureAwait(false);

        return Ok(isAvailable);
    }

    /// <summary>
    /// Allocates a new DHCP lease or updates/renews an existing lease duration.
    /// </summary>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/dhcp/lease
    ///     {
    ///         "clientId": "duid-12345",
    ///         "clientName": "workstation-01",
    ///         "macAddress": "00:11:22:33:44:55",
    ///         "requestedIp": "192.168.1.50",
    ///         "durationInSeconds": 86400
    ///     }
    ///
    /// </remarks>
    /// <param name="request">The allocation or renewal parameters.</param>
    /// <param name="cancellationToken">Cancellation token for cancelling the operation.</param>
    /// <returns>The allocated or updated <see cref="DhcpLease"/> record.</returns>
    /// <response code="200">The DHCP lease was successfully allocated or updated.</response>
    /// <response code="400">The request payload was invalid (e.g., malformed IP address or invalid fields).</response>
    /// <response code="500">An unexpected error occurred during lease allocation.</response>
    [HttpPost("lease")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(DhcpLease), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DhcpLease>> AllocateOrUpdateLease(
        [FromBody] AllocateOrUpdateDhcpLeaseRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.ClientId) || string.IsNullOrWhiteSpace(request.MacAddress))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid Identifiers",
                Detail = "Both 'clientId' and 'macAddress' must be provided."
            });
        }

        if (string.IsNullOrWhiteSpace(request.ClientName))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid Client Name",
                Detail = "The 'clientName' field cannot be empty."
            });
        }

        if (!IPAddress.TryParse(request.RequestedIp, out IPAddress? parsedIp))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid IP Address",
                Detail = $"The string '{request.RequestedIp}' is not a valid IPv4 or IPv6 address."
            });
        }

        if (request.DurationInSeconds <= 0)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid Lease Duration",
                Detail = "Lease duration must be greater than zero seconds."
            });
        }

        TimeSpan duration = TimeSpan.FromSeconds(request.DurationInSeconds);

        _logger.LogInformation(
            "Allocating/updating DHCP lease for ClientId: {ClientId}, IP: {IpAddress}, Duration: {Duration}",
            request.ClientId,
            parsedIp,
            duration);

        DhcpLease lease = await _repository
            .AllocateOrUpdateLeaseAsync(
                request.ClientId,
                request.ClientName,
                request.MacAddress,
                parsedIp,
                duration,
                cancellationToken)
            .ConfigureAwait(false);

        return Ok(lease);
    }

    /// <summary>
    /// Marks an active DHCP lease as released or inactive (DHCPRELEASE).
    /// </summary>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/dhcp/lease/release
    ///     {
    ///         "clientId": "duid-12345",
    ///         "macAddress": "00:11:22:33:44:55"
    ///     }
    ///
    /// </remarks>
    /// <param name="request">The client parameters releasing the lease.</param>
    /// <param name="cancellationToken">Cancellation token for cancelling the operation.</param>
    /// <returns>No content on successful release execution.</returns>
    /// <response code="204">The lease was successfully released.</response>
    /// <response code="400">The request payload contained invalid or empty client identifiers.</response>
    /// <response code="500">An unexpected error occurred during lease release.</response>
    [HttpPost("lease/release")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ReleaseLease(
        [FromBody] ReleaseDhcpLeaseRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.ClientId) || string.IsNullOrWhiteSpace(request.MacAddress))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid Client Identifiers",
                Detail = "Both 'clientId' and 'macAddress' must be provided to release a lease."
            });
        }

        _logger.LogInformation(
            "Releasing DHCP lease for ClientId: {ClientId}, MAC: {MacAddress}",
            request.ClientId,
            request.MacAddress);

        await _repository
            .ReleaseLeaseAsync(request.ClientId, request.MacAddress, cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation("Successfully processed release for ClientId: {ClientId}", request.ClientId);

        return NoContent();
    }

}
