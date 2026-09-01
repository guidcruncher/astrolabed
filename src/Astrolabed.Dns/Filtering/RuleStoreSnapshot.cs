// File: src/Astrolabed.Dns/Filtering/RuleStoreSnapshot.cs
using System.Collections.Frozen;

namespace Astrolabed.Dns.Filtering;

/// <summary>
/// Immutable snapshot container for domain filtering rules tagged with list source IDs.
/// </summary>
/// <param name="ExactAllows">Frozen dictionary mapping exact allow domains to their rule list source IDs.</param>
/// <param name="RegexAllows">Compiled list of regular expression allow rules with list source IDs.</param>
/// <param name="ExactBlocks">Frozen dictionary mapping exact block domains to their rule list source IDs.</param>
/// <param name="RegexBlocks">Compiled list of regular expression block rules with list source IDs.</param>
public sealed record RuleStoreSnapshot(
    FrozenDictionary<string, int> ExactAllows,
    IReadOnlyList<RegexRule> RegexAllows,
    FrozenDictionary<string, int> ExactBlocks,
    IReadOnlyList<RegexRule> RegexBlocks)
{
    /// <summary>
    /// Gets a string representation list of all compiled regex allow patterns.
    /// </summary>
    public IReadOnlyList<string> RegexAllowsSelect { get; } = RegexAllows.Select(r => r.Pattern.ToString()).ToList();

    /// <summary>
    /// Gets a string representation list of all compiled regex block patterns.
    /// </summary>
    public IReadOnlyList<string> RegexBlocksSelect { get; } = RegexBlocks.Select(r => r.Pattern.ToString()).ToList();
}
