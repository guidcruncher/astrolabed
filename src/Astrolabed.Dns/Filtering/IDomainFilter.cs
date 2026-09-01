namespace Astrolabed.Dns.Filtering;

/// <summary>
/// Defines domain filtering operations for evaluating incoming DNS query names against rules.
/// </summary>
public interface IDomainFilter
{
    /// <summary>
    /// Evaluates whether a domain name matches an allowlist rule.
    /// </summary>
    /// <param name="domain">The domain name to check.</param>
    /// <param name="ruleListId">Outputs the matched rule list source ID if allowed; otherwise <c>null</c>.</param>
    /// <returns><c>true</c> if explicitly allowed; otherwise, <c>false</c>.</returns>
    bool IsAllowed(string domain, out int? ruleListId);

    /// <summary>
    /// Evaluates whether a domain name matches an allowlist rule using a character span.
    /// </summary>
    /// <param name="domain">The domain name span to check.</param>
    /// <param name="ruleListId">Outputs the matched rule list source ID if allowed; otherwise <c>null</c>.</param>
    /// <returns><c>true</c> if explicitly allowed; otherwise, <c>false</c>.</returns>
    bool IsAllowed(ReadOnlySpan<char> domain, out int? ruleListId) => IsAllowed(domain.ToString(), out ruleListId);

    /// <summary>
    /// Evaluates whether a domain name matches a blocklist rule.
    /// </summary>
    /// <param name="domain">The domain name to check.</param>
    /// <param name="reason">Outputs the reason for blocking if matched; otherwise <c>null</c>.</param>
    /// <param name="ruleListId">Outputs the matched rule list source ID if blocked; otherwise <c>null</c>.</param>
    /// <returns><c>true</c> if blocked; otherwise, <c>false</c>.</returns>
    bool IsBlocked(string domain, out string? reason, out int? ruleListId);
}
