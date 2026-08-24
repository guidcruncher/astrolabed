namespace Astrolabed.Core.Tests.Network;

using System.Net;
using System.Net.Sockets;
using Astrolabed.Core.Network;
using Xunit;

public class EndPointExtensionsTests
{
    private sealed class CustomEndPoint : EndPoint
    {
        public override AddressFamily AddressFamily => AddressFamily.Unspecified;
    }

    [Fact]
    public void TryGetIPAddress_NullEndPoint_ReturnsFalseAndNullAddress()
    {
        EndPoint? endPoint = null;

        bool result = endPoint.TryGetIPAddress(out IPAddress? ipAddress);

        Assert.False(result);
        Assert.Null(ipAddress);
    }

    [Fact]
    public void TryGetIPAddress_IPEndPoint_ReturnsTrueAndAddress()
    {
        IPAddress expectedAddress = IPAddress.Parse("192.168.1.1");
        EndPoint endPoint = new IPEndPoint(expectedAddress, 80);

        bool result = endPoint.TryGetIPAddress(out IPAddress? ipAddress);

        Assert.True(result);
        Assert.Equal(expectedAddress, ipAddress);
    }

    [Fact]
    public void TryGetIPAddress_NonIPEndPoint_ReturnsFalseAndNullAddress()
    {
        EndPoint endPoint = new CustomEndPoint();

        bool result = endPoint.TryGetIPAddress(out IPAddress? ipAddress);

        Assert.False(result);
        Assert.Null(ipAddress);
    }

    [Fact]
    public void GetIPAddress_NullEndPoint_ThrowsArgumentNullException()
    {
        EndPoint endPoint = null!;

        Assert.Throws<ArgumentNullException>(() => endPoint.GetIPAddress());
    }

    [Fact]
    public void GetIPAddress_IPEndPoint_ReturnsAddress()
    {
        IPAddress expectedAddress = IPAddress.Parse("10.0.0.1");
        EndPoint endPoint = new IPEndPoint(expectedAddress, 443);

        IPAddress actualAddress = endPoint.GetIPAddress();

        Assert.Equal(expectedAddress, actualAddress);
    }

    [Fact]
    public void GetIPAddress_NonIPEndPoint_ThrowsInvalidOperationException()
    {
        EndPoint endPoint = new CustomEndPoint();

        Assert.Throws<InvalidOperationException>(() => endPoint.GetIPAddress());
    }
}
