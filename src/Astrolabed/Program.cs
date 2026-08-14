using Astrolabed.Hosting;

using Microsoft.Extensions.Configuration;

namespace Astrolabed;

public class Program
{
    public static async Task Main(string[] args)
    {
        var host = Startup.BuildHost(args);

        // Increase threadpool minimums for high-QPS scenarios
        try
        {
            System.Threading.ThreadPool.SetMinThreads(Environment.ProcessorCount * 2, Environment.ProcessorCount * 2);
        }
        catch
        {
            // best-effort
        }

        await RuntimeLoader.LoadAsync(host);

        var serverOptions = host.Services.GetRequiredService<ServerOptions>();

        MetricsSidecar.StartIfEnabled(host, serverOptions, args);

        if (ServiceRegistration.SharedDnsCache == null)
        {
            throw new NullReferenceException("Shared DNS Cache is null");
        }

        ApiSidecar.StartIfEnabled(host, serverOptions, args, ServiceRegistration.SharedDnsCache);

        await host.RunAsync();
    }
}
