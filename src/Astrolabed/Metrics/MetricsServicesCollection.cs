using Astrolabed.Events;
using Astrolabed.Exporters;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Astrolabed.Metrics.Bootstrap;

public static class MetricsServiceCollectionExtensions
{
    public static IServiceCollection AddMetricServices(
        this IServiceCollection services, IConfiguration config)
    {
        var server = config.Get<ServerOptions>() ?? new ServerOptions();
        var metrics = server.Metrics;

        services.AddSingleton<MetricsRegistry>();
        services.AddSingleton<IEventConsumer, MetricsEventConsumer>();

        services.AddHostedService<EventDispatcherService>();

        return services;
    }
}

