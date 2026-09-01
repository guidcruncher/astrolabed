// File: src/Astrolabed.Dns/Filtering/DomainFilterRuleStore.cs
using System.Collections.Frozen;
using System.Text.RegularExpressions;

using Microsoft.Extensions.Logging;

namespace Astrolabed.Dns.Filtering;

/// <summary>
/// Represents an exact domain rule paired with its associated list source ID.
/// </summary>
/// <param name="Domain">The normalized domain string to match.</param>
/// <param name="RuleListId">The identifier of the DNS list source that provided this rule.</param>
public sealed record ExactDomainRule(string Domain, int RuleListId);

/// <summary>
/// Represents a compiled regex rule paired with its associated list source ID.
/// </summary>
/// <param name="Pattern">The compiled regular expression object.</param>
/// <param name="RuleListId">The identifier of the DNS list source that provided this rule.</param>
public sealed record RegexRule(Regex Pattern, int RuleListId);

/// <summary>
/// Provides high-performance, lock-free snapshot storage for exact and regex DNS filtering rules tagged with source list identifiers.
/// </summary>
/// <param name="logger">Structured logger instance.</param>
public sealed partial class DomainFilterRuleStore(ILogger<DomainFilterRuleStore> logger) : IDomainFilterRuleStore
{
    /// <summary>
    /// The structured logger for diagnostic outputs.
    /// </summary>
    private readonly ILogger<DomainFilterRuleStore> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Thread synchronization lock for atomic snapshot update swaps.
    /// </summary>
    private readonly object _updateLock = new();

    /// <summary>
    /// Current volatile reference to the active immutable rule snapshot.
    /// </summary>
    private RuleStoreSnapshot _snapshot = new(
        FrozenDictionary<string, int>.Empty,
        Array.Empty<RegexRule>(),
        FrozenDictionary<string, int>.Empty,
        Array.Empty<RegexRule>());

    /// <inheritdoc />
    public IReadOnlyDictionary<string, int> ExactAllows => Volatile.Read(ref _snapshot).ExactAllows;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, int> ExactBlocks => Volatile.Read(ref _snapshot).ExactBlocks;

    /// <inheritdoc />
    public IReadOnlyList<RegexRule> RegexAllows => Volatile.Read(ref _snapshot).RegexAllows;

    /// <inheritdoc />
    public IReadOnlyList<RegexRule> RegexBlocks => Volatile.Read(ref _snapshot).RegexBlocks;

    /// <inheritdoc />
    public void UpdateRules(int ruleListId, IEnumerable<string> allowRules, IEnumerable<string> blockRules)
    {
        ArgumentNullException.ThrowIfNull(allowRules);
        ArgumentNullException.ThrowIfNull(blockRules);

        var (exactAllows, regexAllows) = ProcessRules(ruleListId, allowRules);
        var (exactBlocks, regexBlocks) = ProcessRules(ruleListId, blockRules);

        var newSnapshot = new RuleStoreSnapshot(
            exactAllows.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase),
            regexAllows,
            exactBlocks.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase),
            regexBlocks);

        lock (_updateLock)
        {
            Volatile.Write(ref _snapshot, newSnapshot);
        }

        LogRulesUpdated(
            _logger,
            ruleListId,
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

    /// <summary>
    /// Parses and separates raw filter rule strings into exact string matches and compiled regular expressions associated with a list source ID.
    /// </summary>
    /// <param name="ruleListId">The identifier of the rule list source.</param>
    /// <param name="rules">Raw domain filter rules.</param>
    /// <returns>A dictionary of exact matches mapped to rule list IDs and a list of compiled regex rules.</returns>
    private (Dictionary<string, int> ExactMatches, List<RegexRule> RegexMatches) ProcessRules(int ruleListId, IEnumerable<string> rules)
    {
        var exactMatches = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var rawRegexPatterns = new HashSet<string>(StringComparer.Ordinal);
        var regexMatches = new List<RegexRule>();

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
		if (!pattern.EndsWith("$")) { 
		rawRegexPatterns.Add($"{pattern}$");
		} else {
                rawRegexPatterns.Add(pattern);
		}
            }
            else
            {
                exactMatches[NormalizeDomain(rule)] = ruleListId;
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

                regexMatches.Add(new RegexRule(regex, ruleListId));
            }
            catch (ArgumentException ex)
            {
                LogInvalidRegexSkipped(_logger, ex, pattern);
            }
        }

        return (exactMatches, regexMatches);
    }

    /// <summary>
    /// Determines whether a rule string contains regular expression syntax or wildcard constructs.
    /// </summary>
    /// <param name="rule">The input rule string to test.</param>
    /// <param name="pattern">Outputs the derived regex pattern if matched.</param>
    /// <returns><c>true</c> if the rule represents a regex pattern or wildcard match; otherwise <c>false</c>.</returns>
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

    /// <summary>
    /// Normalizes domain name strings by trimming trailing dots and casing to lowercase.
    /// </summary>
    /// <param name="domain">The raw domain string.</param>
    /// <returns>A normalized lowercase domain string.</returns>
    private static string NormalizeDomain(string domain)
    {
        return domain.Trim().TrimEnd('.').ToLowerInvariant();
    }

    /// <summary>
    /// Logs successful rule store updates with breakdown counts per rule type.
    /// </summary>
    /// <param name="logger">Target structured logger instance.</param>
    /// <param name="ruleListId">The updated rule list source identifier.</param>
    /// <param name="exactAllowsCount">Count of exact allow rules.</param>
    /// <param name="regexAllowsCount">Count of regex allow rules.</param>
    /// <param name="exactBlocksCount">Count of exact block rules.</param>
    /// <param name="regexBlocksCount">Count of regex block rules.</param>
    [LoggerMessage(
        EventId = 201,
        Level = LogLevel.Information,
        Message = "Domain filter rule store updated for RuleListId {RuleListId}. Allows: {ExactAllowsCount} exact, {RegexAllowsCount} regex. Blocks: {ExactBlocksCount} exact, {RegexBlocksCount} regex.")]
    private static partial void LogRulesUpdated(
        ILogger logger,
        int ruleListId,
        int exactAllowsCount,
        int regexAllowsCount,
        int exactBlocksCount,
        int regexBlocksCount);

    /// <summary>
    /// Logs invalid regular expression pattern skipping diagnostics.
    /// </summary>
    /// <param name="logger">Target structured logger instance.</param>
    /// <param name="exception">Captured argument exception.</param>
    /// <param name="pattern">Target regex pattern string that failed compilation.</param>
    [LoggerMessage(
        EventId = 202,
        Level = LogLevel.Error,
        Message = "Invalid regex pattern skipped: {Pattern}")]
    private static partial void LogInvalidRegexSkipped(ILogger logger, Exception exception, string pattern);
}
