using Astrolabed.EventBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Astrolabed.EventBus.Example;

/// <summary>
/// Sample application demonstrating in-process multi-IHost message dispatching.
/// </summary>
public static class SampleProgram
{
    public static async Task Main(string[] args)
    {
        // 1. Build and start Root Host containing the central Broker
        using var rootHost = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddRootEventBroker(context.Configuration);
            })
            .Build();

        await rootHost.StartAsync();

        // Retrieve the shared broker singleton instance
        var centralBroker = rootHost.Services.GetRequiredService<IInProcEventBroker>();

        // 2. Build Sub Host 1 configured with local event listeners
        using var subHost1 = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddSubHostEventBus(centralBroker);
                services.AddEventListener<OrderCreatedEvent, OrderCreatedListener>();
            })
            .Build();

        await subHost1.StartAsync();

        // 3. Dispatch an event from the Root Host (or any service)
        var sampleEvent = new OrderCreatedEvent(Guid.NewGuid(), 299.99m);
        await centralBroker.PublishAsync(sampleEvent);

        // Allow fire-and-forget processing to complete
        await Task.Delay(200);

        await subHost1.StopAsync();
        await rootHost.StopAsync();
    }
}
