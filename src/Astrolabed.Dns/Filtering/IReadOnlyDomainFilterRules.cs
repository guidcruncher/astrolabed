namespace Astrolabed.Dns.Filtering;

/// <summary>
/// Provides read-only access to compiled domain filter rules.
/// </summary>
public interface IReadOnlyDomainFilterRules
{
    /// <summary>
    /// Gets the set of exact allowed domain names.
    /// </summary>
    IReadOnlySet<string> ExactAllows { get; }

    /// <summary>
    /// Gets the set of exact blocked domain names.
    /// </summary>
    IReadOnlySet<string> ExactBlocks { get; }

    /// <summary>
    /// Gets the collection of compiled allowlist regular expression strings.
    /// </summary>
    IReadOnlyList<string> RegexAllows { get; }

    /// <summary>
    /// Gets the collection of compiled blocklist regular expression strings.
    /// </summary>
    IReadOnlyList<string> RegexBlocks { get; }
}

