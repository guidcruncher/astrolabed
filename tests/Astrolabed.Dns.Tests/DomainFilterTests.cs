// File: tests/Astrolabed.Dns.Tests/DomainFilterTests.cs
using System.Collections.Frozen;

using Astrolabed.Dns.Filtering;

using Xunit;

namespace Astrolabed.Dns.Tests;

public class DomainFilterTests
{
    private readonly FakeDomainFilterRuleStore _ruleStore = new();

    [Fact]
    public void Constructor_NullRuleStore_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new DomainFilter(null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsAllowed_NullOrEmptyDomain_ReturnsFalse(string? domain)
    {
        var filter = new DomainFilter(_ruleStore);
        Assert.False(filter.IsAllowed(domain!, out _));
    }

    [Fact]
    public void IsAllowed_DomainInExactAllows_ReturnsTrue()
    {
        _ruleStore.Snapshot = new RuleStoreSnapshot(
            new Dictionary<string, int> { ["example.com"] = 1 }.ToFrozenDictionary(),
            Array.Empty<RegexRule>(),
            new Dictionary<string, int> { ["block.com"] = 1 }.ToFrozenDictionary(),
            Array.Empty<RegexRule>());

        var filter = new DomainFilter(_ruleStore);

        Assert.True(filter.IsAllowed("sub.example.com.", out int? ruleListId));
        Assert.Equal(1, ruleListId);
    }

    [Fact]
    public void IsBlocked_ExactMatch_ReturnsTrueWithReason()
    {
        _ruleStore.Snapshot = new RuleStoreSnapshot(
            FrozenDictionary<string, int>.Empty,
            Array.Empty<RegexRule>(),
            new Dictionary<string, int> { ["malware.com"] = 1 }.ToFrozenDictionary(),
            Array.Empty<RegexRule>());

        var filter = new DomainFilter(_ruleStore);

        bool isBlocked = filter.IsBlocked("sub.malware.com.", out string? reason, out int? ruleListId);

        Assert.True(isBlocked);
        Assert.Contains("malware.com", reason);
        Assert.Equal(1, ruleListId);
    }

    [Fact]
    public void IsBlocked_AllowedSupercedesBlock_ReturnsFalse()
    {
        _ruleStore.Snapshot = new RuleStoreSnapshot(
            new Dictionary<string, int> { ["sub.malware.com"] = 1 }.ToFrozenDictionary(),
            Array.Empty<RegexRule>(),
            new Dictionary<string, int> { ["malware.com"] = 1 }.ToFrozenDictionary(),
            Array.Empty<RegexRule>());

        var filter = new DomainFilter(_ruleStore);

        bool isBlocked = filter.IsBlocked("sub.malware.com", out _, out _);

        Assert.False(isBlocked);
    }

    private sealed class FakeDomainFilterRuleStore : IDomainFilterRuleStore
    {
        public RuleStoreSnapshot Snapshot { get; set; } = new(
            FrozenDictionary<string, int>.Empty,
            Array.Empty<RegexRule>(),
            FrozenDictionary<string, int>.Empty,
            Array.Empty<RegexRule>());

        public IReadOnlyDictionary<string, int> ExactAllows => Snapshot.ExactAllows;
        public IReadOnlyList<RegexRule> RegexAllows => Snapshot.RegexAllows;
        public IReadOnlyDictionary<string, int> ExactBlocks => Snapshot.ExactBlocks;
        public IReadOnlyList<RegexRule> RegexBlocks => Snapshot.RegexBlocks;

        public RuleStoreSnapshot GetCompiledSnapshot() => Snapshot;

        public void UpdateRules(int ruleListId, IEnumerable<string> allowRules, IEnumerable<string> blockRules)
        {
        }
    }
}
