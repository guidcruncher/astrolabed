using Microsoft.Extensions.DependencyInjection;

namespace Astrolabed.Core.Scheduler;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJobScheduler()
    {
        services.AddSingleton(TimeProvider.System);
        return services;
    }

    public static IServiceCollection AddScheduledJob<TJob>(this IServiceCollection services)
        where TJob : class, IScheduledJob
    {
        services.AddScoped<TJob>();
        services.AddHostedService<ScheduledJobWorker<TJob>>();
        return services;
    }
}
