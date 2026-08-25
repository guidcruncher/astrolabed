using Astrolabed.Api.Extensions;
using Astrolabed.Data.Extensions;
using Astrolabed.Dhcp.Extensions;
using Astrolabed.Dns.Events;
using Astrolabed.Dns.Extensions;
using Astrolabed.EventBus.Events;
using Astrolabed.EventBus.Extensions;
using Astrolabed.Ntp.Extensions;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Astrolabed.Main;

/// <summary>
/// Application entry point and bootstrapping host builder for Astrolabed services.
/// </summary>
public static class Program
{
    /// <summary>
    /// Configures and runs the unified Astrolabed application host.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    public static async Task Main(string[] args)
    {
        IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
            })
            .ConfigureServices((context, services) =>
            {
                // 1. Unified Data Layer & Persistence Setup
                services.AddAstrolabedData(context.Configuration);

                // 2. Event Broker Setup
                services.AddRootEventBroker(context.Configuration);

                // 3. Event Listeners
                services.AddEventListener<DnsResponseEvent, DnsResponseListener>();

                // 4. Protocol Servers & Network Engines
                services.AddNtpServer(context.Configuration);
                services.AddDhcpServer(context.Configuration);
                services.AddDnsServer(context.Configuration);

                // 5. API Module Registration
                services.AddApi(context.Configuration);
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        // Map controllers here on the web host endpoint route builder
                        endpoints.MapControllers();
                    });
                });
            })
            .Build();

        // Perform explicit database initialization
        await host.InitializeDatabaseAsync().ConfigureAwait(false);

        // Run application host
        await host.RunAsync().ConfigureAwait(false);
    }
}
