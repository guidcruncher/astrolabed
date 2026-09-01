namespace Astrolabed.Dns.Filtering;

/// <summary>
/// Defines rule storage operations for compiling, storing, and snapshot-retrieving domain filter lists.
/// </summary>
public interface IDomainFilterRuleStore : IReadOnlyDomainFilterRules
{
    /// <summary>
    /// Replaces or updates filtering rules associated with a specific list source ID with updated allowlist and blocklist entries.
    /// </summary>
    /// <param name="ruleListId">The identifier of the DNS list source associated with the provided rules.</param>
    /// <param name="allowRules">Raw allowlist rule entries.</param>
    /// <param name="blockRules">Raw blocklist rule entries.</param>
    void UpdateRules(int ruleListId, IEnumerable<string> allowRules, IEnumerable<string> blockRules);

    /// <summary>
    /// Retrieves a thread-safe, immutable snapshot of the compiled rules for high-throughput evaluation.
    /// </summary>
    /// <returns>A <see cref="RuleStoreSnapshot"/> containing frozen exact sets and compiled regular expressions.</returns>
    RuleStoreSnapshot GetCompiledSnapshot();
}
