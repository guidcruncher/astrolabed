using System;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Astrolabed.Hosting;

public static class HostBuilderFactory
{
    public static string ConfigurationFile { get; set; } = "appsettings.json";

    public static IHost Build(string[] args, IConfiguration cmd)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((ctx, config) =>
            {
                var env = cmd["DOTNET_ENVIRONMENT"]
                          ?? ctx.HostingEnvironment.EnvironmentName;

                if (cmd["ConfigPath"] is string customConfig && !string.IsNullOrWhiteSpace(customConfig))
                {
                    HostBuilderFactory.ConfigurationFile = customConfig;
                    config.AddJsonFile(customConfig, optional: false, reloadOnChange: true);
                }
                else
                {
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    config.AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true);

                    if (env.Equals("Docker", StringComparison.OrdinalIgnoreCase))
                    {
                        config.AddJsonFile("appsettings.Docker.json", optional: true, reloadOnChange: true);
                    }
                }

                config.AddEnvironmentVariables();
                config.AddConfiguration(cmd);
            })
            .ConfigureLogging((ctx, logging) =>
            {
                logging.ClearProviders();
                logging.AddConsole();

                var level = ctx.Configuration["Logging:Level"] ?? "Information";
                if (Enum.TryParse<LogLevel>(level, ignoreCase: true, out var parsedLevel))
                {
                    logging.SetMinimumLevel(parsedLevel);
                }
                else
                {
                    logging.SetMinimumLevel(LogLevel.Information);
                }
            })
            .ConfigureServices((ctx, services) =>
            {
                services.AddSingleton<IApplicationRestartManager, ApplicationRestartManager>();
                ServiceRegistration.Register(ctx, services);
            })
            .Build();
    }
}
