using System.Net;

using Astrolabed.Metrics;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Hosting;

public static class MetricsSidecar
{
    public static void StartIfEnabled(IHost mainHost, ServerOptions serverOptions, string[] args)
    {
        var logger = mainHost.Services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(MetricsSidecar));

        if (serverOptions.Metrics.StorageEngine != "prometheus")
        {
            logger.LogInformation("Metrics sidecar disabled (StorageEngine != prometheus).");
            return;
        }

        logger.LogInformation(
            "Starting Prometheus metrics sidecar on http://{Address}:{Port}{Location}",
            serverOptions.Metrics.ListenAddress,
            serverOptions.Metrics.ListenPort,
            serverOptions.Metrics.Location);

        var lifetime = mainHost.Services.GetRequiredService<IHostApplicationLifetime>();
        var mainConfig = mainHost.Services.GetRequiredService<IConfiguration>();

        var metricsHost = Host.CreateDefaultBuilder(args)
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            })
            .ConfigureServices(services =>
            {
                services.AddSingleton(mainConfig);
                services.Configure<ServerOptions>(mainConfig);
                services.AddSingleton(sp => mainHost.Services.GetRequiredService<MetricsRegistry>());
            })
            .ConfigureWebHostDefaults(web =>
            {
                web.UseKestrel(options =>
                {
                    if (IPAddress.TryParse(serverOptions.Metrics.ListenAddress, out var ip))
                    {
                        options.Listen(ip, serverOptions.Metrics.ListenPort);
                    }
                    else
                    {
                        options.ListenAnyIP(serverOptions.Metrics.ListenPort);
                    }

                    logger.LogInformation(
                        "Kestrel bound to {Address}:{Port}",
                        serverOptions.Metrics.ListenAddress,
                        serverOptions.Metrics.ListenPort);
                });

                web.Configure(app =>
                {
                    app.UseRouting();

                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet(serverOptions.Metrics.Location, async context =>
                        {
                            var registry = context.RequestServices.GetRequiredService<MetricsRegistry>();
                            var text = registry.RenderPrometheus();

                            context.Response.ContentType = "text/plain; version=0.0.4";

                            logger.LogDebug("Metrics scraped from {RemoteIp}", context.Connection.RemoteIpAddress);

                            await context.Response.WriteAsync(text);
                        });
                    });

                    logger.LogInformation("Metrics endpoint registered at {Location}", serverOptions.Metrics.Location);
                });
            })
            .Build();

        _ = metricsHost.RunAsync(lifetime.ApplicationStopping).ContinueWith(t =>
        {
            if (t.IsFaulted && t.Exception is not null)
            {
                logger.LogError(t.Exception, "Metrics sidecar encountered an error during execution.");
            }
        });

        logger.LogInformation("Metrics sidecar started successfully.");
    }
}
