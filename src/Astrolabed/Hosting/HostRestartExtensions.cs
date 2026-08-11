using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Astrolabed.Hosting;

public static class HostRestartExtensions
{
    /// <summary>
    /// Gracefully stops the running host and spawns a new process instance upon complete shutdown.
    /// </summary>
    public static async Task RestartAsync(this IHost host, string[]? arguments = null, CancellationToken cancellationToken = default)
    {
        var startInfo = ApplicationRestartManager.CreateRestartStartInfo(arguments);
        var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

        lifetime.ApplicationStopped.Register(() =>
        {
            Process.Start(startInfo);
        });

        await host.StopAsync(cancellationToken);
    }
}
