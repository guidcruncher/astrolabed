using System.Text.Json;
using System.Text.Json.Nodes;

using Astrolabed;
using Astrolabed.Hosting;

using Microsoft.Extensions.Options;

namespace Astrolabed.Api.Services;

public sealed class AppConfigurationService : IAppConfigurationService
{
    private static readonly SemaphoreSlim FileLock = new(1, 1);
    private readonly IOptionsSnapshot<ServerOptions> _optionsSnapshot;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<AppConfigurationService> _logger;

    public AppConfigurationService(
        IOptionsSnapshot<ServerOptions> optionsSnapshot,
        IHostEnvironment environment,
        ILogger<AppConfigurationService> logger)
    {
        _optionsSnapshot = optionsSnapshot;
        _environment = environment;
        _logger = logger;
    }

    public ServerOptions GetConfiguration()
    {
        return _optionsSnapshot.Value;
    }

    public async Task UpdateConfigurationAsync(ServerOptions newConfig, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(newConfig);

        string settingsPath = HostBuilderFactory.ConfigurationFile;
        if (!File.Exists(settingsPath))
        {
            settingsPath = Path.Combine(_environment.ContentRootPath, "appsettings.json");
        }

        await FileLock.WaitAsync(cancellationToken);
        try
        {
            JsonNode rootNode;
            if (File.Exists(settingsPath))
            {
                string existingJson = await File.ReadAllTextAsync(settingsPath, cancellationToken);
                rootNode = JsonNode.Parse(existingJson) ?? new JsonObject();
            }
            else
            {
                rootNode = new JsonObject();
            }

            JsonNode updatedConfigNode = JsonSerializer.SerializeToNode(newConfig, new JsonSerializerOptions
            {
                WriteIndented = true
            }) ?? new JsonObject();

            if (updatedConfigNode is JsonObject configObject)
            {
                foreach (var property in configObject)
                {
                    rootNode[property.Key] = property.Value?.DeepClone();
                }
            }

            var serializerOptions = new JsonSerializerOptions { WriteIndented = true };
            string outputJson = JsonSerializer.Serialize(rootNode, serializerOptions);

            await File.WriteAllTextAsync(settingsPath, outputJson, cancellationToken);
            _logger.LogInformation("Server configuration updated successfully in {FilePath}", settingsPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update server configuration at {FilePath}", settingsPath);
            throw;
        }
        finally
        {
            FileLock.Release();
        }
    }
}
