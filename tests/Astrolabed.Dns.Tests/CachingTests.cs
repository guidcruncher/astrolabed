using System;
using System.Net;

using Astrolabed.Dns.Core;
using Astrolabed.Dns.RuleEngine;

using Xunit;

namespace Astrolabed.Dns.Tests;

public class CachingTests
{
    [Fact]
    public void Cache_Returns_Cached_Response()
    {
        byte[] response =
        {
            0x12, 0x34, 0x81, 0x80,
            0x00, 0x01, 0x00, 0x01,
            0x00, 0x00, 0x00, 0x00,

            0x07, (byte)'e',(byte)'x',(byte)'a',(byte)'m',(byte)'p',(byte)'l',(byte)'e',
            0x03, (byte)'c',(byte)'o',(byte)'m',
            0x00,
            0x00, 0x01,
            0x00, 0x01,

            0xC0, 0x0C,
            0x00, 0x01,
            0x00, 0x01,
            0x00, 0x00, 0x00, 0x3C,
            0x00, 0x04,
            0x7F, 0x00, 0x00, 0x01
        };

        byte[] query =
        {
            0x12, 0x34, 0x01, 0x00,
            0x00, 0x01, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,

            0x07, (byte)'e',(byte)'x',(byte)'a',(byte)'m',(byte)'p',(byte)'l',(byte)'e',
            0x03, (byte)'c',(byte)'o',(byte)'m',
            0x00,
            0x00, 0x01,
            0x00, 0x01
        };

        var cache = new DnsCache(100);
        var context = new DnsRequestContext(query, "req-123");

        cache.Store(context, response, TimeSpan.FromMinutes(1));

        bool hit = cache.TryGet(context, out var cachedResponse);

        Assert.True(hit);
        Assert.NotNull(cachedResponse);
        Assert.Equal(response.Length, cachedResponse.Length);
    }
}
