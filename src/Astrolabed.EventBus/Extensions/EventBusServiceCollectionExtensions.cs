using Astrolabed.EventBus.Extensions;
using Astrolabed.EventBus.Options;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;


namespace Astrolabed.EventBus.Extensions;

/// <summary>
/// Extension methods for configuring the in-process event bus in Microsoft DI containers.
/// </summary>
public static class EventBusServiceCollectionExtensions
{
    /// <summary>
    /// Registers the central Broker in the root top-level host container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Optional configuration section for EventBusOptions.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddRootEventBroker(this IServiceCollection services, IConfiguration? configuration = null)
    {
        var optionsBuilder = services.AddOptions<EventBusOptions>();
        if (configuration is not null)
        {
            optionsBuilder.Bind(configuration.GetSection("EventBus"));
        }

        services.TryAddSingleton<InProcEventBroker>();
        services.TryAddSingleton<IInProcEventBroker>(sp => sp.GetRequiredService<InProcEventBroker>());

        return services;
    }

    /// <summary>
    /// Registers a sub-host with the event bus infrastructure using a shared central broker instance.
    /// </summary>
    /// <param name="services">The sub-host service collection.</param>
    /// <param name="centralBroker">The central broker instance from the root host.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSubHostEventBus(this IServiceCollection services, IInProcEventBroker centralBroker)
    {
        ArgumentNullException.ThrowIfNull(centralBroker);

        services.TryAddSingleton(centralBroker);
        services.AddHostedService<SubHostEventListenerHostedService>();

        return services;
    }

    /// <summary>
    /// Registers an event listener implementation for a specific message payload type in the host DI container.
    /// </summary>
    /// <typeparam name="TMessage">The message payload type.</typeparam>
    /// <typeparam name="TListener">The listener implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEventListener<TMessage, TListener>(this IServiceCollection services)
        where TMessage : notnull
        where TListener : class, IEventListener<TMessage>
    {
        services.AddScoped<IEventListener<TMessage>, TListener>();
        services.AddSingleton<IEventListenerMarker>(new EventListenerDescriptor(typeof(TMessage)));

        return services;
    }
}
