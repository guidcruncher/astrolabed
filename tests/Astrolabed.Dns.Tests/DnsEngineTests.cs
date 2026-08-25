using System.Net;

using Astrolabed.Dns.Options;
using Astrolabed.Dns.Services;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Xunit;

namespace Astrolabed.Dns.Tests;

public class DnsEngineTests
{
    [Fact]
    public void Constructor_NullOptionsMonitor_ThrowsArgumentNullException()
    {
        var listeners = new[] { new FakeDnsListener() };
        Assert.Throws<ArgumentNullException>(() =>
            new DnsEngine(null!, listeners, NullLogger<DnsEngine>.Instance));
    }

    [Fact]
    public void Constructor_NullListeners_ThrowsArgumentNullException()
    {
        var monitor = new TestOptionsMonitor<DnsEngineOptions>(new DnsEngineOptions());
        Assert.Throws<ArgumentNullException>(() =>
            new DnsEngine(monitor, null!, NullLogger<DnsEngine>.Instance));
    }

    [Fact]
    public async Task ExecuteAsync_DisabledInOptions_LogsAndReturnsEarly()
    {
        // Arrange
        var options = new DnsEngineOptions();
        options.ListenAddress.Enabled = false;

        var monitor = new TestOptionsMonitor<DnsEngineOptions>(options);
        var listener = new FakeDnsListener();

        using var engine = new DnsEngine(monitor, new[] { listener }, NullLogger<DnsEngine>.Instance);
        using var cts = new CancellationTokenSource();

        // Act
        await engine.StartAsync(cts.Token);
        await engine.StopAsync(cts.Token);

        // Assert
        Assert.Equal(0, listener.ListenCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_EnabledInOptions_StartsListeners()
    {
        // Arrange
        var options = new DnsEngineOptions();
        options.ListenAddress.Enabled = true;
        options.ListenAddress.Address = "127.0.0.1";
        options.ListenAddress.Port = 5353;

        var monitor = new TestOptionsMonitor<DnsEngineOptions>(options);
        var listener = new FakeDnsListener();

        using var engine = new DnsEngine(monitor, new[] { listener }, NullLogger<DnsEngine>.Instance);
        using var cts = new CancellationTokenSource();

        // Act
        await engine.StartAsync(cts.Token);
        await Task.Delay(50);
        await engine.StopAsync(cts.Token);

        // Assert
        Assert.True(listener.ListenCallCount > 0);
        Assert.Equal(IPAddress.Parse("127.0.0.1"), listener.LastAddress);
        Assert.Equal(5353, listener.LastPort);
    }

    private sealed class FakeDnsListener : IDnsListener
    {
        public int ListenCallCount { get; private set; }
        public IPAddress? LastAddress { get; private set; }
        public int LastPort { get; private set; }

        public Task ListenAsync(IPAddress address, int port, CancellationToken cancellationToken)
        {
            ListenCallCount++;
            LastAddress = address;
            LastPort = port;
            return Task.Delay(Timeout.Infinite, cancellationToken);
        }
    }

    private sealed class TestOptionsMonitor<T>(T currentValue) : IOptionsMonitor<T>
    {
        public T CurrentValue => currentValue;
        public T Get(string? name) => currentValue;
        public IDisposable OnChange(Action<T, string?> listener) => DummyDisposable.Instance;

        private sealed class DummyDisposable : IDisposable
        {
            public static readonly DummyDisposable Instance = new();
            public void Dispose() { }
        }
    }
}
