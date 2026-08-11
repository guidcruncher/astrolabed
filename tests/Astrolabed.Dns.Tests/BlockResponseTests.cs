using System.Text.RegularExpressions;

using Astrolabed.Dns;
using Astrolabed.Dns.Core;
using Astrolabed.Dns.Filtering;
using Astrolabed.Dns.RuleEngine;

using Microsoft.Extensions.Logging.Abstractions;

using Xunit;

namespace Astrolabed.Dns.Tests;

public class BlockResponseTests
{
    private Astrolabed.Dns.RuleEngine.RuleEngine CreateEngine(string mode, string ip = "0.0.0.0")
    {
        var opts = new DnsForwarderOptions
        {
            BlockResponse = new BlockResponseOptions
            {
                Mode = mode,
                StaticIp = ip,
                Ttl = 60
            },

            // UPDATED: DefaultResolvers replaces DefaultResolver
            DefaultResolvers =
            {
                new UpstreamResolverOptions
                {
                    Name = "default",
                    Address = "1.1.1.1",
                    Port = 53
                }
            }
        };

        var logger = NullLogger<Astrolabed.Dns.RuleEngine.RuleEngine>.Instance;
        var engine = new Astrolabed.Dns.RuleEngine.RuleEngine(opts, logger);

        engine.AddRules(new[]
        {
            new ParsedRule
            {
                Source = "inline",
                Raw = "adsdomain.com",
                Pattern = new Regex("^adsdomain\\.com$", RegexOptions.IgnoreCase)
            }
        }, block: true);

        return engine;
    }

    [Fact]
    public async Task Block_NXDOMAIN()
    {
        using var engine = CreateEngine("NXDOMAIN");
        var context = new DnsRequestContext(BuildQuery("adsdomain.com"), "id");
        var match = engine.Match(context.Domain, context.RequestId);
        var resp = await engine.QueryAsync(context, match, CancellationToken.None);

        Assert.Equal(3, resp[3] & 0x0F);
    }

    [Fact]
    public async Task Block_SERVFAIL()
    {
        using var engine = CreateEngine("SERVFAIL");
        var context = new DnsRequestContext(BuildQuery("adsdomain.com"), "id");
        var match = engine.Match(context.Domain, context.RequestId);
        var resp = await engine.QueryAsync(context, match, CancellationToken.None);

        Assert.Equal(2, resp[3] & 0x0F);
    }

    [Fact]
    public async Task Block_REFUSED()
    {
        using var engine = CreateEngine("REFUSED");
        var context = new DnsRequestContext(BuildQuery("adsdomain.com"), "id");
        var match = engine.Match(context.Domain, context.RequestId);
        var resp = await engine.QueryAsync(context, match, CancellationToken.None);

        Assert.Equal(5, resp[3] & 0x0F);
    }

    [Fact]
    public async Task Block_STATIC_IP()
    {
        using var engine = CreateEngine("STATIC_IP", "10.0.0.5");
        var context = new DnsRequestContext(BuildQuery("adsdomain.com"), "id");
        var match = engine.Match(context.Domain, context.RequestId);
        var resp = await engine.QueryAsync(context, match, CancellationToken.None);

        Assert.Contains((byte)10, resp);
        Assert.Contains((byte)0, resp);
        Assert.Contains((byte)5, resp);
    }

    private byte[] BuildQuery(string domain)
    {
        // Minimal DNS query builder for tests
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
}
