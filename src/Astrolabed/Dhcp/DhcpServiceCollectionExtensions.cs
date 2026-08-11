using System.Net;

using Astrolabed;
using Astrolabed.Dhcp;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Astrolabed.Dhcp.Bootstrap;

public static class DhcpServiceCollectionExtensions
{
    public static IServiceCollection AddDhcpServer(this IServiceCollection services, IConfiguration config)
    {
        var server = config.Get<ServerOptions>() ?? new ServerOptions();
        var dhcp = server.Dhcp;

        if (!dhcp.Enabled)
            return services;

        if (!IPAddress.TryParse(dhcp.ListenAddress, out _))
        {
            return services;
        }

        services.AddSingleton<DhcpOptions>(dhcp);

        services.AddSingleton<IDhcpLeaseStore>(sp =>
            new JsonDhcpLeaseStore(dhcp.LeaseStorePath));

        services.AddSingleton<IUdpTransport>(sp =>
        {
            var opts = sp.GetRequiredService<ServerOptions>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<UdpTransport>>();
            return new UdpTransport(
                IPAddress.Parse(opts.Dhcp.ListenAddress),
                opts.Dhcp.ListenPort, logger);
        });

        services.AddSingleton<DhcpLeaseEngine>();
        services.AddSingleton<DhcpServerEngine>();

        services.AddHostedService<DhcpHostedService>();

        return services;
    }
}
