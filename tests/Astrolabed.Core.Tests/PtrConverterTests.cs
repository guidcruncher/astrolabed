namespace Astrolabed.Core.Tests.Network;

using System.Net;

using Astrolabed.Core.Network;

using Xunit;

public class PtrConverterTests
{
    [Fact]
    public void ToIPAddress_NullInput_ThrowsArgumentNullException()
    {
        string ptrAddress = null!;

        Assert.Throws<ArgumentNullException>(() => PtrConverter.ToIPAddress(ptrAddress));
    }

    [Theory]
    [InlineData("invalid.domain.com")]
    [InlineData("1.1.1.in-addr.arpa")]
    [InlineData("1.1.1.1.1.in-addr.arpa")]
    [InlineData("abc.168.1.1.in-addr.arpa")]
    public void ToIPAddress_InvalidIPv4Ptr_ThrowsFormatException(string ptrAddress)
    {
        Assert.Throws<FormatException>(() => PtrConverter.ToIPAddress(ptrAddress));
    }

    [Theory]
    [InlineData("1.1.168.192.in-addr.arpa", "192.168.1.1")]
    [InlineData("1.1.168.192.in-addr.arpa.", "192.168.1.1")]
    [InlineData("254.0.0.10.IN-ADDR.ARPA", "10.0.0.254")]
    public void ToIPAddress_ValidIPv4Ptr_ReturnsCorrectIPAddress(string ptrAddress, string expectedIp)
    {
        IPAddress result = PtrConverter.ToIPAddress(ptrAddress);

        Assert.Equal(IPAddress.Parse(expectedIp), result);
    }

    [Theory]
    [InlineData("1.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.8.b.d.0.1.0.0.2.ip6.arpa", "2001:db8::1")]
    [InlineData("1.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.8.B.D.0.1.0.0.2.IP6.ARPA.", "2001:db8::1")]
    public void ToIPAddress_ValidIPv6Ptr_ReturnsCorrectIPAddress(string ptrAddress, string expectedIp)
    {
        IPAddress result = PtrConverter.ToIPAddress(ptrAddress);

        Assert.Equal(IPAddress.Parse(expectedIp), result);
    }

    [Fact]
    public void TryParse_InvalidFormat_ReturnsFalseAndNull()
    {
        bool success = PtrConverter.TryParse("invalid.ptr.address".AsSpan(), out IPAddress? address);

        Assert.False(success);
        Assert.Null(address);
    }
}
