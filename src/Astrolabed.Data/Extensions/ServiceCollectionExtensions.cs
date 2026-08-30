// File: src/Astrolabed.Data/Extensions/ServiceCollectionExtensions.cs
using Astrolabed.Core.Scheduler;
using Astrolabed.Data.Jobs;
using Astrolabed.Data.Mappers;
using Astrolabed.Data.Options;
using Astrolabed.Data.Repositories;
using Astrolabed.Data.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Astrolabed.Data.Extensions;

/// <summary>
/// Service collection extensions for registering database infrastructure, repositories, mappers, and initialization execution.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers database persistence options, connection factories, mappers, repositories, schema providers, and scheduled jobs.
    /// </summary>
    /// <param name="services">The target <see cref="IServiceCollection"/> to add database services to.</param>
    /// <param name="configuration">The application <see cref="IConfiguration"/> instance containing settings.</param>
    /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="configuration"/> is <c>null</c>.</exception>
    public static IServiceCollection AddAstrolabedData(
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

        // Core Database Initializer & Schema Services
        services.AddSingleton<ISchemaProvider, EmbeddedSchemaProvider>();
        services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();

        // Connection Infrastructure & Mappers
        services.AddScoped<IDbConnectionFactory, DbConnectionFactory>();
        services.AddSingleton<IDnsResponseEventMapper, DnsResponseEventMapper>();

        // Repositories
        services.AddScoped<IDhcpLeaseRepository, DapperDhcpLeaseRepository>();
        services.AddScoped<IDiscoveredLanDeviceRepository, DapperDiscoveredLanDeviceRepository>();
        services.AddScoped<IDnsResponseEventRepository, DapperDnsResponseEventRepository>();
	services.AddScoped<IStatsRepository, DapperStatsRepository>();


        // Scheduled Maintenance Jobs Infrastructure
        services.AddJobScheduler();
        services.AddScheduledJob<CleanUpDnsActivityJob>();

        return services;
    }

    /// <summary>
    /// Asynchronously initializes and migrates the database schema using the registered <see cref="IDatabaseInitializer"/>.
    /// Should be invoked during application startup after building the <see cref="IHost"/>.
    /// </summary>
    /// <param name="host">The built application <see cref="IHost"/> instance.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests during initialization.</param>
    /// <returns>A task representing the asynchronous initialization operation, returning the original <see cref="IHost"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="host"/> is <c>null</c>.</exception>
    public static async Task<IHost> InitializeDatabaseAsync(
        this IHost host,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(host);

        await using AsyncServiceScope scope = host.Services.CreateAsyncScope();
        IDatabaseInitializer initializer = scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>();

        await initializer.InitializeAsync(cancellationToken);

        return host;
    }
}
