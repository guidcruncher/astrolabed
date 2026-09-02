// File: src/Astrolabed.Dns/Filtering/IFilterRuleStore.cs
using Astrolabed.Data.Pagination;

namespace Astrolabed.Dns.Filtering;

/// <summary>
/// Defines rule storage operations for compiling, storing, deduplicating, and querying filter rules.
/// </summary>
public interface IFilterRuleStore
{
    /// <summary>
    /// Replaces or registers filter rules for a specific list ID.
    /// </summary>
    /// <param name="listId">The list source identifier.</param>
    /// <param name="rules">The rules to store.</param>
    void UpdateListRules(int listId, IEnumerable<FilterRule> rules);

    /// <summary>
    /// Retrieves a snapshot container of active deduplicated rules.
    /// </summary>
    /// <returns>A compiled rule snapshot container.</returns>
    CompiledRuleSnapshot GetSnapshot();

    /// <summary>
    /// Returns a deduplicated paged list of active stored rules.
    /// </summary>
    /// <param name="pageNumber">The 1-based page number.</param>
    /// <param name="pageSize">The requested number of entries per page.</param>
    /// <param name="isAllow">Optional filter to scope rules to allowlist or blocklist only.</param>
    /// <returns>A paged result container of filter rules.</returns>
    PagedResult<FilterRule> GetPagedRules(int pageNumber, int pageSize, bool? isAllow = null);
}
