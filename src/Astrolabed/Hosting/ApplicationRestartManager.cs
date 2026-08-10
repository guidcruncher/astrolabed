using System;
using System.Diagnostics;
using Microsoft.Extensions.Hosting;

namespace Astrolabed.Hosting;

public interface IApplicationRestartManager
{
    void RequestRestart(string[]? arguments = null);
}

public class ApplicationRestartManager : IApplicationRestartManager
{
    private readonly IHostApplicationLifetime _lifetime;

    public ApplicationRestartManager(IHostApplicationLifetime lifetime)
    {
        _lifetime = lifetime;
    }

    public void RequestRestart(string[]? arguments = null)
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

        _lifetime.ApplicationStopped.Register(() =>
        {
            Process.Start(startInfo);
        });

        // Initiates host teardown asynchronously across all hosted services
        _lifetime.StopApplication();
    }
}
