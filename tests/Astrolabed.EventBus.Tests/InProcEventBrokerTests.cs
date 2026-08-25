namespace Astrolabed.EventBus.Tests;

using Astrolabed.EventBus;
using Astrolabed.EventBus.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Xunit;

public class InProcEventBrokerTests
{
    private readonly IOptions<EventBusOptions> _defaultOptions = Options.Create(new EventBusOptions());

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new InProcEventBroker(null!, _defaultOptions));
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new InProcEventBroker(NullLogger<InProcEventBroker>.Instance, null!));
    }

    [Fact]
    public async Task PublishAsync_NullPayload_ThrowsArgumentNullException()
    {
        InProcEventBroker broker = new(NullLogger<InProcEventBroker>.Instance, _defaultOptions);

        await Assert.ThrowsAsync<ArgumentNullException>(async () => await broker.PublishAsync<string>(null!));
    }

    [Fact]
    public async Task PublishAsync_NoSubscribers_CompletesSuccessfully()
    {
        InProcEventBroker broker = new(NullLogger<InProcEventBroker>.Instance, _defaultOptions);

        await broker.PublishAsync("Sample Message");
    }

    [Fact]
    public async Task RegisterListener_And_PublishAsync_InvokesHandler()
    {
        FakeTimeProvider timeProvider = new();
        DateTimeOffset expectedTime = new(2026, 8, 25, 14, 0, 0, TimeSpan.Zero);
        timeProvider.SetUtcNow(expectedTime);

        InProcEventBroker broker = new(NullLogger<InProcEventBroker>.Instance, _defaultOptions, timeProvider);

        TaskCompletionSource<EventMessage<string>> tcs = new();

        using BrokerSubscriptionToken token = broker.RegisterListener<string>((message, _) =>
        {
            tcs.SetResult(message);
            return ValueTask.CompletedTask;
        });

        await broker.PublishAsync("Hello Broker");

        EventMessage<string> result = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal("Hello Broker", result.Payload);
        Assert.Equal(expectedTime, result.Timestamp);
    }

    [Fact]
    public async Task Unsubscribe_TokenDisposed_StopsReceivingEvents()
    {
        InProcEventBroker broker = new(NullLogger<InProcEventBroker>.Instance, _defaultOptions);

        int callCount = 0;
        BrokerSubscriptionToken token = broker.RegisterListener<string>((_, _) =>
        {
            Interlocked.Increment(ref callCount);
            return ValueTask.CompletedTask;
        });

        token.Dispose();

        await broker.PublishAsync("Test Disposed");
        await Task.Delay(100);

        Assert.Equal(0, callCount);
    }
}
