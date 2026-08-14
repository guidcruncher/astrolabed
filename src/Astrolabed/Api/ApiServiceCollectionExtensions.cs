using Astrolabed.Ntp;
using Astrolabed.Events;
using Astrolabed.Dhcp;
using Astrolabed.Dns;
using Astrolabed;
using Astrolabed.Api.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Astrolabed.Api;

public static class NtpServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Options Binding
        services.Configure<NtpServerOptions>(configuration.GetSection("Ntp"));
        services.Configure<DhcpOptions>(configuration.GetSection("Dhcp"));

        // 2. DHCP Services
        services.AddSingleton(services.GetRequiredService<IDhcpLeaseReader>());

        // 2. Core NTP Handler & Metrics Dependencies (Existing)
        services.AddSingleton<INtpMetrics, NtpMetrics>();
        services.AddTransient<INtpRequestHandler, NtpRequestHandler>();

        // 3. NTP Background Listener Service (Existing)
        services.AddHostedService<NtpServerService>();

        // 4. NEW: Register the INtpService for API / internal queries
        services.AddTransient<INtpService, NtpService>();

        return services;
    }
}
