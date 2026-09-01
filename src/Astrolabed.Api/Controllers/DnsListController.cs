namespace Astrolabed.Api.Controllers;

using Astrolabed.Data.Models;
using Astrolabed.Data.Repositories;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

/// <summary>
/// Provides RESTful API endpoints for managing DNS list entities.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class DnsListController : ControllerBase
{
    private readonly IDnsListRepository _dnsListRepository;
    private readonly ILogger<DnsListController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DnsListController"/> class.
    /// </summary>
    /// <param name="dnsListRepository">The repository used to access and persist DNS list records.</param>
    /// <param name="logger">The logger instance for reporting operational events and diagnostics.</param>
    public DnsListController(
        IDnsListRepository dnsListRepository,
        ILogger<DnsListController> logger)
    {
        _dnsListRepository = dnsListRepository ?? throw new ArgumentNullException(nameof(dnsListRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Retrieves all configured DNS list entities.
    /// </summary>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A list of <see cref="DnsListEntity"/> items.</returns>
    /// <response code="200">Returns the list of retrieved DNS list entities.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyList<DnsListEntity>))]
    public async Task<ActionResult<IReadOnlyList<DnsListEntity>>> GetAll(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Request received to retrieve all DNS lists.");

        IReadOnlyList<DnsListEntity> entities = await _dnsListRepository.GetAllAsync(cancellationToken);

        _logger.LogInformation("Successfully retrieved {Count} DNS list record(s).", entities.Count);

        return Ok(entities);
    }

    /// <summary>
    /// Retrieves a specific DNS list entity by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the DNS list to retrieve.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>The requested <see cref="DnsListEntity"/> record if found.</returns>
    /// <response code="200">Returns the requested entity.</response>
    /// <response code="404">If no entity with the given identifier is found.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DnsListEntity))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DnsListEntity>> GetById(int id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Request received to retrieve DNS list with ID: {Id}.", id);

        DnsListEntity? entity = await _dnsListRepository.GetByIdAsync(id, cancellationToken);

        if (entity is null)
        {
            _logger.LogWarning("DNS list with ID: {Id} was not found.", id);
            return NotFound();
        }

        _logger.LogInformation("Successfully retrieved DNS list with ID: {Id}.", id);

        return Ok(entity);
    }

}
