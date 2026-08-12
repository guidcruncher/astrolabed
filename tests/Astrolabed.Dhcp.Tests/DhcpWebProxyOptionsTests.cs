using System;
using System.Linq;
using System.Net;
using System.Text;

using Xunit;

namespace Astrolabed.Dhcp.Tests;

public class DhcpWebProxyOptionTests
{
    private static DhcpPacket MakeBasePacket(bool requestWebProxy = true)
    {
        var req = new DhcpPacket
        {
            Op = 1,
            Htype = 1,
            Hlen = 6,
            Hops = 0,
            Xid = 0x12345678,
            Secs = 0,
            Flags = 0,
            Ciaddr = IPAddress.Parse("0.0.0.0"),
            Yiaddr = IPAddress.Parse("0.0.0.0"),
            Siaddr = IPAddress.Parse("0.0.0.0"),
            Giaddr = IPAddress.Parse("0.0.0.0"),
            Chaddr = new byte[16]
        };

        req.Options.Add(new DhcpOption(53, new byte[] { 1 })); // DHCPDISCOVER

        if (requestWebProxy)
        {
            // Option 55: Subnet Mask (1), Router (3), DNS (6), Domain Name (15), WPAD/WebProxy (252)
            req.Options.Add(new DhcpOption(55, new byte[] { 1, 3, 6, 15, 252 }));
        }

        return req;
    }

    [Fact]
    public void Offer_Includes_WebProxy_When_Configured()
    {
        var req = MakeBasePacket(requestWebProxy: true);
        var serverId = IPAddress.Parse("192.168.1.1");
        var router = IPAddress.Parse("192.168.1.1");
        var dns = IPAddress.Parse("192.168.1.53");
        var webproxy = "http://192.168.1.123";
        var subnetMask = IPAddress.Parse("255.255.255.0");

        var offerBytes = DhcpPacketCodec.BuildOffer(
            req,
            IPAddress.Parse("192.168.1.50"),
            serverId,
            router,
            dns,
            subnetMask,
            TimeSpan.FromHours(1),
            domainName: null,
            webproxy: webproxy);

        var parsed = DhcpPacketCodec.Parse(offerBytes);

        var proxyOpt = parsed.Options.FirstOrDefault(o => o.Code == 252);
        Assert.NotNull(proxyOpt);
        Assert.Equal(Encoding.UTF8.GetBytes(webproxy), proxyOpt.Data);
    }

    [Fact]
    public void Offer_Does_Not_Include_WebProxy_When_Not_Configured()
    {
        var req = MakeBasePacket(requestWebProxy: true);
        var serverId = IPAddress.Parse("192.168.1.1");
        var router = IPAddress.Parse("192.168.1.1");
        var dns = IPAddress.Parse("192.168.1.53");
        var subnetMask = IPAddress.Parse("255.255.255.0");

        var offerBytes = DhcpPacketCodec.BuildOffer(
            req,
            IPAddress.Parse("192.168.1.50"),
            serverId,
            router,
            dns,
            subnetMask,
            TimeSpan.FromHours(1),
            domainName: null,
            webproxy: null);

        var parsed = DhcpPacketCodec.Parse(offerBytes);

        Assert.DoesNotContain(parsed.Options, o => o.Code == 252);
    }

    [Fact]
    public void Ack_Includes_WebProxy_When_Configured()
    {
        var req = MakeBasePacket(requestWebProxy: true);
        req.Options[0] = new DhcpOption(53, new byte[] { 3 }); // DHCPREQUEST

        var serverId = IPAddress.Parse("192.168.1.1");
        var router = IPAddress.Parse("192.168.1.1");
        var dns = IPAddress.Parse("192.168.1.53");
        var webproxy = "http://192.168.1.123";
        var subnetMask = IPAddress.Parse("255.255.255.0");

        var ackBytes = DhcpPacketCodec.BuildAck(
            req,
            IPAddress.Parse("192.168.1.50"),
            serverId,
            router,
            dns,
            subnetMask,
            TimeSpan.FromHours(1),
            domainName: null,
            webproxy: webproxy);

        var parsed = DhcpPacketCodec.Parse(ackBytes);

        var proxyOpt = parsed.Options.FirstOrDefault(o => o.Code == 252);
        Assert.NotNull(proxyOpt);
        Assert.Equal(Encoding.UTF8.GetBytes(webproxy), proxyOpt.Data);
    }

    [Fact]
    public void InformAck_Includes_WebProxy_When_Configured()
    {
        var req = MakeBasePacket(requestWebProxy: true);
        req.Options[0] = new DhcpOption(53, new byte[] { 8 }); // DHCPINFORM
        req.Ciaddr = IPAddress.Parse("192.168.1.77");

        var serverId = IPAddress.Parse("192.168.1.1");
        var router = IPAddress.Parse("192.168.1.1");
        var dns = IPAddress.Parse("192.168.1.53");
        var webproxy = "http://192.168.1.123";
        var subnetMask = IPAddress.Parse("255.255.255.0");

        var ackBytes = DhcpPacketCodec.BuildInformAck(
            req,
            serverId,
            router,
            dns,
            subnetMask,
            domainName: null,
            webproxy: webproxy);

        var parsed = DhcpPacketCodec.Parse(ackBytes);

        var proxyOpt = parsed.Options.FirstOrDefault(o => o.Code == 252);
        Assert.NotNull(proxyOpt);
        Assert.Equal(Encoding.UTF8.GetBytes(webproxy), proxyOpt.Data);
    }

    [Fact]
    public void InformAck_Does_Not_Include_WebProxy_When_Not_Configured()
    {
        var req = MakeBasePacket(requestWebProxy: true);
        req.Options[0] = new DhcpOption(53, new byte[] { 8 }); // DHCPINFORM
        req.Ciaddr = IPAddress.Parse("192.168.1.77");

        var serverId = IPAddress.Parse("192.168.1.1");
        var router = IPAddress.Parse("192.168.1.1");
        var dns = IPAddress.Parse("192.168.1.53");
        var subnetMask = IPAddress.Parse("255.255.255.0");

        var ackBytes = DhcpPacketCodec.BuildInformAck(
            req,
            serverId,
            router,
            dns,
            subnetMask,
            domainName: null,
            webproxy: null);

        var parsed = DhcpPacketCodec.Parse(ackBytes);

        Assert.DoesNotContain(parsed.Options, o => o.Code == 252);
    }
}
