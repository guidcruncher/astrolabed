using System.Net;
using Astrolabed.Dhcp.Protocol;
using Xunit;

namespace Astrolabed.Dhcp.Tests;

public class DhcpDecoderAdvancedTests
{
    [Fact]
    public void Decode_WithOptionOverload_ReadsOptionsFromFileNameAndSNameFields()
    {
        // Arrange
        byte[] buffer = new byte[300];
        buffer[0] = (byte)DhcpOpCode.BootRequest;

        // Magic Cookie at offset 236
        buffer[236] = 0x63; buffer[237] = 0x82; buffer[238] = 0x53; buffer[239] = 0x63;

        // Standard Options area (offset 240) -> OptionOverload = 3 (both file and sname contain options)
        buffer[240] = (byte)DhcpOptionCode.OptionOverload;
        buffer[241] = 1;
        buffer[242] = 3; 
        buffer[243] = (byte)DhcpOptionCode.End;

        // File field (offset 108, length 128) -> Subnet Mask option
        buffer[108] = (byte)DhcpOptionCode.SubnetMask;
        buffer[109] = 4;
        new byte[] { 255, 255, 255, 0 }.CopyTo(buffer, 110);
        buffer[114] = (byte)DhcpOptionCode.End;

        // SName field (offset 44, length 64) -> Router option
        buffer[44] = (byte)DhcpOptionCode.Router;
        buffer[45] = 4;
        new byte[] { 192, 168, 1, 1 }.CopyTo(buffer, 46);
        buffer[50] = (byte)DhcpOptionCode.End;

        // Act
        DhcpMessage message = DhcpDecoder.Decode(buffer);

        // Assert
        Assert.Contains(message.Options, o => o.Code == DhcpOptionCode.OptionOverload);
        Assert.Contains(message.Options, o => o.Code == DhcpOptionCode.SubnetMask);
        Assert.Contains(message.Options, o => o.Code == DhcpOptionCode.Router);

        DhcpOption subnetOpt = message.Options.First(o => o.Code == DhcpOptionCode.SubnetMask);
        Assert.Equal(new byte[] { 255, 255, 255, 0 }, subnetOpt.Data);

        DhcpOption routerOpt = message.Options.First(o => o.Code == DhcpOptionCode.Router);
        Assert.Equal(new byte[] { 192, 168, 1, 1 }, routerOpt.Data);
    }

    [Fact]
    public void Decode_TruncatedOptionData_HaltsParsingGracefully()
    {
        // Arrange
        byte[] buffer = new byte[245];
        buffer[236] = 0x63; buffer[237] = 0x82; buffer[238] = 0x53; buffer[239] = 0x63;

        // Option specifying length 10, but buffer ends after 2 bytes
        buffer[240] = (byte)DhcpOptionCode.HostName;
        buffer[241] = 10;
        buffer[242] = (byte)'a';
        buffer[243] = (byte)'b';

        // Act
        DhcpMessage message = DhcpDecoder.Decode(buffer);

        // Assert
        Assert.Empty(message.Options);
    }
}
