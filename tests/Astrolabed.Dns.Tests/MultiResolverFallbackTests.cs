using System.Collections.Generic;
using System.Net.Http;

using Astrolabed.Dns.Core;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Xunit;

namespace Astrolabed.Dns.Tests;

public sealed class MultiResolverFallbackTests
{
    private class HttpClientFactoryStub : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new HttpClient();
    }

    private Astrolabed.Dns.RuleEngine.RuleEngine CreateEngine()
    {
        var options = new DnsForwarderOptions
        {
            DefaultResolvers =
            {
                new UpstreamResolverOptions
                {
                    Address = "127.0.0.1",
                    Port = 5300,
                    Rule = "*.fallback.test",
                    Name = "default"
                }
            },

            Resolvers = new List<UpstreamResolverOptions>
            {
                new()
                {
                    Address = "127.0.0.1",
                    Port = 5301,
                    Rule = "*.primary.test",
                    Name = "primary"
                },
                new()
                {
                    Address = "127.0.0.1",
                    Port = 5302,
                    Rule = "*.secondary.test",
                    Name = "secondary"
                }
            }
        };

        var logger = NullLogger<Astrolabed.Dns.RuleEngine.RuleEngine>.Instance;
        var clientFactory = new DefaultDnsClientFactory(new HttpClientFactoryStub());
	var cache = new Astrolabed.Dns.RuleEngine.DnsCache(50);
        return new Astrolabed.Dns.RuleEngine.RuleEngine(Options.Create(options), logger, clientFactory, cache);
    }

    [Fact]
    public void PrimaryResolverTimeout_Should_FallbackToSecondary()
    {
        var engine = CreateEngine();

        var domain = "api.primary.test";

        var result = engine.Match(domain, "-");

        Assert.Equal("primary", result.Upstreams[0].Name);
        Assert.Single(result.Upstreams);
    }
}
