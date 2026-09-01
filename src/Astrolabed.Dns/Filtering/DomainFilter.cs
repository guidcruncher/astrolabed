namespace Astrolabed.Dns.Filtering;

using Microsoft.Extensions.Logging;

/// <summary>
/// Evaluates DNS query domains against exact, hierarchical subdomain, and regex rules.
/// </summary>
/// <param name="ruleStore">Rule store providing compiled filter rule snapshots.</param>
public sealed class DomainFilter(IDomainFilterRuleStore ruleStore) : IDomainFilter
{
    /// <summary>
    /// The rule store providing active compiled filtering snapshots.
    /// </summary>
    private readonly IDomainFilterRuleStore _ruleStore = ruleStore ?? throw new ArgumentNullException(nameof(ruleStore));

    /// <inheritdoc />
    public bool IsAllowed(string domain, out int? ruleListId)
    {
        ruleListId = null;
        if (string.IsNullOrWhiteSpace(domain))
        {
            return false;
        }

        string cleanDomain = NormalizeDomain(domain);
        RuleStoreSnapshot snapshot = _ruleStore.GetCompiledSnapshot();

        return IsDomainAllowedInternal(cleanDomain, snapshot, out ruleListId);
    }

    /// <inheritdoc />
    public bool IsBlocked(string domain, out string? reason, out int? ruleListId)
    {
        reason = null;
        ruleListId = null;

        if (string.IsNullOrWhiteSpace(domain))
        {
            return false;
        }

        string cleanDomain = NormalizeDomain(domain);
        RuleStoreSnapshot snapshot = _ruleStore.GetCompiledSnapshot();

        // Priority 1: Check Allow Rules (Exact, Subdomain, or Regex)
        if (IsDomainAllowedInternal(cleanDomain, snapshot, out int? allowRuleListId))
        {
            return false;
        }

        // Priority 2: Check Exact / Hierarchical Subdomain Block Rules
        ReadOnlySpan<char> span = cleanDomain.AsSpan();
        int offset = 0;

        while (offset < span.Length)
        {
            string candidate = span[offset..].ToString();
            if (snapshot.ExactBlocks.TryGetValue(candidate, out int matchedRuleListId))
            {
                ruleListId = matchedRuleListId;
                reason = $"Matched exact blocklist entry: {candidate}";
                return true;
            }

            int nextDot = span[offset..].IndexOf('.');
            if (nextDot < 0)
            {
                break;
            }

            offset += nextDot + 1;
        }

        // Priority 3: Check Regex Block Rules
        IReadOnlyList<RegexRule> regexBlocks = snapshot.RegexBlocks;

        for (int i = 0; i < regexBlocks.Count; i++)
        {
            RegexRule regexRule = regexBlocks[i];

            if (regexRule.Pattern.IsMatch(cleanDomain))
            {
                ruleListId = regexRule.RuleListId;
                reason = $"Matched blocklist regex pattern: {regexRule.Pattern}";
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Evaluates internal exact, hierarchical subdomain, and regex allow matching logic against snapshot dictionaries.
    /// </summary>
    /// <param name="cleanDomain">The normalized domain string.</param>
    /// <param name="snapshot">The active compiled rule snapshot.</param>
    /// <param name="ruleListId">Outputs the matched rule list ID if allowed.</param>
    /// <returns><c>true</c> if allowed; otherwise <c>false</c>.</returns>
    private static bool IsDomainAllowedInternal(string cleanDomain, RuleStoreSnapshot snapshot, out int? ruleListId)
    {
        ruleListId = null;

        // 1. Hierarchical Subdomain Allow Match
        ReadOnlySpan<char> span = cleanDomain.AsSpan();
        int offset = 0;

        while (offset < span.Length)
        {
            string candidate = span[offset..].ToString();
            if (snapshot.ExactAllows.TryGetValue(candidate, out int matchedRuleListId))
            {
                ruleListId = matchedRuleListId;
                return true;
            }

            int nextDot = span[offset..].IndexOf('.');
            if (nextDot < 0)
            {
                break;
            }

            offset += nextDot + 1;
        }

        // 2. Pre-compiled Regex Allow Match
        IReadOnlyList<RegexRule> regexAllows = snapshot.RegexAllows;
        for (int i = 0; i < regexAllows.Count; i++)
        {
            RegexRule regexRule = regexAllows[i];
            if (regexRule.Pattern.IsMatch(cleanDomain))
            {
                ruleListId = regexRule.RuleListId;
                return true;
            }
        }

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
}
