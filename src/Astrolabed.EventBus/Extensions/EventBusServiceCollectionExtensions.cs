// File: src/Astrolabed.EventBus/Extensions/EventBusServiceCollectionExtensions.cs
using Astrolabed.EventBus.Options;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Astrolabed.EventBus.Extensions;

/// <summary>
/// Service collection extensions for configuring the event broker and event listeners.
/// </summary>
public static class EventBusServiceCollectionExtensions
{

    /// <summary>
    /// Registers the root event broker and configures event options.
    /// </summary>
    /// <param name="services">Target service collection.</param>
    /// <param name="configuration">Configuration provider instance.</param>
    /// <returns>The updated <see cref="IServiceCollection"/> instance.</returns>
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

        return services;
    }
}
