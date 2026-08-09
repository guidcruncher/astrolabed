using Astrolabed.Dns.Core;

using Microsoft.Extensions.Logging.Abstractions;

using Xunit;

namespace Astrolabed.Dns.Tests;

public sealed class HostsServfailTests
{
    [Fact]
    public void NonexistentDomain_Should_Return_SERVFAIL()
    {
        var options = new AstrolabedOptions
        {
            // UPDATED: DefaultResolvers replaces DefaultResolver
            DefaultResolvers =
            {
                new UpstreamResolverOptions
                {
                    Address = "127.0.0.1",
                    Port = 5300,
                    Rule = "*.test",
                    Name = "default"
                }
            }
        };

        var logger = NullLogger<Astrolabed.Dns.RuleEngine.RuleEngine>.Instance;
        var engine = new Astrolabed.Dns.RuleEngine.RuleEngine(options, logger);

        var result = engine.Match("nonexistent.test", "-");

        Assert.NotEmpty(result.Upstreams);
        Assert.Equal("default", result.Upstreams[0].Name);
    }
}
