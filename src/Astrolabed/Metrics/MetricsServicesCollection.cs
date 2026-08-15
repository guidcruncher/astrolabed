using Astrolabed.Data.Repositories;
using Astrolabed.Events;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Astrolabed.Metrics.Bootstrap;

public static class MetricsServiceCollectionExtensions
{
    public static IServiceCollection AddMetricServices(
        this IServiceCollection services, IConfiguration config)
    {

        services.AddSingleton<MetricsRepository>();
        services.AddSingleton<MetricsRegistry>();
        services.AddSingleton<IEventConsumer, MetricsEventConsumer>();

        return services;
    }
}
