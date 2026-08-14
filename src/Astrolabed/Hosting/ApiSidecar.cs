using System.Net;

using Astrolabed.Api;
using Astrolabed.Api.Controllers;
using Astrolabed.Configuration;
using Astrolabed.Dhcp;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Hosting;

public static class ApiSidecar
{
    public static void StartIfEnabled(IHost mainHost, ServerOptions serverOptions, string[] args)
    {
        var logger = mainHost.Services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(ApiSidecar));

        if (!serverOptions.WebUI.Enabled)
        {
            logger.LogWarning("API sidecar not enabled.");
            return;
        }

        logger.LogInformation(
            "Starting API sidecar on http://{Address}:{Port}",
            serverOptions.WebUI.ListenAddress,
            serverOptions.WebUI.ListenPort);

        var lifetime = mainHost.Services.GetRequiredService<IHostApplicationLifetime>();
        var mainConfig = mainHost.Services.GetRequiredService<IConfiguration>();

        var apiHost = Host.CreateDefaultBuilder(args)
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            })
            .ConfigureServices(services =>
            {
                services.AddSingleton(mainConfig);
                services.AddSingleton(mainHost.Services.GetRequiredService<IDhcpLeaseReader>());
                services.Configure<DhcpOptions>(mainConfig.GetSection("Dhcp"));
                services.Configure<ServerOptions>(mainConfig);

                services.AddControllers()
                        .AddControllersFromNamespace<LeasesController>("Astrolabed.Api.Controllers");
            })
            .ConfigureWebHostDefaults(web =>
            {
                web.UseKestrel(options =>
                {
                    if (IPAddress.TryParse(serverOptions.WebUI.ListenAddress, out var ip))
                    {
                        options.Listen(ip, serverOptions.WebUI.ListenPort);
                    }
                    else
                    {
                        options.ListenAnyIP(serverOptions.WebUI.ListenPort);
                    }

                    logger.LogInformation(
                        "Kestrel bound API sidecar to {Address}:{Port}",
                        serverOptions.WebUI.ListenAddress,
                        serverOptions.WebUI.ListenPort);
                });

                web.Configure(app =>
                {
                    app.UseRouting();

                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapControllers();
                    });

                    logger.LogInformation("API sidecar controllers registered successfully.");
                });
            })
            .Build();

        _ = apiHost.RunAsync(lifetime.ApplicationStopping).ContinueWith(t =>
        {
            if (t.IsFaulted && t.Exception is not null)
            {
                logger.LogError(t.Exception, "API sidecar encountered an error during execution.");
            }
        });

        logger.LogInformation("API sidecar started successfully.");
    }
}

