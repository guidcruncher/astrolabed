using Astrolabed.Ntp.Options;
using Astrolabed.Ntp.Protocol;
using Astrolabed.Ntp.Services;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Xunit;

namespace Astrolabed.Ntp.Tests;

public class NtpServerHandlerTests
{
    private readonly TestOptionsMonitor<NtpServerOptions> _optionsMonitor = new();
    private readonly NullLogger<NtpServerHandler> _logger = NullLogger<NtpServerHandler>.Instance;

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

        _optionsMonitor.CurrentValue = options;

        var handler = new NtpServerHandler(_optionsMonitor, _logger);

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
        var handler = new NtpServerHandler(_optionsMonitor, _logger);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => handler.CreateResponse(null!, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
    }

    private sealed class TestOptionsMonitor<T> : IOptionsMonitor<T> where T : class, new()
    {
        public T CurrentValue { get; set; } = new();

        public T Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
