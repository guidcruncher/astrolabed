namespace Astrolabed.Dns.Filtering;

using Astrolabed.Dns.Options;

/// <summary>
/// Defines asynchronous loading operations for fetching, parsing, and applying domain filter lists from HTTP endpoints or local file paths.
/// </summary>
public interface IListLoader
{
    /// <summary>
    /// Asynchronously fetches and parses domain filter rules from a specified URI or local filesystem path.
    /// </summary>
    /// <param name="source">The List Source.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>A tuple containing read-only lists of parsed allow and block domain rules.</returns>
    Task<(IReadOnlyList<string> AllowRules, IReadOnlyList<string> BlockRules)> LoadRulesAsync(ListSource source, CancellationToken ct = default);

    /// <summary>
    /// Asynchronously fetches, parses, and applies domain filter rules directly to the underlying <see cref="IDomainFilterRuleStore"/>.
    /// </summary>
    /// <param name="source">The List Source.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task LoadAndApplyListAsync(ListSource source, CancellationToken ct = default);
}
