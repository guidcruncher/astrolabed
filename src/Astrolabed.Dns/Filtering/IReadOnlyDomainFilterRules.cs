// File: src/Astrolabed.Dns/Filtering/IReadOnlyDomainFilterRules.cs
namespace Astrolabed.Dns.Filtering;

/// <summary>
/// Defines read-only snapshot access to compiled domain filter rules and their associated list source IDs.
/// </summary>
public interface IReadOnlyDomainFilterRules
{
    /// <summary>
    /// Gets a dictionary mapping exact allow domain names to their list source IDs.
    /// </summary>
    IReadOnlyDictionary<string, int> ExactAllows { get; }

    /// <summary>
    /// Gets a dictionary mapping exact block domain names to their list source IDs.
    /// </summary>
    IReadOnlyDictionary<string, int> ExactBlocks { get; }

    /// <summary>
    /// Gets a list of compiled regular expression allow rules and their list source IDs.
    /// </summary>
    IReadOnlyList<RegexRule> RegexAllows { get; }

    /// <summary>
    /// Gets a list of compiled regular expression block rules and their list source IDs.
    /// </summary>
    IReadOnlyList<RegexRule> RegexBlocks { get; }
}
