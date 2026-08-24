// File: src/Astrolabed.EventBus/Extensions/ServiceCollectionExtensions.cs
using Astrolabed.EventBus.Options;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Astrolabed.EventBus.Extensions;

/// <summary>
/// Service collection extensions for configuring the event broker and event listeners.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the root event broker, hosting background wireup services, and configures event options.
    /// </summary>
    /// <param name="services">Target service collection.</param>
    /// <param name="configuration">Configuration provider instance.</param>
    /// <returns>The updated <see cref="IServiceCollection"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="configuration"/> is <c>null</c>.</exception>
    public static IServiceCollection AddRootEventBroker(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<EventBusOptions>()
            .Bind(configuration.GetSection(EventBusOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IInProcEventBroker, InProcEventBroker>();
        services.AddHostedService<SubHostEventListenerHostedService>();

        return services;
    }

    /// <summary>
    /// Registers an event listener for a specific event type using a scoped lifetime and registers its discovery marker.
    /// </summary>
    /// <typeparam name="TEvent">The event type to listen for.</typeparam>
    /// <typeparam name="TListener">The listener implementation type.</typeparam>
    /// <param name="services">Target service collection.</param>
    /// <returns>The updated <see cref="IServiceCollection"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is <c>null</c>.</exception>
    public static IServiceCollection AddEventListener<TEvent, TListener>(this IServiceCollection services)
        where TListener : class, IEventListener<TEvent>
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IEventListener<TEvent>, TListener>();
        services.AddSingleton<IEventListenerMarker>(new EventListenerDescriptor(typeof(TEvent)));

        return services;
    }

    /// <summary>
    /// Automatically registers all <see cref="IEventListener{TEvent}"/> interfaces implemented by <typeparamref name="TListener"/> using a scoped lifetime and registers their discovery markers.
    /// </summary>
    /// <typeparam name="TListener">The concrete listener type to register.</typeparam>
    /// <param name="services">Target service collection.</param>
    /// <returns>The updated <see cref="IServiceCollection"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <typeparamref name="TListener"/> does not implement any event listener interfaces.</exception>
    public static IServiceCollection AddEventListener<TListener>(this IServiceCollection services)
        where TListener : class
    {
        ArgumentNullException.ThrowIfNull(services);

        Type listenerType = typeof(TListener);
        Type serviceInterfaceType = typeof(IEventListener<>);

        Type[] implementedInterfaces = listenerType.GetInterfaces();
        bool registered = false;

        foreach (Type iface in implementedInterfaces)
        {
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == serviceInterfaceType)
            {
                services.AddScoped(iface, listenerType);

                Type messageType = iface.GetGenericArguments()[0];
                services.AddSingleton<IEventListenerMarker>(new EventListenerDescriptor(messageType));

                registered = true;
            }
        }

        if (!registered)
        {
            throw new InvalidOperationException($"Type '{listenerType.FullName}' does not implement any '{serviceInterfaceType.Name}' interfaces.");
        }

        return services;
    }
}
