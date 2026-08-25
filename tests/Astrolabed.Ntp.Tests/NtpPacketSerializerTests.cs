using Astrolabed.Ntp.Options;
using Astrolabed.Ntp.Protocol;
using Astrolabed.Ntp.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
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

public class NtpServerHandlerTests
{
    private readonly Mock<IOptionsMonitor<NtpServerOptions>> _optionsMonitorMock;
    private readonly Mock<ILogger<NtpServerHandler>> _loggerMock;

    public NtpServerHandlerTests()
    {
        _optionsMonitorMock = new Mock<IOptionsMonitor<NtpServerOptions>>();
        _loggerMock = new Mock<ILogger<NtpServerHandler>>();
    }

    [Fact]
    public void CreateResponse_ValidRequest_CraftsCorrectNtpResponse()
    {
        // Arrange
        var options = new NtpServerOptions
        {
            Stratum = 1,
            Precision = -20,
            RootDelay = 0,
            RootDispersion = 10,
            ReferenceIdentifier = "LOCL"
        };

        _optionsMonitorMock.Setup(o => o.CurrentValue).Returns(options);

        var handler = new NtpServerHandler(_optionsMonitorMock.Object, _loggerMock.Object);

        var request = new NtpPacket
        {
            VersionNumber = 4,
            Mode = NtpMode.Client,
            Poll = 4,
            TransmitTimestamp = new NtpTimestamp(5000, 100)
        };

        var receiveTime = DateTimeOffset.UtcNow;
        var transmitTime = receiveTime.AddMilliseconds(1);

        // Act
        var response = handler.CreateResponse(request, receiveTime, transmitTime);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(NtpMode.Server, response.Mode);
        Assert.Equal(request.VersionNumber, response.VersionNumber);
        Assert.Equal(request.TransmitTimestamp, response.OriginTimestamp);
        Assert.Equal(NtpTimestamp.FromDateTimeOffset(receiveTime), response.ReceiveTimestamp);
        Assert.Equal(NtpTimestamp.FromDateTimeOffset(transmitTime), response.TransmitTimestamp);
        Assert.Equal(options.Stratum, response.Stratum);
        Assert.Equal(NtpPacketSerializer.ConvertReferenceIdToUint("LOCL"), response.ReferenceIdentifier);
    }

    [Fact]
    public void CreateResponse_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var handler = new NtpServerHandler(_optionsMonitorMock.Object, _loggerMock.Object);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => handler.CreateResponse(null!, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
    }
}
