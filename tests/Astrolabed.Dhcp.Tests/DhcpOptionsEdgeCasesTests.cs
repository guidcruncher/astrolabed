using System;
using System.Linq;
using System.Net;

using Astrolabed.Dhcp;

using Xunit;

namespace Astrolabed.Dhcp.Tests;

public class DhcpOptionsEdgeCasesTests
{
    [Fact]
    public void BuildOffer_ContainsExpectedOptions_AndParserSkipsPad()
    {
        var discover = PacketFactory.Discover();
        var offer = DhcpPacketCodec.BuildOffer(
            discover,
            IPAddress.Parse("192.168.10.50"),
            IPAddress.Parse("192.168.10.1"),
            IPAddress.Parse("192.168.10.1"),
            IPAddress.Parse("1.1.1.1"),
            null,
	    null,
            TimeSpan.FromHours(1),
            IPAddress.Parse("255.255.255.248"));

        var list = offer.ToList();
        int endIndex = list.LastIndexOf(255);
        list.Insert(endIndex, 0);
        var modified = list.ToArray();

        var parsed = DhcpPacketCodec.Parse(modified);

        var codes = parsed.Options.Select(o => o.Code).ToArray();
        Assert.Contains((byte)53, codes);
        Assert.Contains((byte)54, codes);
        Assert.Contains((byte)3, codes);
        Assert.Contains((byte)6, codes);
    }
}
