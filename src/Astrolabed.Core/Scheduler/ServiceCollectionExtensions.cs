using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Astrolabed.Core.Scheduler;

/// <summary>
/// Extension methods for registering background job scheduling infrastructure and scheduled job workers into an <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers core job scheduling dependencies, including the default system <see cref="TimeProvider"/>.
    /// </summary>
    /// <param name="services">The target <see cref="IServiceCollection"/>.</param>
    /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddJobScheduler(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);

        return services;
    }

    /// <summary>
    /// Registers a scheduled job along with its managing hosted background service worker <see cref="ScheduledJobWorker{TJob}"/>.
    /// </summary>
    /// <typeparam name="TJob">The job type implementing <see cref="IScheduledJob"/>.</typeparam>
    /// <param name="services">The target <see cref="IServiceCollection"/>.</param>
    /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddScheduledJob<TJob>(this IServiceCollection services)
        where TJob : class, IScheduledJob
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<TJob>();
        services.AddHostedService<ScheduledJobWorker<TJob>>();

        return services;
    }
}
