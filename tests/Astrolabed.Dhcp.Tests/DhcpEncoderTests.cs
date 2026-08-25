using System.Net;

using Astrolabed.Dhcp.Protocol;

using Xunit;

namespace Astrolabed.Dhcp.Tests;

public class DhcpEncoderTests
{
    [Fact]
    public void Encode_NullMessage_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => DhcpEncoder.Encode(null!));
    }

    [Fact]
    public void EncodeToSpan_BufferTooSmall_ThrowsArgumentException()
    {
        // Arrange
        var message = new DhcpMessage();
        byte[] destination = new byte[200];

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => DhcpEncoder.EncodeToSpan(message, destination));
        Assert.Equal("destination", exception.ParamName);
    }

    [Fact]
    public void Encode_And_Decode_RoundTripsSuccessfully()
    {
        // Arrange
        var original = new DhcpMessage
        {
            Operation = DhcpOpCode.BootReply,
            HardwareType = 1,
            HardwareAddressLength = 6,
            TransactionId = 0xABCDEF12,
            YourIpAddress = IPAddress.Parse("10.0.0.50"),
            ServerIpAddress = IPAddress.Parse("10.0.0.1")
        };
        original.ClientHardwareAddress[0] = 0xAA;
        original.ClientHardwareAddress[1] = 0xBB;
        original.ClientHardwareAddress[2] = 0xCC;

        original.Options.Add(DhcpOption.CreateByte(DhcpOptionCode.DhcpMessageType, (byte)DhcpMessageType.Offer));
        original.Options.Add(DhcpOption.CreateIpAddress(DhcpOptionCode.SubnetMask, IPAddress.Parse("255.255.255.0")));

        // Act
        byte[] encoded = DhcpEncoder.Encode(original);
        DhcpMessage decoded = DhcpDecoder.Decode(encoded);

        // Assert
        Assert.Equal(original.Operation, decoded.Operation);
        Assert.Equal(original.TransactionId, decoded.TransactionId);
        Assert.Equal(original.YourIpAddress, decoded.YourIpAddress);
        Assert.Equal(original.ServerIpAddress, decoded.ServerIpAddress);
        Assert.Equal(DhcpMessageType.Offer, decoded.GetMessageType());
        Assert.Contains(decoded.Options, o => o.Code == DhcpOptionCode.SubnetMask);
    }
}
