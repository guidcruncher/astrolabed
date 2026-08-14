using System.Net.Http;

using Astrolabed.Dns.Core;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Xunit;

namespace Astrolabed.Dns.Tests;

public sealed class RuleEngineTests
{
    private class HttpClientFactoryStub : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new HttpClient();
    }

    [Fact]
    public void Match_Uses_Default_When_No_Rules()
    {
        var options = new DnsForwarderOptions
        {
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
        var clientFactory = new DefaultDnsClientFactory(new HttpClientFactoryStub());
        var cache = new Astrolabed.Dns.RuleEngine.DnsCache(50);
        var engine = new Astrolabed.Dns.RuleEngine.RuleEngine(Options.Create(options), logger, clientFactory, cache);

        var result = engine.Match("anything.test", "-");

        Assert.NotEmpty(result.Upstreams);
        Assert.Equal("default", result.Upstreams[0].Name);
    }
}
