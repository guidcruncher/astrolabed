using Astrolabed.Core.Scheduler;
using Astrolabed.Data.Jobs;
using Astrolabed.Data.Mappers;
using Astrolabed.Data.Options;
using Astrolabed.Data.Repositories;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Astrolabed.Data.Extensions;

/// <summary>
/// Service collection extension methods to configure persistence, connection factories,
/// mapping infrastructure, and database cleanup background jobs.
/// </summary>
public static class RepositoryServiceCollectionExtensions
{
    /// <summary>
    /// Registers database persistence options, connection factories, mappers, repositories, and scheduled maintenance jobs.
    /// </summary>
    /// <param name="services">The target <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configuration">The application <see cref="IConfiguration"/> instance containing database settings.</param>
    /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="configuration"/> is null.</exception>
    public static IServiceCollection AddDatabasePersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Bind and validate options at startup to enforce fail-fast configuration policies
        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Stateless thread-safe mappers can remain Singleton
        services.AddSingleton<IDnsResponseEventMapper, DnsResponseEventMapper>();

        // Scoped connection factories and repositories prevent concurrent socket access and cross-thread connection leaks
        services.AddScoped<IDbConnectionFactory, DbConnectionFactory>();
        services.AddScoped<IDnsResponseEventRepository, DapperDnsResponseEventRepository>();
        services.AddScoped<IDiscoveredLanDeviceRepository, DapperDiscoveredLanDeviceRepository>();

        // Scheduled Data Jobs infrastructure
        services.AddJobScheduler();
        services.AddScheduledJob<CleanUpDnsActivityJob>();

        return services;
    }
}
