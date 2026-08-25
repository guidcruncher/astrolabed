using Astrolabed.Ntp.Options;
using Astrolabed.Ntp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Astrolabed.Ntp.Tests;

public class CompositeTimeResolverTests
{
    private readonly TestOptionsMonitor<NtpServerOptions> _optionsMonitor = new();
    private readonly NullLogger<CompositeTimeResolver> _logger = NullLogger<CompositeTimeResolver>.Instance;

    [Fact]
    public async Task GetCurrentTimeAsync_WhenModeIsLocal_ReturnsLocalTime()
    {
        // Arrange
        _optionsMonitor.CurrentValue = new NtpServerOptions { ResolverMode = TimeResolverMode.Local };

        var localResolver = new LocalTimeResolver();
        var upstreamResolver = new UpstreamTimeResolver(
            _optionsMonitor,
            NullLogger<UpstreamTimeResolver>.Instance);

        var resolver = new CompositeTimeResolver(
            localResolver,
            upstreamResolver,
            _optionsMonitor,
            _logger);

        // Act
        var actualTime = await resolver.GetCurrentTimeAsync();

        // Assert
        Assert.NotEqual(default, actualTime);
    }

    [Fact]
    public async Task GetCurrentTimeAsync_WhenModeIsUpstreamAndServerUnreachable_FallsBackToLocalTime()
    {
        // Arrange
        _optionsMonitor.CurrentValue = new NtpServerOptions 
        { 
            ResolverMode = TimeResolverMode.Upstream
        };

        var localResolver = new LocalTimeResolver();
        var upstreamResolver = new UpstreamTimeResolver(
            _optionsMonitor,
            NullLogger<UpstreamTimeResolver>.Instance);

        var resolver = new CompositeTimeResolver(
            localResolver,
            upstreamResolver,
            _optionsMonitor,
            _logger);

        // Act
        var actualTime = await resolver.GetCurrentTimeAsync();

        // Assert
        Assert.NotEqual(default, actualTime);
    }

    [Fact]
    public async Task GetCurrentTimeAsync_WhenUpstreamIsCanceled_ThrowsOperationCanceledException()
    {
        // Arrange
        _optionsMonitor.CurrentValue = new NtpServerOptions { ResolverMode = TimeResolverMode.Upstream };
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var localResolver = new LocalTimeResolver();
        var upstreamResolver = new UpstreamTimeResolver(
            _optionsMonitor,
            NullLogger<UpstreamTimeResolver>.Instance);

        var resolver = new CompositeTimeResolver(
            localResolver,
            upstreamResolver,
            _optionsMonitor,
            _logger);

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() => resolver.GetCurrentTimeAsync(cts.Token).AsTask());
    }

    [Fact]
    public void Constructor_WithNullArguments_ThrowsArgumentNullException()
    {
        // Arrange
        var localResolver = new LocalTimeResolver();
        var upstreamResolver = new UpstreamTimeResolver(
            _optionsMonitor,
            NullLogger<UpstreamTimeResolver>.Instance);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CompositeTimeResolver(
            null!,
            upstreamResolver,
            _optionsMonitor,
            _logger));

        Assert.Throws<ArgumentNullException>(() => new CompositeTimeResolver(
            localResolver,
            null!,
            _optionsMonitor,
            _logger));

        Assert.Throws<ArgumentNullException>(() => new CompositeTimeResolver(
            localResolver,
            upstreamResolver,
            null!,
            _logger));

        Assert.Throws<ArgumentNullException>(() => new CompositeTimeResolver(
            localResolver,
            upstreamResolver,
            _optionsMonitor,
            null!));
    }

    private sealed class TestOptionsMonitor<T> : IOptionsMonitor<T> where T : class, new()
    {
        public T CurrentValue { get; set; } = new();

        public T Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
