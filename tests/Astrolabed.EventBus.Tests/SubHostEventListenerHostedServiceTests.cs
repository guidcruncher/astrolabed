namespace Astrolabed.EventBus.Tests;

using Astrolabed.EventBus;
using Astrolabed.EventBus.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

public class SubHostEventListenerHostedServiceTests
{
    private sealed class SampleEvent
    {
        public string Content { get; set; } = string.Empty;
    }

    private sealed class SampleEventListener : IEventListener<SampleEvent>
    {
        public static TaskCompletionSource<SampleEvent> Tcs { get; } = new();

        public Type MessageType => typeof(SampleEvent);

        public ValueTask HandleAsync(EventMessage<SampleEvent> message, CancellationToken cancellationToken = default)
        {
            Tcs.TrySetResult(message.Payload);
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        InProcEventBroker broker = new(NullLogger<InProcEventBroker>.Instance, Options.Create(new EventBusOptions()));

        Assert.Throws<ArgumentNullException>(() => new SubHostEventListenerHostedService(
            null!,
            broker,
            NullLogger<SubHostEventListenerHostedService>.Instance));
    }

    [Fact]
    public void Constructor_NullBroker_ThrowsArgumentNullException()
    {
        ServiceCollection services = new();
        ServiceProvider provider = services.BuildServiceProvider();

        Assert.Throws<ArgumentNullException>(() => new SubHostEventListenerHostedService(
            provider,
            null!,
            NullLogger<SubHostEventListenerHostedService>.Instance));
    }

    [Fact]
    public async Task StartAsync_RegistersDiscoveredListenersAndHandlesEvents()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton<IOptions<EventBusOptions>>(Options.Create(new EventBusOptions()));
        services.AddSingleton<IInProcEventBroker, InProcEventBroker>();
        services.AddTransient<IEventListener<SampleEvent>, SampleEventListener>();
        services.AddSingleton<IEventListenerMarker, EventListenerDescriptor>(_ => new EventListenerDescriptor(typeof(SampleEvent)));

        ServiceProvider provider = services.BuildServiceProvider();
        IInProcEventBroker broker = provider.GetRequiredService<IInProcEventBroker>();

        SubHostEventListenerHostedService hostedService = new(
            provider,
            broker,
            NullLogger<SubHostEventListenerHostedService>.Instance);

        await hostedService.StartAsync(CancellationToken.None);

        SampleEvent payload = new() { Content = "Routed successfully" };
        await broker.PublishAsync(payload);

        SampleEvent result = await SampleEventListener.Tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal("Routed successfully", result.Content);

        await hostedService.StopAsync(CancellationToken.None);
        await hostedService.DisposeAsync();
    }
}
