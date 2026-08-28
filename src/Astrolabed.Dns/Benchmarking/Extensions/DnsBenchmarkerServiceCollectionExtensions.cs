// File: DnsBenchmarkerServiceCollectionExtensions.cs
namespace Astrolabed.Dns.Benchmarking.Extensions;

using System;

using Astrolabed.Dns.Benchmarking;
using Astrolabed.Dns.Benchmarking.Options;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering DNS benchmarking services with the dependency injection container.
/// </summary>
public static class DnsBenchmarkerServiceCollectionExtensions
{
    /// <summary>
    /// Registers the DNS benchmarking service, binding options from configuration and loading public-resolvers.json from the application root.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration root.</param>
    /// <returns>The modified service collection for chaining.</returns>
    public static IServiceCollection AddDnsBenchmarker(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<DnsBenchmarkOptions>(configuration.GetSection(DnsBenchmarkOptions.SectionName));
        services.ConfigureOptions<DnsBenchmarkOptionsSetup>();
        services.AddSingleton<IDnsBenchmarker, DnsBenchmarker>();

        return services;
    }

    /// <summary>
    /// Registers the DNS benchmarking service, using inline options configuration and loading public-resolvers.json from the application root.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">The action used to configure options.</param>
    /// <returns>The modified service collection for chaining.</returns>
    public static IServiceCollection AddDnsBenchmarker(
        this IServiceCollection services,
        Action<DnsBenchmarkOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.Configure(configureOptions);
        services.ConfigureOptions<DnsBenchmarkOptionsSetup>();
        services.AddSingleton<IDnsBenchmarker, DnsBenchmarker>();

        return services;
    }
}
