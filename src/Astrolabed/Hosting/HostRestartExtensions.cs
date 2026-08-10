using System;
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
        string? processPath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(processPath))
        {
            throw new InvalidOperationException("Failed to resolve current executable process path.");
        }

        var startInfo = new ProcessStartInfo(processPath)
        {
            UseShellExecute = false,
            WorkingDirectory = AppContext.BaseDirectory
        };

        if (arguments is not null)
        {
            foreach (string arg in arguments)
            {
                startInfo.ArgumentList.Add(arg);
            }
        }
        else
        {
            string[] currentArgs = Environment.GetCommandLineArgs();
            for (int i = 1; i < currentArgs.Length; i++)
            {
                startInfo.ArgumentList.Add(currentArgs[i]);
            }
        }

        var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

        // Schedule process restart strictly after the host finished running shutdown delegates
        lifetime.ApplicationStopped.Register(() =>
        {
            Process.Start(startInfo);
        });

        await host.StopAsync(cancellationToken);
    }
}
