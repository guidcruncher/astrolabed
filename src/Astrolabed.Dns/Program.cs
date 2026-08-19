// File: src/Astrolabed.Dns/Program.cs
using System.Threading.Tasks;

using Astrolabed.Dns.Extensions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Astrolabed.Dns;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateDefaultBuilder(args)
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
                // Clean Dependency Injection Binding
                services.AddAstrolabedDnsEngine(hostContext.Configuration);
            });

        await builder.Build().RunAsync().ConfigureAwait(false);
    }
}
