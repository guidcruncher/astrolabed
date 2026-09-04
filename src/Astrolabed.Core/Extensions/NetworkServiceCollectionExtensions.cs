using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Astrolabed.Core.Options;
using Astrolabed.Core.Network;

namespace Astrolabed.Core.Extensions;

/// <summary>
/// Provides extension methods for registering network-related core services and options 
/// within the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers network services and configures their related options within the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/> instance containing network service configuration settings.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> or <paramref name="configuration"/> is <see langword="null"/>.
    /// </exception>
    public static IServiceCollection AddMetworkServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<PingServiceOptions>(
            configuration.GetSection(PingServiceOptions.SectionName));

        services.AddTransient<IPingService, PingService>();

        return services;
    }
}
