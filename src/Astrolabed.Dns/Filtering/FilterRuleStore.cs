// File: src/Astrolabed.Dns/Filtering/FilterRuleStore.cs
using System.Collections.Frozen;

using Astrolabed.Data.Pagination;

using Microsoft.Extensions.Logging;

namespace Astrolabed.Dns.Filtering;

/// <summary>
/// Provides high-performance, thread-safe deduplicated storage for domain filter rules.
/// </summary>
/// <param name="logger">Structured logger instance.</param>
public sealed partial class FilterRuleStore(ILogger<FilterRuleStore> logger) : IFilterRuleStore
{
    /// <summary>
    /// Structured logger instance.
    /// </summary>
    private readonly ILogger<FilterRuleStore> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Synchronization object for state updates.
    /// </summary>
    private readonly object _lock = new();

    /// <summary>
    /// Internal rule storage keyed by list source ID.
    /// </summary>
    private readonly Dictionary<int, List<FilterRule>> _rulesByListId = new();

    /// <summary>
    /// Active volatile reference to compiled snapshot.
    /// </summary>
    private CompiledRuleSnapshot _snapshot = new(
        FrozenDictionary<string, FilterRule>.Empty,
        Array.Empty<FilterRule>(),
        FrozenDictionary<string, FilterRule>.Empty,
        Array.Empty<FilterRule>());

    /// <inheritdoc />
    public void UpdateListRules(int listId, IEnumerable<FilterRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);

        lock (_lock)
        {
            _rulesByListId[listId] = rules.ToList();

            var allowDomains = new Dictionary<string, FilterRule>(StringComparer.OrdinalIgnoreCase);
            var blockDomains = new Dictionary<string, FilterRule>(StringComparer.OrdinalIgnoreCase);
            var allowRegexes = new List<FilterRule>();
            var blockRegexes = new List<FilterRule>();

            foreach (FilterRule rule in _rulesByListId.Values.SelectMany(r => r))
            {
                if (rule.IsAllow)
                {
                    if (rule.RuleKind == RuleKind.Regex)
                    {
                        allowRegexes.Add(rule);
                    }
                    else
                    {
                        allowDomains[rule.Pattern] = rule;
                    }
                }
                else
                {
                    if (rule.RuleKind == RuleKind.Regex)
                    {
                        blockRegexes.Add(rule);
                    }
                    else
                    {
                        blockDomains[rule.Pattern] = rule;
                    }
                }
            }

            _snapshot = new CompiledRuleSnapshot(
                allowDomains.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase),
                allowRegexes,
                blockDomains.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase),
                blockRegexes);
        }

        LogSnapshotUpdated(_logger, listId);
    }

    /// <inheritdoc />
    public CompiledRuleSnapshot GetSnapshot()
    {
        return Volatile.Read(ref _snapshot);
    }

    /// <inheritdoc />
    public PagedResult<FilterRule> GetPagedRules(int pageNumber, int pageSize, bool? isAllow = null)
    {
        List<FilterRule> allRules;

        lock (_lock)
        {
            allRules = _rulesByListId.Values
                .SelectMany(r => r)
                .Where(r => !isAllow.HasValue || r.IsAllow == isAllow.Value)
                .DistinctBy(r => $"{r.IsAllow}:{r.Pattern}")
                .ToList();
        }

        int totalCount = allRules.Count;
        int skip = (pageNumber - 1) * pageSize;

        List<FilterRule> pagedItems = allRules
            .Skip(skip)
            .Take(pageSize)
            .ToList();

        return PagedResult<FilterRule>.Create(pagedItems, totalCount, pageNumber, pageSize);
    }

    [LoggerMessage(EventId = 401, Level = LogLevel.Information, Message = "Filter rule store updated snapshot for ListId {ListId}.")]
    private static partial void LogSnapshotUpdated(ILogger logger, int listId);
}
