using System.Reflection;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Data.Services;

public sealed class EmbeddedSchemaProvider : ISchemaProvider
{
    private readonly string _resourceName = "DatabaseSchema";

    private readonly ILogger<EmbeddedSchemaProvider> _logger;

    public EmbeddedSchemaProvider(
        ILogger<EmbeddedSchemaProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
    }

    public async Task<string> GetSchemaSqlAsync(CancellationToken cancellationToken = default)
    {
        Assembly assembly = typeof(EmbeddedSchemaProvider).Assembly;

        _logger.LogDebug("Attempting to load embedded SQL schema resource '{_resourceName}'", _resourceName);

        using Stream? stream = assembly.GetManifestResourceStream(_resourceName);
        if (stream is null)
        {
            _logger.LogError("Embedded resource '{_resourceName}' was not found in assembly '{AssemblyName}'", _resourceName, assembly.FullName);
            throw new InvalidOperationException($"Embedded SQL script '{_resourceName}' could not be located.");
        }

        using var reader = new StreamReader(stream);
        string sql = await reader.ReadToEndAsync(cancellationToken);

        _logger.LogInformation("Successfully loaded embedded schema SQL ({ByteCount} bytes)", sql.Length);
        return sql;
    }
}
