using System.Net;
using Astrolabed.Dhcp.Protocol;
using Xunit;

namespace Astrolabed.Dhcp.Tests;

public class DhcpDecoderTests
{
    [Fact]
    public void Decode_BufferTooSmall_ThrowsArgumentException()
    {
        // Arrange
        byte[] smallBuffer = new byte[200];

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => DhcpDecoder.Decode(smallBuffer));
        Assert.Equal("buffer", exception.ParamName);
    }

    [Fact]
    public void Decode_ValidHeader_ParsesHeaderFieldsCorrectly()
    {
        // Arrange
        byte[] buffer = new byte[236];
        buffer[0] = (byte)DhcpOpCode.BootRequest;
        buffer[1] = 1; // Ethernet
        buffer[2] = 6; // Hardware address length
        buffer[3] = 2; // Hops

        // Transaction ID = 0x12345678 (bytes 4-7)
        buffer[4] = 0x12; buffer[5] = 0x34; buffer[6] = 0x56; buffer[7] = 0x78;

        // Seconds = 10 (bytes 8-9)
        buffer[8] = 0x00; buffer[9] = 0x0A;

        // Flags = 0x8000 (Broadcast) (bytes 10-11)
        buffer[10] = 0x80; buffer[11] = 0x00;

        // IP addresses
        new byte[] { 192, 168, 1, 100 }.CopyTo(buffer, 12); // Client IP
        new byte[] { 192, 168, 1, 101 }.CopyTo(buffer, 16); // Your IP
        new byte[] { 192, 168, 1, 1 }.CopyTo(buffer, 20);   // Server IP
        new byte[] { 192, 168, 1, 254 }.CopyTo(buffer, 24); // Gateway IP

        // MAC Address
        byte[] mac = [0x00, 0x11, 0x22, 0x33, 0x44, 0x55];
        mac.CopyTo(buffer, 28);

        // Act
        DhcpMessage message = DhcpDecoder.Decode(buffer);

        // Assert
        Assert.Equal(DhcpOpCode.BootRequest, message.Operation);
        Assert.Equal(1, message.HardwareType);
        Assert.Equal(6, message.HardwareAddressLength);
        Assert.Equal(2, message.Hops);
        Assert.Equal(0x12345678u, message.TransactionId);
        Assert.Equal((ushort)10, message.Seconds);
        Assert.Equal((ushort)0x8000, message.Flags);
        Assert.Equal(IPAddress.Parse("192.168.1.100"), message.ClientIpAddress);
        Assert.Equal(IPAddress.Parse("192.168.1.101"), message.YourIpAddress);
        Assert.Equal(IPAddress.Parse("192.168.1.1"), message.ServerIpAddress);
        Assert.Equal(IPAddress.Parse("192.168.1.254"), message.GatewayIpAddress);
        Assert.Equal(mac, message.ClientHardwareAddress[..6]);
    }

    [Fact]
    public void Decode_WithMagicCookieAndOptions_ParsesOptionsCorrectly()
    {
        // Arrange
        byte[] buffer = new byte[240 + 5]; // MinimumHeader (236) + MagicCookie (4) + Option payload
        buffer[0] = (byte)DhcpOpCode.BootRequest;

        // Magic Cookie (bytes 236-239)
        buffer[236] = 0x63; buffer[237] = 0x82; buffer[238] = 0x53; buffer[239] = 0x63;

        // Option 53: DhcpMessageType = Discover (1)
        byte[] optionsPayload =
        [
            (byte)DhcpOptionCode.DhcpMessageType, 1, (byte)DhcpMessageType.Discover,
            (byte)DhcpOptionCode.End
        ];

        byte[] fullPacket = new byte[240 + optionsPayload.Length];
        buffer.CopyTo(fullPacket, 0);
        optionsPayload.CopyTo(fullPacket, 240);

        // Act
        DhcpMessage message = DhcpDecoder.Decode(fullPacket);

        // Assert
        Assert.Single(message.Options);
        Assert.Equal(DhcpOptionCode.DhcpMessageType, message.Options[0].Code);
        Assert.Equal(DhcpMessageType.Discover, message.GetMessageType());
    }
}
