// File: src/Astrolabed.Dns/Filtering/IFilterListParser.cs
namespace Astrolabed.Dns.Filtering;

/// <summary>
/// Defines parsing operations for reading domain filter rules in AdGuard or Hosts file format.
/// </summary>
public interface IFilterListParser
{
    /// <summary>
    /// Asynchronously parses domain filter rules from a stream reader.
    /// </summary>
    /// <param name="reader">The text reader containing the raw list data.</param>
    /// <param name="listId">The target list source identifier.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A collection of parsed and deduplicated filter rules.</returns>
    Task<IReadOnlyList<FilterRule>> ParseAsync(TextReader reader, int listId, CancellationToken cancellationToken = default);
}
