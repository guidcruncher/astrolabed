// File: src/Astrolabed.Main/Program.cs
using Astrolabed.Data.Extensions;
using Astrolabed.Dhcp.Extensions;
using Astrolabed.Dns.Events;
using Astrolabed.Dns.Extensions;
using Astrolabed.EventBus.Events;
using Astrolabed.EventBus.Extensions;
using Astrolabed.Ntp.Extensions;

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
            })
            .Build();

        // Perform explicit database initialization after DI container build and before engine execution
        await host.InitializeDatabaseAsync().ConfigureAwait(false);

        // Run application host asynchronously until process termination request
        await host.RunAsync().ConfigureAwait(false);
    }
}

