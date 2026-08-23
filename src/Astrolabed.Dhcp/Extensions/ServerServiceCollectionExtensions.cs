// File: src/Astrolabed.Dhcp/Extensions/ServiceCollectionExtensions.cs
using Astrolabed.Dhcp.Options;
using Astrolabed.Dhcp.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Astrolabed.Dhcp.Extensions;

/// <summary>
/// Extension methods for setting up DHCP server services in an <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds DHCP server options, handler execution, and background hosted engine services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configuration">The application configuration root containing the DHCP section.</param>
    /// <returns>The modified <see cref="IServiceCollection"/> for fluent method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="configuration"/> is <c>null</c>.</exception>
    public static IServiceCollection AddDhcpServer(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Options Configuration
        services.AddOptions<DhcpServerOptions>()
            .Bind(configuration.GetSection(DhcpServerOptions.Position))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Handlers (Scoped for individual packet / request processing)
        services.AddScoped<IDhcpHandler, DhcpHandler>();

        // Background Host Engine (Singleton hosted service executing per-packet IServiceScopeFactory)
        services.AddHostedService<DhcpEngine>();

        return services;
    }
}

