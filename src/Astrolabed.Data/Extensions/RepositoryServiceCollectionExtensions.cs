using Astrolabed.Core.Scheduler;
using Astrolabed.Data.Jobs;
using Astrolabed.Data.Mappers;
using Astrolabed.Data.Models;
using Astrolabed.Data.Options;
using Astrolabed.Data.Repositories;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Astrolabed.Data.Extensions;

/// <summary>
/// Service collection extension methods to configure persistence and mapping infrastructure dependencies.
/// </summary>
public static class RepositoryServiceCollectionExtensions
{
    public static IServiceCollection AddDatabasePersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<DatabaseOptions>(
            configuration.GetSection(DatabaseOptions.SectionName));

        services.AddSingleton<IDnsResponseEventMapper, DnsResponseEventMapper>();
        services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
        services.AddScoped<IDnsResponseEventRepository, DapperDnsResponseEventRepository>();

        // Scheduled Data jobs
        services.AddJobScheduler();
        services.AddScheduledJob<CleanUpDnsActivityJob>();

        return services;
    }
}
