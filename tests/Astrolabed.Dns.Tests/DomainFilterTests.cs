using System.Collections.Frozen;
using System.Text.RegularExpressions;

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
        Assert.False(filter.IsAllowed(domain!));
    }

    [Fact]
    public void IsAllowed_DomainInExactAllows_ReturnsTrue()
    {
        _ruleStore.Snapshot = new RuleStoreSnapshot(
            new[] { "example.com" }.ToFrozenSet(),
            Array.Empty<Regex>(),
            new[] { "block.com" }.ToFrozenSet(),
            Array.Empty<Regex>());

        var filter = new DomainFilter(_ruleStore);

        Assert.True(filter.IsAllowed("sub.example.com."));
    }

    [Fact]
    public void IsBlocked_ExactMatch_ReturnsTrueWithReason()
    {
        _ruleStore.Snapshot = new RuleStoreSnapshot(
            Array.Empty<string>().ToFrozenSet(),
            Array.Empty<Regex>(),
            new[] { "malware.com" }.ToFrozenSet(),
            Array.Empty<Regex>());

        var filter = new DomainFilter(_ruleStore);

        bool isBlocked = filter.IsBlocked("sub.malware.com.", out string? reason);

        Assert.True(isBlocked);
        Assert.Contains("malware.com", reason);
    }

    [Fact]
    public void IsBlocked_AllowedSupercedesBlock_ReturnsFalse()
    {
        _ruleStore.Snapshot = new RuleStoreSnapshot(
            new[] { "sub.malware.com" }.ToFrozenSet(),
            Array.Empty<Regex>(),
            new[] { "malware.com" }.ToFrozenSet(),
            Array.Empty<Regex>());

        var filter = new DomainFilter(_ruleStore);

        bool isBlocked = filter.IsBlocked("sub.malware.com", out _);

        Assert.False(isBlocked);
    }

    private sealed class FakeDomainFilterRuleStore : IDomainFilterRuleStore
    {
        public RuleStoreSnapshot Snapshot { get; set; } = new(
            Array.Empty<string>().ToFrozenSet(),
            Array.Empty<Regex>(),
            Array.Empty<string>().ToFrozenSet(),
            Array.Empty<Regex>());

        public IReadOnlySet<string> ExactAllows => Snapshot.ExactAllows;
        public IReadOnlyList<string> RegexAllows => Snapshot.RegexAllows.Select(r => r.ToString()).ToList();
        public IReadOnlySet<string> ExactBlocks => Snapshot.ExactBlocks;
        public IReadOnlyList<string> RegexBlocks => Snapshot.RegexBlocks.Select(r => r.ToString()).ToList();

        public RuleStoreSnapshot GetCompiledSnapshot() => Snapshot;

        public void UpdateRules(IEnumerable<string> allowRules, IEnumerable<string> blockRules)
        {
        }
    }
}
