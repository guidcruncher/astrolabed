namespace Astrolabed.Main;

using Astrolabed.Api.Extensions;
using Astrolabed.Api.Options;
using Astrolabed.Core.Extensions;
using Astrolabed.Data.Extensions;
using Astrolabed.Dhcp.Extensions;
using Astrolabed.Dns.Benchmarking.Extensions;
using Astrolabed.Dns.Events;
using Astrolabed.Dns.Extensions;
using Astrolabed.EventBus.Events;
using Astrolabed.EventBus.Extensions;
using Astrolabed.Ntp.Extensions;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

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
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        // Logging configuration
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();

        // Configure Kestrel web server
        builder.WebHost.ConfigureKestrel((context, options) =>
        {
            options.AddServerHeader = false;

            ApiOptions? apiOptions = context.Configuration
                .GetSection(ApiOptions.SectionName)
                .Get<ApiOptions>();

            if (!string.IsNullOrWhiteSpace(apiOptions?.ApiEndpoint) &&
                Uri.TryCreate(apiOptions.ApiEndpoint, UriKind.Absolute, out Uri? uri))
            {
                // Bind Kestrel only to Scheme + Host + Port (e.g., http://localhost:5000)
                string listenUrl = $"{uri.Scheme}://{uri.Host}:{uri.Port}";
                builder.WebHost.UseUrls(listenUrl);
            }
        });

        // 1. Unified Data Layer & Persistence Setup
        builder.Services.AddAstrolabedData(builder.Configuration);
        builder.Services.AddMacVendorLookup(builder.Configuration);

        // 2. Event Broker Setup
        builder.Services.AddRootEventBroker(builder.Configuration);

        // 3. Event Listeners
        builder.Services.AddEventListener<DnsResponseEvent, DnsResponseListener>();

        // 4. Protocol Servers & Network Engines
        builder.Services.AddNtpServer(builder.Configuration);
        builder.Services.AddDhcpServer(builder.Configuration);
        builder.Services.AddDnsServer(builder.Configuration);

        // 5. DNS Benchmarker
        builder.Services.AddDnsBenchmarker(builder.Configuration);

        // 6. API Module Registration
        builder.Services.AddApi(builder.Configuration);

        WebApplication app = builder.Build();

        // Configure HTTP middleware pipeline
        app.UseSecurityHeaders();

        app.UseRouting();

        // Enable OpenAPI endpoints and Scalar UI in development
        app.UseAstrolabedOpenApi(app.Configuration);

        app.MapControllers();
        app.UseSpaHosting();

        // Perform explicit database initialization using WebApplication as IHost
        await app.InitializeDatabaseAsync().ConfigureAwait(false);

        // Run application host
        await app.RunAsync().ConfigureAwait(false);
    }
}
