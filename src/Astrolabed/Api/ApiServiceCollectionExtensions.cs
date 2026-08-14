using Astrolabed;
using Astrolabed.Events;
using Astrolabed.Ntp;
using Astrolabed.Dhcp;
using Astrolabed.Dns;
using Astrolabed.Api.Services;
using Astrolabed.Events.Bootstrap;
using Astrolabed.Dhcp.Bootstrap;
using Astrolabed.Dns.Bootstrap;
using Astrolabed.Ntp.Bootstrap;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Astrolabed.Api;

public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Options Binding
        services.Configure<NtpServerOptions>(configuration.GetSection("Ntp"));
        services.Configure<DhcpOptions>(configuration.GetSection("Dhcp"));

	// 3. Event bus
	services.AddEventBus(configuration);

        // 4. DHCP Services
        services.AddDhcpServer(configuration);

        //5. NTP Services
	services.AddNtpServer(configuration);
        services.AddTransient<INtpService, NtpService>();

        return services;
    }
}
