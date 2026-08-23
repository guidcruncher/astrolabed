using System.Collections.Frozen;
using System.Text.RegularExpressions;

using Microsoft.Extensions.Logging;

namespace Astrolabed.Dns.Filtering;

/// <summary>
/// Provides high-performance, lock-free snapshot storage for exact and regex DNS filtering rules.
/// </summary>
/// <param name="logger">Structured logger instance.</param>
public sealed partial class DomainFilterRuleStore(ILogger<DomainFilterRuleStore> logger) : IDomainFilterRuleStore
{
    private readonly ILogger<DomainFilterRuleStore> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly object _updateLock = new();

    private RuleStoreSnapshot _snapshot = new(
        FrozenSet<string>.Empty,
        Array.Empty<Regex>(),
        FrozenSet<string>.Empty,
        Array.Empty<Regex>());

    /// <inheritdoc />
    public IReadOnlySet<string> ExactAllows => Volatile.Read(ref _snapshot).ExactAllows;

    /// <inheritdoc />
    public IReadOnlySet<string> ExactBlocks => Volatile.Read(ref _snapshot).ExactBlocks;

    /// <inheritdoc />
    public IReadOnlyList<string> RegexAllows => Volatile.Read(ref _snapshot).RegexAllowsSelect;

    /// <inheritdoc />
    public IReadOnlyList<string> RegexBlocks => Volatile.Read(ref _snapshot).RegexBlocksSelect;

    /// <inheritdoc />
    public void UpdateRules(IEnumerable<string> allowRules, IEnumerable<string> blockRules)
    {
        ArgumentNullException.ThrowIfNull(allowRules);
        ArgumentNullException.ThrowIfNull(blockRules);

        var (exactAllows, regexAllows) = ProcessRules(allowRules);
        var (exactBlocks, regexBlocks) = ProcessRules(blockRules);

        var newSnapshot = new RuleStoreSnapshot(
            exactAllows.ToFrozenSet(StringComparer.OrdinalIgnoreCase),
            regexAllows,
            exactBlocks.ToFrozenSet(StringComparer.OrdinalIgnoreCase),
            regexBlocks);

        lock (_updateLock)
        {
            Volatile.Write(ref _snapshot, newSnapshot);
        }

        LogRulesUpdated(
            _logger,
            newSnapshot.ExactAllows.Count,
            newSnapshot.RegexAllows.Count,
            newSnapshot.ExactBlocks.Count,
            newSnapshot.RegexBlocks.Count);
    }

    /// <inheritdoc />
    public RuleStoreSnapshot GetCompiledSnapshot()
    {
        return Volatile.Read(ref _snapshot);
    }

    private (HashSet<string> ExactMatches, List<Regex> RegexMatches) ProcessRules(IEnumerable<string> rules)
    {
        var exactMatches = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var rawRegexPatterns = new HashSet<string>(StringComparer.Ordinal);
        var regexMatches = new List<Regex>();

        foreach (string rawRule in rules)
        {
            if (string.IsNullOrWhiteSpace(rawRule))
            {
                continue;
            }

            string rule = rawRule.Trim();
            if (rule.StartsWith('#'))
            {
                continue;
            }

            if (IsRegexOrWildcardRule(rule, out string pattern))
            {
                rawRegexPatterns.Add(pattern);
            }
            else
            {
                exactMatches.Add(NormalizeDomain(rule));
            }
        }

        foreach (string pattern in rawRegexPatterns)
        {
            try
            {
                var regex = new Regex(
                    pattern,
                    RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
                    TimeSpan.FromMilliseconds(100));

                regexMatches.Add(regex);
            }
            catch (ArgumentException ex)
            {
                LogInvalidRegexSkipped(_logger, ex, pattern);
            }
        }

        return (exactMatches, regexMatches);
    }

    private static bool IsRegexOrWildcardRule(string rule, out string pattern)
    {
        if (rule.StartsWith('/') && rule.EndsWith('/') && rule.Length > 2)
        {
            pattern = rule[1..^1];
            return true;
        }

        if (rule.Contains('*') || rule.Contains('?'))
        {
            string escaped = Regex.Escape(rule)
                .Replace(@"\*", ".*")
                .Replace(@"\?", ".");
            pattern = $"^{escaped}$";
            return true;
        }

        if (rule.Contains('^') || rule.Contains('$'))
        {
            pattern = rule;
            return true;
        }

        pattern = string.Empty;
        return false;
    }

    private static string NormalizeDomain(string domain)
    {
        return domain.Trim().TrimEnd('.').ToLowerInvariant();
    }

    [LoggerMessage(
        EventId = 201,
        Level = LogLevel.Information,
        Message = "Domain filter rule store updated. Allows: {ExactAllowsCount} exact, {RegexAllowsCount} regex. Blocks: {ExactBlocksCount} exact, {RegexBlocksCount} regex.")]
    private static partial void LogRulesUpdated(
        ILogger logger,
        int exactAllowsCount,
        int regexAllowsCount,
        int exactBlocksCount,
        int regexBlocksCount);

    [LoggerMessage(
        EventId = 202,
        Level = LogLevel.Error,
        Message = "Invalid regex pattern skipped: {Pattern}")]
    private static partial void LogInvalidRegexSkipped(ILogger logger, Exception exception, string pattern);
}

/// <summary>
/// Immutable snapshot container for domain filtering rules.
/// </summary>
public sealed record RuleStoreSnapshot(
    FrozenSet<string> ExactAllows,
    IReadOnlyList<Regex> RegexAllows,
    FrozenSet<string> ExactBlocks,
    IReadOnlyList<Regex> RegexBlocks)
{
    public IReadOnlyList<string> RegexAllowsSelect { get; } = RegexAllows.Select(r => r.ToString()).ToList();
    public IReadOnlyList<string> RegexBlocksSelect { get; } = RegexBlocks.Select(r => r.ToString()).ToList();
}
