using Astrolabed.Ntp.Options;
using Astrolabed.Ntp.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Astrolabed.Ntp.Extensions;

/// <summary>
/// Extension methods for registering Network Time Protocol (NTP) server infrastructure services in an <see cref="IServiceCollection"/>.
/// </summary>
public static class NtpServiceCollectionExtensions
{
    /// <summary>
    /// Adds NTP server services, resolvers, handlers, and background hosted engines to the specified <see cref="IServiceCollection"/> using configuration binding.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configuration">The configuration section root containing NTP server options.</param>
    /// <returns>The modified <see cref="IServiceCollection"/> for fluent method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="configuration"/> is null.</exception>
    public static IServiceCollection AddNtpServer(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<NtpServerOptions>()
            .Bind(configuration.GetSection(NtpServerOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        AddCoreServices(services);

        return services;
    }

    /// <summary>
    /// Adds NTP server services, resolvers, handlers, and background hosted engines to the specified <see cref="IServiceCollection"/> using an action delegate.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configureOptions">The action delegate used to configure <see cref="NtpServerOptions"/>.</param>
    /// <returns>The modified <see cref="IServiceCollection"/> for fluent method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="configureOptions"/> is null.</exception>
    public static IServiceCollection AddNtpServer(this IServiceCollection services, Action<NtpServerOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.AddOptions<NtpServerOptions>()
            .Configure(configureOptions)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        AddCoreServices(services);

        return services;
    }

    private static void AddCoreServices(IServiceCollection services)
    {
        services.AddSingleton<LocalTimeResolver>();
        services.AddSingleton<UpstreamTimeResolver>();
        services.AddSingleton<ITimeResolver, CompositeTimeResolver>();

        services.AddSingleton<INtpServerHandler, NtpServerHandler>();
        services.AddHostedService<NtpServerEngine>();
    }
}
