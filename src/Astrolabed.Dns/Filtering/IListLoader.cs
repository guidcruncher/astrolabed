namespace Astrolabed.Dns.Filtering;

/// <summary>
/// Defines asynchronous loading operations for fetching, parsing, and applying domain filter lists from HTTP endpoints or local file paths.
/// </summary>
public interface IListLoader
{
    /// <summary>
    /// Asynchronously fetches and parses domain filter rules from a specified URI or local filesystem path.
    /// </summary>
    /// <param name="uriOrPath">The HTTP/HTTPS URL or local file path (including file:// URIs) pointing to the list.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>A tuple containing read-only lists of parsed allow and block domain rules.</returns>
    Task<(IReadOnlyList<string> AllowRules, IReadOnlyList<string> BlockRules)> LoadRulesAsync(string uriOrPath, CancellationToken ct = default);

    /// <summary>
    /// Asynchronously fetches, parses, and applies domain filter rules directly to the underlying <see cref="IDomainFilterRuleStore"/>.
    /// </summary>
    /// <param name="uriOrPath">The HTTP/HTTPS URL or local file path (including file:// URIs) pointing to the list.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task LoadAndApplyListAsync(string uriOrPath, CancellationToken ct = default);
}
