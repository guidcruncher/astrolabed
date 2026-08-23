// File: src/Astrolabed.Data/Extensions/ServiceCollectionExtensions.cs
using Astrolabed.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Astrolabed.Data.Extensions;

/// <summary>
/// Service collection extensions for configuring database and repository services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers database infrastructure and repository implementations with compatible lifetimes.
    /// </summary>
    /// <param name="services">Target service collection.</param>
    /// <returns>The modified service collection.</returns>
    public static IServiceCollection AddAstrolabedData(this IServiceCollection services)
    {
        // Change IDbConnectionFactory and Repositories to Singleton if they manage thread-safe connection strings
        services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
        services.AddSingleton<IDhcpLeaseRepository, DapperDhcpLeaseRepository>();
        services.AddSingleton<IDiscoveredLanDeviceRepository, DapperDiscoveredLanDeviceRepository>();

        return services;
    }
}
