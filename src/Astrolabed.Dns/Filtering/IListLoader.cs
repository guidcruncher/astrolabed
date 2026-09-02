// File: src/Astrolabed.Dns/Filtering/IListLoader.cs
using Astrolabed.Dns.Options;

namespace Astrolabed.Dns.Filtering;

/// <summary>
/// Defines streaming fetching and parsing operations for remote and local DNS lists.
/// </summary>
public interface IListLoader
{
    /// <summary>
    /// Asynchronously fetches, parses, and updates rules in the filter rule store.
    /// </summary>
    /// <param name="source">The list source specification.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task LoadAndApplyListAsync(ListSource source, CancellationToken cancellationToken = default);
}
