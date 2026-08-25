using Astrolabed.Ntp.Options;
using Astrolabed.Ntp.Protocol;
using Astrolabed.Ntp.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Astrolabed.Ntp.Tests;

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
