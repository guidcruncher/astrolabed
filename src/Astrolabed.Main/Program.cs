// File: src/Astrolabed.Main/Program.cs
using Astrolabed.Data.Extensions;
using Astrolabed.Dns.Events;
using Astrolabed.Dns.Extensions;
using Astrolabed.EventBus;
using Astrolabed.EventBus.Events;
using Astrolabed.EventBus.Extensions;
using Astrolabed.Ntp.Extensions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Astrolabed.Main;

public static class Program
{
    public static async Task Main(string[] args)
    {
        using var rootHost = Host.CreateDefaultBuilder(args)
            .ConfigureServices(async (context, services) =>
            {
                services.AddDatabasePersistenceServices(context.Configuration);
                services.AddDatabaseInitializer();
                services.AddRootEventBroker(context.Configuration);
                await services.InitializeDatabase();
            })
            .Build();

        var centralBroker = rootHost.Services.GetRequiredService<IInProcEventBroker>();

        using var dnsHost = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                config.AddEnvironmentVariables();
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
            })
            .ConfigureServices((hostContext, services) =>
            {
                // Data Layer
                services.AddDatabasePersistenceServices(hostContext.Configuration);

                // Event Bus
                services.AddSubHostEventBus(centralBroker);
                services.AddEventListener<DnsResponseEvent, DnsResponseListener>();

                // NTP Services
                services.AddNtpServer(hostContext.Configuration);

                // DNS Services
                services.AddAstrolabedDnsEngine(hostContext.Configuration);
            }).Build();

        await rootHost.StartAsync();
        await dnsHost.RunAsync().ConfigureAwait(false);

        await dnsHost.StopAsync();
        await rootHost.StopAsync();

    }
}
