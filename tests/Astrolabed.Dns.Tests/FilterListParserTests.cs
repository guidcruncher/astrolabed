// File: tests/Astrolabed.Dns/FilterListParserTests.cs
using System.Net;

using Astrolabed.Dns.Filtering;

using Microsoft.Extensions.Logging.Abstractions;

using Xunit;

namespace Astrolabed.Dns;

public class FilterListParserTests
{
    private readonly FilterListParser _parser = new(NullLogger<FilterListParser>.Instance);

    [Fact]
    public async Task ParseAsync_ParsesAdGuardHierarchyAndAllowRules()
    {
        // Arrange
        string content = """
            ! Comment line
            ||ads.example.com^
            @@||allowed.example.com^
            """;

        using var reader = new StringReader(content);

        // Act
        IReadOnlyList<FilterRule> rules = await _parser.ParseAsync(reader, listId: 1);

        // Assert
        Assert.Equal(2, rules.Count);
        Assert.Contains(rules, r => !r.IsAllow && r.Pattern == "ads.example.com" && r.RuleKind == RuleKind.Hierarchy);
        Assert.Contains(rules, r => r.IsAllow && r.Pattern == "allowed.example.com" && r.RuleKind == RuleKind.Hierarchy);
    }

    [Fact]
    public async Task ParseAsync_ParsesHostsFileFormatWithIpAddress()
    {
        // Arrange
        string content = """
            127.0.0.1 malicious-site.com
            0.0.0.0 telemetry.tracker.net
            """;

        using var reader = new StringReader(content);

        // Act
        IReadOnlyList<FilterRule> rules = await _parser.ParseAsync(reader, listId: 2);

        // Assert
        Assert.Equal(2, rules.Count);
        Assert.Equal(IPAddress.Parse("127.0.0.1"), rules[0].IpAddress);
        Assert.Equal("malicious-site.com", rules[0].Pattern);
        Assert.Equal(IPAddress.Parse("0.0.0.0"), rules[1].IpAddress);
        Assert.Equal("telemetry.tracker.net", rules[1].Pattern);
    }
}
