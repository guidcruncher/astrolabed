using Astrolabed.Dns.Models;
using Astrolabed.Dns.Serialization;

using Xunit;

namespace Astrolabed.Dns.Tests;

public class DnsWireParserTests
{
    [Fact]
    public void TryParse_BufferSmallerThanHeader_ReturnsFalse()
    {
        byte[] buffer = new byte[11];
        bool result = DnsWireParser.TryParse(buffer, out DnsWireMessage? message);

        Assert.False(result);
        Assert.Null(message);
    }

    [Fact]
    public void TryParse_ValidHeaderWithoutSections_ReturnsTrueAndCorrectFlags()
    {
        byte[] buffer = [
            0x10, 0x20, // Transaction ID: 0x1020
            0x81, 0x80, // Standard Query Response, No Error
            0x00, 0x00, // QDCOUNT = 0
            0x00, 0x00, // ANCOUNT = 0
            0x00, 0x00, // NSCOUNT = 0
            0x00, 0x00  // ARCOUNT = 0
        ];

        bool result = DnsWireParser.TryParse(buffer, out DnsWireMessage? message);

        Assert.True(result);
        Assert.NotNull(message);
        Assert.Equal(0x1020, message.TransactionId);
        Assert.True(message.IsResponse);
        Assert.True(message.RecursionDesired);
        Assert.True(message.RecursionAvailable);
        Assert.Equal(DnsResponseCode.NoError, message.ResponseCode);
    }

    [Fact]
    public void TryReadDomainName_ValidUncompressedDomain_ParsesCorrectly()
    {
        // Wire format representation of "example.com"
        byte[] buffer = [0x07, (byte)'e', (byte)'x', (byte)'a', (byte)'m', (byte)'p', (byte)'l', (byte)'e', 0x03, (byte)'c', (byte)'o', (byte)'m', 0x00];
        int offset = 0;

        bool success = DnsWireParser.TryReadDomainName(buffer, ref offset, out string domain);

        Assert.True(success);
        Assert.Equal("example.com", domain);
        Assert.Equal(buffer.Length, offset);
    }

    [Fact]
    public void TryReadDomainName_CompressionPointerLoop_ReturnsFalse()
    {
        // Pointer pointing directly to itself (Infinite Loop Protection)
        byte[] buffer = [0xC0, 0x00];
        int offset = 0;

        bool success = DnsWireParser.TryReadDomainName(buffer, ref offset, out _);

        Assert.False(success);
    }
}
