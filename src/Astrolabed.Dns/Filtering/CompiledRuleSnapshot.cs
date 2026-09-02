// File: src/Astrolabed.Dns/Filtering/CompiledRuleSnapshot.cs
using System.Collections.Frozen;

namespace Astrolabed.Dns.Filtering;

/// <summary>
/// Holds thread-safe immutable search collections for high-throughput evaluation.
/// </summary>
public sealed class CompiledRuleSnapshot
{
    /// <summary>
    /// Gets frozen lookups for exact/hierarchical allow rules.
    /// </summary>
    public FrozenDictionary<string, FilterRule> AllowDomains { get; }

    /// <summary>
    /// Gets precompiled regex allow rules.
    /// </summary>
    public IReadOnlyList<FilterRule> AllowRegexes { get; }

    /// <summary>
    /// Gets frozen lookups for exact/hierarchical block rules.
    /// </summary>
    public FrozenDictionary<string, FilterRule> BlockDomains { get; }

    /// <summary>
    /// Gets precompiled regex block rules.
    /// </summary>
    public IReadOnlyList<FilterRule> BlockRegexes { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompiledRuleSnapshot"/> class.
    /// </summary>
    public CompiledRuleSnapshot(
        FrozenDictionary<string, FilterRule> allowDomains,
        IReadOnlyList<FilterRule> allowRegexes,
        FrozenDictionary<string, FilterRule> blockDomains,
        IReadOnlyList<FilterRule> blockRegexes)
    {
        AllowDomains = allowDomains ?? throw new ArgumentNullException(nameof(allowDomains));
        AllowRegexes = allowRegexes ?? throw new ArgumentNullException(nameof(allowRegexes));
        BlockDomains = blockDomains ?? throw new ArgumentNullException(nameof(blockDomains));
        BlockRegexes = blockRegexes ?? throw new ArgumentNullException(nameof(blockRegexes));
    }
}
