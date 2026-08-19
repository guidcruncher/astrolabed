using Astrolabed.Api.Services;
using Astrolabed.Data;
using Astrolabed.Dhcp;
using Astrolabed.Dns;
using Astrolabed.Dns.Core;
using Astrolabed.Dns.Filtering;
using Astrolabed.Dns.RuleEngine;
using Astrolabed.Ntp;
using Astrolabed.Utilities;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Astrolabed.Api;

public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IHost mainHost, IConfiguration configuration, IDnsCache sharedCache)
    {
        // 1. Options Binding
        services.Configure<NtpServerOptions>(configuration.GetSection(NtpServerOptions.SectionName));
        services.Configure<DhcpOptions>(configuration.GetSection(DhcpOptions.SectionName));
        services.Configure<DnsForwarderOptions>(configuration.GetSection(DnsForwarderOptions.SectionName));
        services.Configure<CrossPlatformScannerOptions>(configuration.GetSection(CrossPlatformScannerOptions.SectionName));

        services.AddTransient<IAppConfigurationService, AppConfigurationService>();
        services.AddTransient<ICrossPlatformLanScannerService, CrossPlatformLanScannerService>();

        services.AddDataServices(configuration);

        // 2. DHCP Services
        services.AddSingleton(mainHost.Services.GetRequiredService<IDhcpLeaseReader>());

        services.AddSingleton<IDnsCache>(sharedCache);
	services.AddSingleton<DnsForwarderService, DnsForwarderService>();
        services.AddSingleton(mainHost.Services.GetRequiredService<IClientNameResolver>());
        services.AddSingleton(mainHost.Services.GetRequiredService<Astrolabed.Dns.RuleEngine.RuleEngine>());
        services.AddSingleton(mainHost.Services.GetRequiredService<IHostsFileSource>());

        services.AddTransient<IDnsService, DnsService>();

        // 4. NTP Services
        services.AddSingleton(mainHost.Services.GetRequiredService<INtpRequestHandler>());
        services.AddTransient<INtpService, NtpService>();

        return services;
    }
}
