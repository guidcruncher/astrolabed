using System.Text.RegularExpressions;

namespace Astrolabed.Dns.Filtering;

/// <summary>
/// Evaluates DNS query domains against exact, hierarchical subdomain, and regex rules.
/// </summary>
/// <param name="ruleStore">Rule store providing compiled filter rule snapshots.</param>
public sealed class DomainFilter(IDomainFilterRuleStore ruleStore) : IDomainFilter
{
    private readonly IDomainFilterRuleStore _ruleStore = ruleStore ?? throw new ArgumentNullException(nameof(ruleStore));

    /// <inheritdoc />
    public bool IsAllowed(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
        {
            return false;
        }

        string cleanDomain = NormalizeDomain(domain);
        RuleStoreSnapshot snapshot = _ruleStore.GetCompiledSnapshot();

        return IsDomainAllowedInternal(cleanDomain, snapshot);
    }

    /// <inheritdoc />
    public bool IsBlocked(string domain, out string? reason)
    {
        reason = null;
        if (string.IsNullOrWhiteSpace(domain))
        {
            return false;
        }

        string cleanDomain = NormalizeDomain(domain);
        RuleStoreSnapshot snapshot = _ruleStore.GetCompiledSnapshot();

        // Priority 1: Check Allow Rules (Exact, Subdomain, or Regex)
        if (IsDomainAllowedInternal(cleanDomain, snapshot))
        {
            return false;
        }

        // Priority 2: Check Exact / Hierarchical Subdomain Block Rules
        ReadOnlySpan<char> span = cleanDomain.AsSpan();
        int offset = 0;

        while (offset < span.Length)
        {
            string candidate = span[offset..].ToString();
            if (snapshot.ExactBlocks.Contains(candidate))
            {
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
        IReadOnlyList<Regex> regexBlocks = snapshot.RegexBlocks;
        for (int i = 0; i < regexBlocks.Count; i++)
        {
            Regex regex = regexBlocks[i];
            if (regex.IsMatch(cleanDomain))
            {
                reason = $"Matched blocklist regex pattern: {regex}";
                return true;
            }
        }

        return false;
    }

    private static bool IsDomainAllowedInternal(string cleanDomain, RuleStoreSnapshot snapshot)
    {
        // 1. Hierarchical Subdomain Allow Match
        ReadOnlySpan<char> span = cleanDomain.AsSpan();
        int offset = 0;

        while (offset < span.Length)
        {
            string candidate = span[offset..].ToString();
            if (snapshot.ExactAllows.Contains(candidate))
            {
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
        IReadOnlyList<Regex> regexAllows = snapshot.RegexAllows;
        for (int i = 0; i < regexAllows.Count; i++)
        {
            if (regexAllows[i].IsMatch(cleanDomain))
            {
                return true;
            }
        }

        return false;
    }

    private static string NormalizeDomain(string domain)
    {
        return domain.Trim().TrimEnd('.').ToLowerInvariant();
    }
}

