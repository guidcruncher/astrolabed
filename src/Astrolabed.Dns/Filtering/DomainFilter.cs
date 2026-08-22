// File: src/Astrolabed.Dns/Filtering/DomainFilter.cs
namespace Astrolabed.Dns.Filtering;

public sealed class DomainFilter : IDomainFilter
{
    private readonly IDomainFilterRuleStore _ruleStore;

    public DomainFilter(IDomainFilterRuleStore ruleStore)
    {
        _ruleStore = ruleStore;
    }

    public bool IsAllowed(string domain)
    {
        var cleanDomain = NormalizeDomain(domain);
        var (exactAllows, regexAllows, _, _) = _ruleStore.GetCompiledSnapshot();

        // 1. Fast O(1) exact allow check
        if (exactAllows.Contains(cleanDomain))
        {
            return true;
        }

        // 2. Pre-compiled Regex allow matching
        for (int i = 0; i < regexAllows.Count; i++)
        {
            if (regexAllows[i].IsMatch(cleanDomain))
            {
                return true;
            }
        }

        return false;
    }

    public bool IsBlocked(string domain, out string? reason)
    {
        var cleanDomain = NormalizeDomain(domain);
        reason = null;

        var (exactAllows, regexAllows, exactBlocks, regexBlocks) = _ruleStore.GetCompiledSnapshot();

        // 1. Priority Override: Allow rules ALWAYS override block rules
        if (exactAllows.Contains(cleanDomain))
        {
            return false;
        }

        for (int i = 0; i < regexAllows.Count; i++)
        {
            if (regexAllows[i].IsMatch(cleanDomain))
            {
                return false;
            }
        }

        // 2. Evaluate Block Rules (Exact match first, then Regex)
        if (exactBlocks.Contains(cleanDomain))
        {
            reason = $"Matched exact blocklist entry: {cleanDomain}";
            return true;
        }

        for (int i = 0; i < regexBlocks.Count; i++)
        {
            var regex = regexBlocks[i];
            if (regex.IsMatch(cleanDomain))
            {
                reason = $"Matched blocklist regex pattern: {regex}";
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
