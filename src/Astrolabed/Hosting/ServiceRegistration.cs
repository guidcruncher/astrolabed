using Astrolabed.Configuration;
using Astrolabed.Dhcp.Bootstrap;
using Astrolabed.Dns.Bootstrap;
using Astrolabed.Events.Bootstrap;
using Astrolabed.Metrics.Bootstrap;
using Astrolabed.Ntp.Bootstrap;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Astrolabed.Hosting;

public static class ServiceRegistration
{
    public static void Register(HostBuilderContext ctx, IServiceCollection services)
    {
        services.Configure<ServerOptions>(ctx.Configuration);
        services.AddSingleton(sp => sp.GetRequiredService<IOptionsMonitor<ServerOptions>>().CurrentValue);

        services.AddSingleton<ConfigurationWriter>();

        services.AddEventBus(ctx.Configuration);
        services.AddDnsForwarder(ctx.Configuration);
        services.AddDhcpServer(ctx.Configuration);
        services.AddNtpServer(ctx.Configuration);
        services.AddMetricServices(ctx.Configuration);
    }
}
