namespace Astrolabed.Core.Tests.Network;

using System.Net;

using Astrolabed.Core.Network;

using Xunit;

public class IpAddressExtensionsTests
{
    [Fact]
    public void ToPtrFormat_NullAddress_ThrowsArgumentNullException()
    {
        IPAddress address = null!;

        Assert.Throws<ArgumentNullException>(() => address.ToPtrFormat());
    }

    [Theory]
    [InlineData("192.168.1.1", false, "1.1.168.192.in-addr.arpa")]
    [InlineData("192.168.1.1", true, "1.1.168.192.in-addr.arpa.")]
    [InlineData("10.0.0.254", false, "254.0.0.10.in-addr.arpa")]
    public void ToPtrFormat_IPv4Address_ReturnsExpectedPtrFormat(string ipString, bool includeTrailingDot, string expected)
    {
        IPAddress address = IPAddress.Parse(ipString);

        string result = address.ToPtrFormat(includeTrailingDot: includeTrailingDot);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("2001:db8::1", false, false, "1.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.8.b.d.0.1.0.0.2.ip6.arpa")]
    [InlineData("2001:db8::1", true, false, "1.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.8.b.d.0.1.0.0.2.ip6.arpa.")]
    [InlineData("2001:db8::1", false, true, "1.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.8.B.D.0.1.0.0.2.ip6.arpa")]
    public void ToPtrFormat_IPv6Address_ReturnsExpectedPtrFormat(string ipString, bool includeTrailingDot, bool useUpperCaseHex, string expected)
    {
        IPAddress address = IPAddress.Parse(ipString);

        string result = address.ToPtrFormat(includeTrailingDot: includeTrailingDot, useUpperCaseHex: useUpperCaseHex);

        Assert.Equal(expected, result);
    }
}
