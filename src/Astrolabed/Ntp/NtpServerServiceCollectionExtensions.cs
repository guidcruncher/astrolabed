using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        var ntpOptions = ntpSection.Get<NtpServerOptions>() ?? new NtpServerOptions();

        if (!ntpOptions.Enabled)
        {
            return services;
        }

        services.Configure<NtpServerOptions>(ntpSection);

        if (ntpOptions.Upstream.Enabled)
        {
            services.AddSingleton<INtpTimeSource, UpstreamNtpTimeSource>();
        }
        else
        {
            services.AddSingleton<INtpTimeSource, SystemTimeSource>();
        }

        services.AddSingleton<INtpRequestHandler, NtpRequestHandler>();
        services.AddHostedService<NtpRuntimeLoader>();
        services.AddHostedService<NtpServerService>();

        return services;
    }
}
