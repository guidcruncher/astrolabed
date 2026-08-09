using System.Net;

using Astrolabed;
using Astrolabed.Ntp.Bootstrap;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Astrolabed.Ntp.Bootstrap;

public static class NtpServerServiceCollectionExtensions
{
    public static IServiceCollection AddNtpServer(
        this IServiceCollection services,
        IConfiguration config)
    {
        var server = config.Get<ServerOptions>() ?? new ServerOptions();
        var ntp = server.Ntp;

        if (!ntp.Enabled)
            return services; // NTP disabled — do nothing


        services.AddSingleton<NtpServerOptions>(ntp);

        if (ntp.Upstream.Enabled)
        {
            services.AddSingleton<INtpTimeSource, UpstreamNtpTimeSource>();
        }
        else
        {
            services.AddSingleton<INtpTimeSource, SystemTimeSource>();
        }

        services.AddSingleton<INtpRequestHandler, NtpRequestHandler>();

        // Runtime loader runs BEFORE the server starts
        services.AddHostedService<NtpRuntimeLoader>();

        // NTP server itself
        services.AddHostedService<NtpServerService>();

        return services;
    }
}
