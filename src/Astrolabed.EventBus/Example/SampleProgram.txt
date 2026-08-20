namespace Astrolabed.EventBus.Example;

using Astrolabed.EventBus;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

/// <summary>
/// Sample application demonstrating in-process multi-IHost message dispatching.
/// </summary>
public static class SampleProgram
{
    public static async Task Main(string[] args)
    {
        using var rootHost = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddRootEventBroker(context.Configuration);
            })
            .Build();

        await rootHost.StartAsync();

        var centralBroker = rootHost.Services.GetRequiredService<IInProcEventBroker>();

        using var subHost1 = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddSubHostEventBus(centralBroker);
                services.AddEventListener<OrderCreatedEvent, OrderCreatedListener>();
            })
            .Build();

        await subHost1.StartAsync();

        var sampleEvent = new OrderCreatedEvent(Guid.NewGuid(), 299.99m);
        await centralBroker.PublishAsync(sampleEvent);

        await Task.Delay(200);

        await subHost1.StopAsync();
        await rootHost.StopAsync();
    }
}
