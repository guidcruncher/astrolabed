using Astrolabed.Ntp.Protocol;

using Xunit;

namespace Astrolabed.Ntp.Tests;

public class NtpPacketSerializerTests
{
    [Fact]
    public void Serialize_And_Deserialize_ValidPacket_MatchesHeaderFields()
    {
        // Arrange
        var packet = new NtpPacket
        {
            LeapIndicator = NtpLeapIndicator.NoWarning,
            VersionNumber = 4,
            Mode = NtpMode.Server,
            Stratum = 2,
            Poll = 6,
            Precision = -20,
            RootDelay = 100,
            RootDispersion = 200,
            ReferenceIdentifier = 0x47505320, // "GPS "
            TransmitTimestamp = new NtpTimestamp(1000, 2000)
        };

        Span<byte> buffer = stackalloc byte[NtpPacket.HeaderSize];

        // Act
        int bytesWritten = NtpPacketSerializer.Serialize(packet, buffer);
        var deserialized = NtpPacketSerializer.Deserialize(buffer);

        // Assert
        Assert.Equal(NtpPacket.HeaderSize, bytesWritten);
        Assert.NotNull(deserialized);
        Assert.Equal(packet.LeapIndicator, deserialized.LeapIndicator);
        Assert.Equal(packet.VersionNumber, deserialized.VersionNumber);
        Assert.Equal(packet.Mode, deserialized.Mode);
        Assert.Equal(packet.Stratum, deserialized.Stratum);
        Assert.Equal(packet.ReferenceIdentifier, deserialized.ReferenceIdentifier);
        Assert.Equal(packet.TransmitTimestamp, deserialized.TransmitTimestamp);
    }

    [Fact]
    public void ConvertReferenceIdToUint_ValidString_EncodesBigEndian()
    {
        // Arrange
        string refId = "GPS ";

        // Act
        uint result = NtpPacketSerializer.ConvertReferenceIdToUint(refId);

        // Assert ('G' = 0x47, 'P' = 0x50, 'S' = 0x53, ' ' = 0x20)
        Assert.Equal(0x47505320u, result);
    }

    [Fact]
    public void TryDeserialize_BufferTooSmall_ReturnsFalse()
    {
        // Arrange
        Span<byte> smallBuffer = stackalloc byte[20];

        // Act
        bool success = NtpPacketSerializer.TryDeserialize(smallBuffer, out var packet);

        // Assert
        Assert.False(success);
        Assert.Null(packet);
    }
}
