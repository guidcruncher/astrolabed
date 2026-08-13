using Astrolabed.Dhcp;
using Astrolabed.Dns;
using Astrolabed.Exporters;
using Astrolabed.Metrics;
using Astrolabed.Ntp;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Astrolabed.Events.Bootstrap;

public static class EventsServiceCollection
{
    public static IServiceCollection AddEventBus(this IServiceCollection services, IConfiguration config)
    {
        var server = config.Get<ServerOptions>() ?? new ServerOptions();
        var metrics = server.Metrics;

        // Inject options
        services.AddSingleton<MetricOptions>(metrics);

        // Core EventBus and Dispatcher Host
        services.AddSingleton<EventBus>();
        services.AddHostedService<EventDispatcherService>();

        // Metrics registry
        services.AddSingleton<MetricsRegistry>();

        // Metric facades
        services.AddSingleton<IDhcpMetrics, DhcpMetrics>();
        services.AddSingleton<IDnsMetrics, DnsMetrics>();
        services.AddSingleton<INtpMetrics, NtpMetrics>();

        // Exporters configuration
        if (!metrics.Enabled)
        {
            services.AddHostedService<NullEventExporter>();
            return services;
        }

        switch (metrics.StorageEngine?.ToLowerInvariant())
        {
            case "json":
                services.AddHostedService<JsonEventExporter>();
                break;

            case "prometheus":
                break;

            default:
                services.AddHostedService<NullEventExporter>();
                break;
        }

        return services;
    }
}
