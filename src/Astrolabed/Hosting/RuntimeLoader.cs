using System.Threading.Tasks;

using Astrolabed.Dhcp.Bootstrap;
using Astrolabed.Dns.Bootstrap;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Astrolabed.Hosting;

public static class RuntimeLoader
{
    public static async Task LoadAsync(IHost host)
    {
        var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(RuntimeLoader));

        logger.LogInformation("Starting runtime loader...");

        using var scope = host.Services.CreateScope();
        var provider = scope.ServiceProvider;

        logger.LogInformation("Loading DNS runtime...");
        var dnsLoader = provider.GetRequiredService<IDnsForwarderRuntimeLoader>();
        await dnsLoader.LoadAsync();
        logger.LogInformation("DNS runtime loaded.");

        var dhcpLoader = provider.GetService<IDhcpRuntimeLoader>();
        if (dhcpLoader is not null)
        {
            logger.LogInformation("Loading DHCP runtime...");
            await dhcpLoader.LoadAsync();
            logger.LogInformation("DHCP runtime loaded.");
        }
        else
        {
            logger.LogWarning("DHCP is disabled; skipping DHCP runtime loader.");
        }

        logger.LogInformation("Runtime loader completed.");
    }
}
