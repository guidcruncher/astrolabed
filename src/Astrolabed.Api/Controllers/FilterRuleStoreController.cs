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
/// Represents a query parameter model for requesting paged filter rules.
/// </summary>
public sealed class GetPagedRulesQueryParameters
{
    /// <summary>
    /// Gets or sets the 1-based page number. Defaults to 1.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "PageNumber must be greater than or equal to 1.")]
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Gets or sets the requested number of items per page.
    /// </summary>
    [Range(1, 1000, ErrorMessage = "PageSize must be between 1 and 1000.")]
    public int? PageSize { get; set; }

    /// <summary>
    /// Gets or sets List to filter by
    /// </summary>
    public int ListId { get; set; } = 0;

    /// <summary>
    /// Gets or sets an optional filter scope: <c>true</c> for allowlist rules, <c>false</c> for blocklist rules, or <c>null</c> for all.
    /// </summary>
    public bool? IsAllow { get; set; }
}

/// <summary>
/// Data transfer object representing a serialized filter rule for API responses.
/// </summary>
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
    private readonly IFilterRuleStore _ruleStore;
    private readonly ILogger<FilterRuleStoreController> _logger;
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
    /// <param name="parameters">Query parameters for pagination and filtering.</param>
    /// <returns>A paged result set containing rule DTOs.</returns>
    /// <response code="200">Returns the requested paged set of filter rules.</response>
    /// <response code="400">If query validation fails or page parameters are invalid.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<FilterRuleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult GetPagedRules([FromQuery] GetPagedRulesQueryParameters parameters)
    {
        int effectivePageSize = parameters.PageSize ?? _options.DefaultPageSize;

        if (effectivePageSize > _options.MaxPageSize)
        {
            _logger.LogWarning(
                "Requested PageSize {RequestedSize} exceeds maximum allowed size {MaxSize}. Clamping to maximum.",
                effectivePageSize,
                _options.MaxPageSize);

            effectivePageSize = _options.MaxPageSize;
        }

        _logger.LogDebug(
            "Fetching paged rules. PageNumber: {PageNumber}, PageSize: {PageSize}, IsAllowFilter: {IsAllow}",
            parameters.PageNumber,
            effectivePageSize,
            parameters.IsAllow);

        PagedResult<FilterRule> domainPagedResult = _ruleStore.GetPagedRules(
            parameters.PageNumber,
            effectivePageSize,
            parameters.ListId,
            parameters.IsAllow);

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
