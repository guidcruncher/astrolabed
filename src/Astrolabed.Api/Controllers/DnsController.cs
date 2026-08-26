namespace Astrolabed.Api.Controllers;

using Astrolabed.Data.Models;
using Astrolabed.Data.Pagination;
using Astrolabed.Data.Repositories;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

/// <summary>
/// Provides HTTP endpoints for inspecting, persisting, and managing DNS response event records.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class DnsController : ControllerBase
{
    private readonly IDnsResponseEventRepository _repository;
    private readonly ILogger<DnsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DnsController"/> class.
    /// </summary>
    /// <param name="repository">The DNS response event repository instance.</param>
    /// <param name="logger">The controller logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when required dependencies are null.</exception>
    public DnsController(
        IDnsResponseEventRepository repository,
        ILogger<DnsController> logger)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(logger);

        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a paged list of DNS response events ordered chronologically.
    /// </summary>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/dns?pageNumber=1&amp;pageSize=10
    ///
    /// Returns a paginated collection containing recorded DNS events.
    /// </remarks>
    /// <param name="pageNumber">1-based page index. Defaults to 1.</param>
    /// <param name="pageSize">Maximum number of items per page. Clamped between 1 and 100. Defaults to 10.</param>
    /// <param name="cancellationToken">Cancellation token for cancelling the underlying operation.</param>
    /// <returns>A paged container holding <see cref="DnsResponseEventEntity"/> items.</returns>
    /// <response code="200">Successfully retrieved the requested page of DNS events.</response>
    /// <response code="400">The page number or page size parameter was invalid.</response>
    /// <response code="500">An unexpected error occurred while querying the repository.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<DnsResponseEventEntity>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResult<DnsResponseEventEntity>>> GetPaged(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1)
        {
            _logger.LogWarning("Invalid page number requested: {PageNumber}", pageNumber);
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid Pagination Parameter",
                Detail = "Page number must be greater than or equal to 1."
            });
        }

        int normalizedPageSize = Math.Clamp(pageSize, 1, 100);

        _logger.LogInformation(
            "Fetching paged DNS events. Page: {PageNumber}, Size: {PageSize}",
            pageNumber,
            normalizedPageSize);

        PagedResult<DnsResponseEventEntity> entities = await _repository
            .GetPagedAsync(pageNumber, normalizedPageSize, cancellationToken)
            .ConfigureAwait(false);

        return Ok(entities);
    }

    /// <summary>
    /// Retrieves a specific DNS response event by its unique identifier.
    /// </summary>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/dns/evt_123456789
    ///
    /// </remarks>
    /// <param name="id">The unique identifier of the DNS event record.</param>
    /// <param name="cancellationToken">Cancellation token for cancelling the operation.</param>
    /// <returns>The matching <see cref="DnsResponseEventEntity"/> record.</returns>
    /// <response code="200">The matching DNS response event was found and returned.</response>
    /// <response code="400">The provided identifier was null or empty.</response>
    /// <response code="404">No DNS response event record was found matching the ID.</response>
    /// <response code="500">An unexpected error occurred while fetching the record.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(DnsResponseEventEntity), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DnsResponseEventEntity>> GetById(
        string id,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid Event Identifier",
                Detail = "The event ID parameter cannot be empty or whitespace."
            });
        }

        _logger.LogDebug("Retrieving DNS response event by ID: {EventId}", id);

        DnsResponseEventEntity? entity = await _repository
            .GetByIdAsync(id, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
        {
            _logger.LogInformation("DNS response event not found for ID: {EventId}", id);
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Event Not Found",
                Detail = $"No DNS response event was found matching ID '{id}'."
            });
        }

        return Ok(entity);
    }

    /// <summary>
    /// Executes a maintenance purge of historical DNS event data past the retention threshold.
    /// </summary>
    /// <remarks>
    /// Sample request:
    ///
    ///     DELETE /api/dns
    ///
    /// Invokes the underlying storage provider to remove historical records based on retention policy.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token for cancelling the operation.</param>
    /// <returns>No content on successful cleanup completion.</returns>
    /// <response code="204">Historical DNS records were successfully purged.</response>
    /// <response code="500">An unexpected error occurred during data purge.</response>
    [HttpDelete()]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CleanOldData(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing maintenance cleanup of historical DNS response events.");

        await _repository
            .CleanOldDataAsync(cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation("Historical DNS response event cleanup completed.");

        return NoContent();
    }

}
