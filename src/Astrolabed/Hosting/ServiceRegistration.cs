using Astrolabed.Dhcp.Bootstrap;
using Astrolabed.Dns.Bootstrap;
using Astrolabed.Events.Bootstrap;
using Astrolabed.Metrics.Bootstrap;
using Astrolabed.Ntp.Bootstrap;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Astrolabed.Hosting;

public static class ServiceRegistration
{
    public static void Register(HostBuilderContext ctx, IServiceCollection services)
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
        });

        var logger = loggerFactory.CreateLogger("ServiceRegistration");

        logger.LogInformation("Loading ServerOptions…");

        var serverOptions = ctx.Configuration.Get<ServerOptions>() ?? new ServerOptions();
        services.AddSingleton<ServerOptions>(serverOptions);


        logger.LogInformation("Registering EventBus…");
        services.AddEventBus(ctx.Configuration);

        logger.LogInformation("Registering DNS Forwarder…");
        services.AddAstrolabed(ctx.Configuration);

        logger.LogInformation("Registering DHCP Server…");
        services.AddDhcpServer(ctx.Configuration);

        logger.LogInformation("Registering NTP Server…");
        services.AddNtpServer(ctx.Configuration);

        logger.LogInformation("Registering Metrics services…");
        services.AddMetricServices(ctx.Configuration);

        logger.LogInformation("All services registered successfully.");
    }
}
