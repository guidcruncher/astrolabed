// File: DnsBenchmarkOptionsSetup.cs
namespace Astrolabed.Dns.Benchmarking.Options;

using System;
using System.IO;
using System.Text.Json;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Configures <see cref="DnsBenchmarkOptions"/> by loading server definitions from public-resolvers.json located in the application root path.
/// </summary>
public sealed class DnsBenchmarkOptionsSetup : IConfigureOptions<DnsBenchmarkOptions>
{
    private const string FileName = "public-resolvers.json";
    private readonly IHostEnvironment _environment;
    private readonly ILogger<DnsBenchmarkOptionsSetup> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DnsBenchmarkOptionsSetup"/> class.
    /// </summary>
    /// <param name="environment">The host environment providing access to the application content root path.</param>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown if any dependency is null.</exception>
    public DnsBenchmarkOptionsSetup(
        IHostEnvironment environment,
        ILogger<DnsBenchmarkOptionsSetup> logger)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(logger);

        _environment = environment;
        _logger = logger;
    }

    /// <inheritdoc />
    public void Configure(DnsBenchmarkOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        string filePath = Path.Combine(_environment.ContentRootPath, FileName);

        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Public DNS resolvers file was not found at expected path: {FilePath}", filePath);
            return;
        }

        try
        {
            string json = File.ReadAllText(filePath);
            JsonSerializerOptions serializerOptions = new()
            {
                PropertyNameCaseInsensitive = true
            };

            PublicResolversDocument? document = JsonSerializer.Deserialize<PublicResolversDocument>(json, serializerOptions);

            if (document?.PublicDnsServers is not null && document.PublicDnsServers.Count > 0)
            {
                options.Servers = document.PublicDnsServers;
                _logger.LogInformation(
                    "Successfully loaded {Count} DNS servers from {FileName}",
                    options.Servers.Count,
                    FileName);
            }
            else
            {
                _logger.LogWarning("Public DNS resolver file at {FilePath} was empty or contained no valid servers.", filePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read or deserialize DNS resolver file at {FilePath}", filePath);
        }
    }
}
