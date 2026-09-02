// File: src/Astrolabed.Dns.Api/Controllers/FilterRuleStoreController.cs
using System.ComponentModel.DataAnnotations;
using System.Net.Mime;

using Astrolabed.Data.Pagination;
using Astrolabed.Dns.Filtering;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Astrolabed.Dns.Api.Controllers;

/// <summary>
/// Options for configuring API settings for the filter rule store endpoints.
/// </summary>
public sealed class FilterRuleStoreControllerOptions
{
    /// <summary>
    /// Gets or sets the default page size when none is specified in requests.
    /// Default value is 20.
    /// </summary>
    public int DefaultPageSize { get; set; } = 20;

    /// <summary>
    /// Gets or sets the maximum allowed page size to prevent memory exhaustion.
    /// Default value is 500.
    /// </summary>
    public int MaxPageSize { get; set; } = 500;
}

/// <summary>
/// Data transfer object representing a serialized filter rule for API responses.
/// </summary>
/// <param name="Pattern">The domain or regular expression pattern matched by this rule.</param>
/// <param name="RuleKind">The rule evaluation type, such as exact domain or regex pattern.</param>
/// <param name="IsAllow">A value indicating whether this is an allowlist (<see langword="true"/>) or blocklist (<see langword="false"/>) rule.</param>
/// <param name="ListId">The identifier of the source list containing this rule.</param>
/// <param name="IpAddress">The optional IP address bound to this rule, represented as a string.</param>
/// <param name="HasIpAddress">A value indicating whether this rule has an associated IP address.</param>
public sealed record FilterRuleDto(
    string Pattern,
    RuleKind RuleKind,
    bool IsAllow,
    int ListId,
    string? IpAddress,
    bool HasIpAddress);

/// <summary>
/// Controller providing API access to stored DNS filter rules.
/// </summary>
[ApiController]
[Route("api/dns/rules")]
[Produces(MediaTypeNames.Application.Json)]
public class FilterRuleStoreController : ControllerBase
{
    /// <summary>
    /// The backing filter rule store instance.
    /// </summary>
    private readonly IFilterRuleStore _ruleStore;

    /// <summary>
    /// Structured logger instance for the controller.
    /// </summary>
    private readonly ILogger<FilterRuleStoreController> _logger;

    /// <summary>
    /// Active configuration options for rule store controller actions.
    /// </summary>
    private readonly FilterRuleStoreControllerOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="FilterRuleStoreController"/> class.
    /// </summary>
    /// <param name="ruleStore">The filter rule store instance.</param>
    /// <param name="logger">The logger service instance.</param>
    /// <param name="options">Configuration options for rule store endpoints.</param>
    public FilterRuleStoreController(
        IFilterRuleStore ruleStore,
        ILogger<FilterRuleStoreController> logger,
        IOptions<FilterRuleStoreControllerOptions> options)
    {
        _ruleStore = ruleStore ?? throw new ArgumentNullException(nameof(ruleStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Retrieves a paged collection of active deduplicated filter rules.
    /// </summary>
    /// <param name="pageNumber">1-based page index for requesting a slice of results. Default is 1.</param>
    /// <param name="pageSize">Number of items returned per page. Default is 10.</param>
    /// <param name="listId">Identifier of the target list to filter rules by. Specify 0 to return rules across all lists.</param>
    /// <param name="IsAllow">Optional filter to restrict results to allowlist (<see langword="true"/>) or blocklist (<see langword="false"/>) rules.</param>
    /// <returns>A paged result set containing rule DTOs.</returns>
    /// <response code="200">Returns the requested paged set of filter rules.</response>
    /// <response code="400">If query validation fails or page parameters are invalid.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<FilterRuleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult GetPagedRules(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int listId = 0,
        [FromQuery] bool? IsAllow = null)
    {
        int effectivePageSize = pageSize;

        if (effectivePageSize > _options.MaxPageSize)
        {
            _logger.LogWarning(
                "Requested PageSize {RequestedSize} exceeds maximum allowed size {MaxSize}. Clamping to maximum.",
                effectivePageSize,
                _options.MaxPageSize);

            effectivePageSize = _options.MaxPageSize;
        }

        _logger.LogInformation(
            "Fetching paged rules. PageNumber: {PageNumber}, PageSize: {PageSize}, ListId: {ListId}, IsAllowFilter: {IsAllow}",
            pageNumber,
            pageSize,
            listId,
            IsAllow);

        PagedResult<FilterRule> domainPagedResult = _ruleStore.GetPagedRules(
            pageNumber,
            effectivePageSize,
            listId,
            IsAllow);

        IReadOnlyList<FilterRuleDto> dtoItems = domainPagedResult.Items
            .Select(rule => new FilterRuleDto(
                rule.Pattern,
                rule.RuleKind,
                rule.IsAllow,
                rule.ListId,
                rule.IpAddress?.ToString(),
                rule.HasIpAddress))
            .ToList();

        var dtoPagedResult = PagedResult<FilterRuleDto>.Create(
            dtoItems,
            domainPagedResult.TotalCount,
            domainPagedResult.CurrentPage,
            domainPagedResult.PageSize);

        return Ok(dtoPagedResult);
    }
}
