using System;
using System.Net;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Dhcp.Bootstrap;

public static class DhcpServiceCollectionExtensions
{
    public static IServiceCollection AddDhcpServer(
        this IServiceCollection services,
        IConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(config);

        var dhcpSection = config.GetSection(DhcpOptions.SectionName);
        services.Configure<DhcpOptions>(dhcpSection);

        var dhcpOptions = dhcpSection.Get<DhcpOptions>() ?? new DhcpOptions();

        if (!dhcpOptions.Enabled)
        {
            return services;
        }

        services.TryAddSingleton<ICidrPoolAllocator, CidrPoolAllocator>();
        services.TryAddSingleton<IArpConflictDetector, ArpConflictDetector>();
        services.TryAddSingleton<IDhcpLeaseEngine, DhcpLeaseEngine>();

        services.TryAddSingleton<DhcpServerEngine>();
        services.TryAddSingleton<IDhcpServerEngine>(sp => sp.GetRequiredService<DhcpServerEngine>());

        services.TryAddSingleton<IDhcpLeaseStore>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<DhcpOptions>>().Value;
            return new JsonDhcpLeaseStore(options.LeaseStorePath);
        });

        services.TryAddSingleton<IUdpTransport>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<DhcpOptions>>().Value;
            var logger = sp.GetRequiredService<ILogger<UdpTransport>>();
            return new UdpTransport(
                IPAddress.Parse(options.ListenAddress),
                options.ListenPort,
                logger);
        });

        services.TryAddSingleton<IDhcpRuntimeLoader, DhcpRuntimeLoader>();
        services.AddHostedService<DhcpHostedService>();

        return services;
    }
}
