using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Astrolabed.Events;

namespace Astrolabed.Ntp.Bootstrap;

public static class NtpServerServiceCollectionExtensions
{
    public static IServiceCollection AddNtpServer(
        this IServiceCollection services,
        IConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(config);

        var ntpSection = config.GetSection("Ntp");
        services.Configure<NtpServerOptions>(ntpSection);

        var ntpOptions = ntpSection.Get<NtpServerOptions>() ?? new NtpServerOptions();

        if (!ntpOptions.Enabled)
        {
            return services;
        }

        // Register core time source based on configuration
        if (ntpOptions.Upstream?.Enabled == true)
        {
            services.TryAddSingleton<INtpTimeSource, UpstreamNtpTimeSource>();
        }
        else
        {
            services.TryAddSingleton<INtpTimeSource, SystemTimeSource>();
        }

        // Register missing dependencies required by NtpServerService
        services.TryAddSingleton<INtpMetrics, NtpMetrics>();
        services.TryAddSingleton<INtpRequestHandler, NtpRequestHandler>();
        
        // Register hosted services
        services.AddHostedService<NtpRuntimeLoader>();
        services.AddHostedService<NtpServerService>();

        return services;
    }
}
