using Astrolabed.Ntp.Options;
using Astrolabed.Ntp.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Astrolabed.Ntp.Tests;

public class CompositeTimeResolverTests
{
    private readonly Mock<LocalTimeResolver> _localTimeResolverMock;
    private readonly Mock<UpstreamTimeResolver> _upstreamTimeResolverMock;
    private readonly Mock<IOptionsMonitor<NtpServerOptions>> _optionsMonitorMock;
    private readonly Mock<ILogger<CompositeTimeResolver>> _loggerMock;

    public CompositeTimeResolverTests()
    {
        _localTimeResolverMock = new Mock<LocalTimeResolver>();
        _upstreamTimeResolverMock = new Mock<UpstreamTimeResolver>();
        _optionsMonitorMock = new Mock<IOptionsMonitor<NtpServerOptions>>();
        _loggerMock = new Mock<ILogger<CompositeTimeResolver>>();
    }

    [Fact]
    public async Task GetCurrentTimeAsync_WhenModeIsLocal_ReturnsLocalTime()
    {
        // Arrange
        var expectedTime = new DateTimeOffset(2026, 8, 25, 12, 0, 0, TimeSpan.Zero);
        var options = new NtpServerOptions { ResolverMode = TimeResolverMode.Local };

        _optionsMonitorMock.Setup(o => o.CurrentValue).Returns(options);
        _localTimeResolverMock.Setup(r => r.GetCurrentTimeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTime);

        var resolver = new CompositeTimeResolver(
            _localTimeResolverMock.Object,
            _upstreamTimeResolverMock.Object,
            _optionsMonitorMock.Object,
            _loggerMock.Object);

        // Act
        var actualTime = await resolver.GetCurrentTimeAsync();

        // Assert
        Assert.Equal(expectedTime, actualTime);
        _localTimeResolverMock.Verify(r => r.GetCurrentTimeAsync(It.IsAny<CancellationToken>()), Times.Once);
        _upstreamTimeResolverMock.Verify(r => r.GetCurrentTimeAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetCurrentTimeAsync_WhenModeIsUpstreamAndSucceeds_ReturnsUpstreamTime()
    {
        // Arrange
        var expectedTime = new DateTimeOffset(2026, 8, 25, 12, 0, 0, TimeSpan.Zero);
        var options = new NtpServerOptions { ResolverMode = TimeResolverMode.Upstream };

        _optionsMonitorMock.Setup(o => o.CurrentValue).Returns(options);
        _upstreamTimeResolverMock.Setup(r => r.GetCurrentTimeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTime);

        var resolver = new CompositeTimeResolver(
            _localTimeResolverMock.Object,
            _upstreamTimeResolverMock.Object,
            _optionsMonitorMock.Object,
            _loggerMock.Object);

        // Act
        var actualTime = await resolver.GetCurrentTimeAsync();

        // Assert
        Assert.Equal(expectedTime, actualTime);
        _upstreamTimeResolverMock.Verify(r => r.GetCurrentTimeAsync(It.IsAny<CancellationToken>()), Times.Once);
        _localTimeResolverMock.Verify(r => r.GetCurrentTimeAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetCurrentTimeAsync_WhenUpstreamFailsWithNonCancellationException_FallsBackToLocalTime()
    {
        // Arrange
        var expectedFallbackTime = new DateTimeOffset(2026, 8, 25, 12, 0, 0, TimeSpan.Zero);
        var options = new NtpServerOptions { ResolverMode = TimeResolverMode.Upstream };

        _optionsMonitorMock.Setup(o => o.CurrentValue).Returns(options);
        _upstreamTimeResolverMock.Setup(r => r.GetCurrentTimeAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Network issue"));
        _localTimeResolverMock.Setup(r => r.GetCurrentTimeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedFallbackTime);

        var resolver = new CompositeTimeResolver(
            _localTimeResolverMock.Object,
            _upstreamTimeResolverMock.Object,
            _optionsMonitorMock.Object,
            _loggerMock.Object);

        // Act
        var actualTime = await resolver.GetCurrentTimeAsync();

        // Assert
        Assert.Equal(expectedFallbackTime, actualTime);
        _upstreamTimeResolverMock.Verify(r => r.GetCurrentTimeAsync(It.IsAny<CancellationToken>()), Times.Once);
        _localTimeResolverMock.Verify(r => r.GetCurrentTimeAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCurrentTimeAsync_WhenUpstreamIsCanceled_ThrowsOperationCanceledException()
    {
        // Arrange
        var options = new NtpServerOptions { ResolverMode = TimeResolverMode.Upstream };
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _optionsMonitorMock.Setup(o => o.CurrentValue).Returns(options);
        _upstreamTimeResolverMock.Setup(r => r.GetCurrentTimeAsync(cts.Token))
            .ThrowsAsync(new OperationCanceledException(cts.Token));

        var resolver = new CompositeTimeResolver(
            _localTimeResolverMock.Object,
            _upstreamTimeResolverMock.Object,
            _optionsMonitorMock.Object,
            _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => resolver.GetCurrentTimeAsync(cts.Token).AsTask());
        _upstreamTimeResolverMock.Verify(r => r.GetCurrentTimeAsync(cts.Token), Times.Once);
        _localTimeResolverMock.Verify(r => r.GetCurrentTimeAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void Constructor_WithNullArguments_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CompositeTimeResolver(
            null!,
            _upstreamTimeResolverMock.Object,
            _optionsMonitorMock.Object,
            _loggerMock.Object));

        Assert.Throws<ArgumentNullException>(() => new CompositeTimeResolver(
            _localTimeResolverMock.Object,
            null!,
            _optionsMonitorMock.Object,
            _loggerMock.Object));

        Assert.Throws<ArgumentNullException>(() => new CompositeTimeResolver(
            _localTimeResolverMock.Object,
            _upstreamTimeResolverMock.Object,
            null!,
            _loggerMock.Object));

        Assert.Throws<ArgumentNullException>(() => new CompositeTimeResolver(
            _localTimeResolverMock.Object,
            _upstreamTimeResolverMock.Object,
            _optionsMonitorMock.Object,
            null!));
    }
}
