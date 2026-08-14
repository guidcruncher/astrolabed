using Astrolabed.Api.Services;
using Astrolabed.Dhcp;
using Astrolabed.Dns;
using Astrolabed.Ntp;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Astrolabed.Api;

public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IHost mainHost, IConfiguration configuration)
    {
        // 1. Options Binding
        services.Configure<NtpServerOptions>(configuration.GetSection("Ntp"));
        services.Configure<DhcpOptions>(configuration.GetSection("Dhcp"));
	services.Configure<DnsForwarderOptions>(configuration.GetSection("Dns"));

        // 2. DHCP Services
        services.AddSingleton(mainHost.Services.GetRequiredService<IDhcpLeaseReader>());

	// 3. DNS Services
	services.AddTransient<IDnsService, DnsService>();

        // 4. NTP Services
        services.AddSingleton(mainHost.Services.GetRequiredService<INtpRequestHandler>());
        services.AddTransient<INtpService, NtpService>();

        return services;
    }
}
