using Astrolabed.EventBus.Options;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Astrolabed.EventBus.Extensions;

/// <summary>
/// Service collection extension methods for registering in-process event broker infrastructure,
/// sub-host integration, and event listener descriptors within Microsoft Dependency Injection containers.
/// </summary>
public static class EventBusServiceCollectionExtensions
{
    /// <summary>
    /// Registers the central in-process event broker and its options in the root top-level host service collection.
    /// </summary>
    /// <param name="services">The target <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configuration">Optional <see cref="IConfiguration"/> section containing <see cref="EventBusOptions"/> settings.</param>
    /// <returns>The modified <see cref="IServiceCollection"/> instance for fluent method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddRootEventBroker(
        this IServiceCollection services,
        IConfiguration? configuration = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var optionsBuilder = services.AddOptions<EventBusOptions>()
            .ValidateDataAnnotations()
            .ValidateOnStart();

        if (configuration is not null)
        {
            optionsBuilder.Bind(configuration.GetSection(EventBusOptions.SectionName));
        }

        services.TryAddSingleton<InProcEventBroker>();
        services.TryAddSingleton<IInProcEventBroker>(static sp => sp.GetRequiredService<InProcEventBroker>());

        return services;
    }

    /// <summary>
    /// Registers a sub-host with the event bus infrastructure using a shared central broker instance provided by the root host.
    /// </summary>
    /// <param name="services">The sub-host service collection.</param>
    /// <param name="centralBroker">The central <see cref="IInProcEventBroker"/> instance from the root top-level host.</param>
    /// <returns>The modified <see cref="IServiceCollection"/> instance for fluent method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="centralBroker"/> is null.</exception>
    public static IServiceCollection AddSubHostEventBus(
        this IServiceCollection services,
        IInProcEventBroker centralBroker)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(centralBroker);

        services.TryAddSingleton(centralBroker);
        services.AddHostedService<SubHostEventListenerHostedService>();

        return services;
    }

    /// <summary>
    /// Registers an event listener implementation for a specific message payload type in the host DI container.
    /// </summary>
    /// <typeparam name="TMessage">The event message payload type handled by the listener.</typeparam>
    /// <typeparam name="TListener">The concrete listener implementation type handling the event message.</typeparam>
    /// <param name="services">The target <see cref="IServiceCollection"/> to register the listener into.</param>
    /// <returns>The modified <see cref="IServiceCollection"/> instance for fluent method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddEventListener<TMessage, TListener>(this IServiceCollection services)
        where TMessage : notnull
        where TListener : class, IEventListener<TMessage>
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IEventListener<TMessage>, TListener>();
        services.AddSingleton<IEventListenerMarker>(_ => new EventListenerDescriptor(typeof(TMessage)));

        return services;
    }
}
