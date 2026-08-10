using System.Net;

using Astrolabed;
using Astrolabed.Dhcp;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Astrolabed.Dhcp.Bootstrap;

public static class DhcpServiceCollectionExtensions
{
    public static IServiceCollection AddDhcpServer(this IServiceCollection services, IConfiguration config)
    {
        var server = config.Get<ServerOptions>() ?? new ServerOptions();
        var dhcp = server.Dhcp;

        if (!dhcp.Enabled)
            return services; // DHCP disabled — do nothing

        using var serviceProvider = services.BuildServiceProvider();
        var _logger = serviceProvider.GetRequiredService<ILogger>();

        if (!IPAddress.TryParse(dhcp.ListenAddress, out IPAddress? address))
        {
            _logger.LogCritical("Invalid IP Address in DHCP ListenAddress. Cannot initialise DHCP Service");
            return services;
        }

        // DHCP engine + lease store
        services.AddSingleton<DhcpOptions>(dhcp);

        services.AddSingleton<IDhcpLeaseStore>(sp =>
            new JsonDhcpLeaseStore(dhcp.LeaseStorePath));

        services.AddSingleton<IUdpTransport>(sp =>
        {
            var opts = sp.GetRequiredService<ServerOptions>();
            var logger = sp.GetRequiredService<ILogger<UdpTransport>>();
            return new UdpTransport(
                IPAddress.Parse(opts.Dhcp.ListenAddress),
                opts.Dhcp.ListenPort, logger);
        });

        services.AddSingleton<IDhcpLeaseStore, InMemoryDhcpLeaseStore>();
        services.AddSingleton<DhcpLeaseEngine>();
        services.AddSingleton<DhcpServerEngine>();

        // Hosted DHCP server
        services.AddHostedService<DhcpHostedService>();

        return services;
    }
}
