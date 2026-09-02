// File: src/Astrolabed.Dns/Filtering/DomainMatchEngine.cs
using System.Diagnostics.CodeAnalysis;

namespace Astrolabed.Dns.Filtering;

/// <summary>
/// Evaluates DNS query domains against active rule snapshot collections.
/// </summary>
/// <param name="ruleStore">The filter rule storage instance.</param>
public sealed class DomainMatchEngine(IFilterRuleStore ruleStore) : IDomainMatchEngine
{
    /// <summary>
    /// The rule store providing active rule snapshots.
    /// </summary>
    private readonly IFilterRuleStore _ruleStore = ruleStore ?? throw new ArgumentNullException(nameof(ruleStore));

    /// <inheritdoc />
    public bool TryMatch(string domain, [NotNullWhen(true)] out FilterRule? matchedRule)
    {
        matchedRule = null;
        if (string.IsNullOrWhiteSpace(domain))
        {
            return false;
        }

        string cleanDomain = domain.Trim().TrimEnd('.').ToLowerInvariant();
        CompiledRuleSnapshot snapshot = _ruleStore.GetSnapshot();

        // 1. Evaluate Allow Collection Priority
        if (TryMatchCollection(cleanDomain, snapshot.AllowDomains, snapshot.AllowRegexes, out matchedRule))
        {
            return true;
        }

        // 2. Evaluate Block Collection
        return TryMatchCollection(cleanDomain, snapshot.BlockDomains, snapshot.BlockRegexes, out matchedRule);
    }

    /// <summary>
    /// Evaluates domain string against domain dictionary and regex lists.
    /// </summary>
    private static bool TryMatchCollection(
        string cleanDomain,
        IReadOnlyDictionary<string, FilterRule> domainMap,
        IReadOnlyList<FilterRule> regexRules,
        [NotNullWhen(true)] out FilterRule? matchedRule)
    {
        matchedRule = null;

        // Exact and Hierarchical Subdomain Match
        ReadOnlySpan<char> span = cleanDomain.AsSpan();
        int offset = 0;

        while (offset < span.Length)
        {
            string candidate = span[offset..].ToString();
            if (domainMap.TryGetValue(candidate, out FilterRule? rule))
            {
                if (rule.RuleKind == RuleKind.Exact && candidate.Length != cleanDomain.Length)
                {
                    // Exact rules match only the precise domain, not subdomains
                }
                else
                {
                    matchedRule = rule;
                    return true;
                }
            }

            int nextDot = span[offset..].IndexOf('.');
            if (nextDot < 0)
            {
                break;
            }

            offset += nextDot + 1;
        }

        // Precompiled Regex Match
        for (int i = 0; i < regexRules.Count; i++)
        {
            FilterRule regexRule = regexRules[i];
            if (regexRule.CompiledRegex is not null && regexRule.CompiledRegex.IsMatch(cleanDomain))
            {
                matchedRule = regexRule;
                return true;
            }
        }

        return false;
    }
}
