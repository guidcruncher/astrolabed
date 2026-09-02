// File: tests/Astrolabed.Dns/DomainMatchEngineTests.cs
using Astrolabed.Dns.Filtering;

using Microsoft.Extensions.Logging.Abstractions;

using Xunit;

namespace Astrolabed.Dns;

public class DomainMatchEngineTests
{
    private readonly FilterRuleStore _ruleStore = new(NullLogger<FilterRuleStore>.Instance);
    private readonly DomainMatchEngine _engine;

    public DomainMatchEngineTests()
    {
        _engine = new DomainMatchEngine(_ruleStore);
    }

    [Fact]
    public void TryMatch_AllowRuleOverridesBlockRule()
    {
        // Arrange
        var blockRule = new FilterRule("example.com", RuleKind.Hierarchy, IsAllow: false, ListId: 1);
        var allowRule = new FilterRule("sub.example.com", RuleKind.Exact, IsAllow: true, ListId: 2);

        _ruleStore.UpdateListRules(1, new[] { blockRule });
        _ruleStore.UpdateListRules(2, new[] { allowRule });

        // Act
        bool matched = _engine.TryMatch("sub.example.com", out FilterRule? matchedRule);

        // Assert
        Assert.True(matched);
        Assert.NotNull(matchedRule);
        Assert.True(matchedRule.IsAllow);
        Assert.Equal(2, matchedRule.ListId);
    }

    [Fact]
    public void TryMatch_BlocksSubdomainOnHierarchyMatch()
    {
        // Arrange
        var blockRule = new FilterRule("bad-domain.com", RuleKind.Hierarchy, IsAllow: false, ListId: 1);
        _ruleStore.UpdateListRules(1, new[] { blockRule });

        // Act
        bool matched = _engine.TryMatch("sub.bad-domain.com", out FilterRule? matchedRule);

        // Assert
        Assert.True(matched);
        Assert.NotNull(matchedRule);
        Assert.False(matchedRule.IsAllow);
        Assert.Equal("bad-domain.com", matchedRule.Pattern);
    }

    [Fact]
    public void GetPagedRules_ReturnsDeduplicatedPagedResults()
    {
        // Arrange
        var rulesList1 = new[] { new FilterRule("domain1.com", RuleKind.Exact, IsAllow: false, ListId: 1) };
        var rulesList2 = new[] { new FilterRule("domain2.com", RuleKind.Exact, IsAllow: false, ListId: 2) };

        _ruleStore.UpdateListRules(1, rulesList1);
        _ruleStore.UpdateListRules(2, rulesList2);

        // Act
        var pagedResult = _ruleStore.GetPagedRules(pageNumber: 1, pageSize: 1);

        // Assert
        Assert.Equal(2, pagedResult.TotalCount);
        Assert.Equal(2, pagedResult.TotalPages);
        Assert.Single(pagedResult.Items);
        Assert.True(pagedResult.HasNextPage);
    }
}
