using System.Collections.Generic;
using System.Net.Http;

using Astrolabed.Dns.Core;
using Astrolabed.Dns.RuleEngine;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Xunit;

namespace Astrolabed.Dns.Tests;

public sealed class SequentialFallbackTests
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
                    Name = "primary",
                    Address = "127.0.0.1",
                    Port = 5301
                },
                new UpstreamResolverOptions
                {
                    Name = "secondary",
                    Address = "127.0.0.1",
                    Port = 5302
                },
                new UpstreamResolverOptions
                {
                    Name = "tertiary",
                    Address = "127.0.0.1",
                    Port = 5303
                }
            }
        };

        var logger = NullLogger<Astrolabed.Dns.RuleEngine.RuleEngine>.Instance;
        var clientFactory = new DefaultDnsClientFactory(new HttpClientFactoryStub());
        var cacheOptions = Options.Create(new CachingOptions { MaxEntries = 50 });
        var cacheLogger = NullLogger<DnsCache>.Instance;
        var cache = new DnsCache(cacheOptions, cacheLogger);
        return new Astrolabed.Dns.RuleEngine.RuleEngine(Options.Create(options), logger, clientFactory, cache);
    }

    private static byte[] BuildQuery(string domain)
    {
        var parts = domain.Split('.');
        var q = new List<byte> { 0x12, 0x34, 0x01, 0x00, 0x00, 0x01, 0, 0, 0, 0, 0, 0 };

        foreach (var p in parts)
        {
            q.Add((byte)p.Length);
            q.AddRange(System.Text.Encoding.ASCII.GetBytes(p));
        }

        q.Add(0);
        q.AddRange(new byte[] { 0x00, 0x01, 0x00, 0x01 });
        return q.ToArray();
    }

    [Fact]
    public void Match_Should_Return_default_DefaultResolver()
    {
        var engine = CreateEngine();

        var result = engine.Match("anything.test", "-");

        Assert.Single(result.Upstreams);
        Assert.Equal("default", result.Upstreams[0].Name);
    }
}
