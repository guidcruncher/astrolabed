using System.Reflection;

using Microsoft.Extensions.Logging;

namespace Astrolabed.Data.Services;

/// <summary>
/// Provides access to database schema SQL scripts embedded directly within assembly manifest resources.
/// </summary>
public sealed partial class EmbeddedSchemaProvider : ISchemaProvider
{
    /// <summary>
    /// The fully qualified embedded resource name for the database schema SQL script.
    /// </summary>
    private readonly string _resourceName = "Astrolabed.Data.Resources.DatabaseSchema.sql";

    /// <summary>
    /// Structured logger instance for diagnostics and operational logging.
    /// </summary>
    private readonly ILogger<EmbeddedSchemaProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmbeddedSchemaProvider"/> class with logger dependencies.
    /// </summary>
    /// <param name="logger">Structured logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is <c>null</c>.</exception>
    public EmbeddedSchemaProvider(
        ILogger<EmbeddedSchemaProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> GetSchemaSqlAsync(CancellationToken cancellationToken = default)
    {
        Assembly assembly = typeof(EmbeddedSchemaProvider).Assembly;

        LogAttemptingToLoadSchema(_logger, _resourceName);

        using Stream? stream = assembly.GetManifestResourceStream(_resourceName);
        if (stream is null)
        {
            LogEmbeddedResourceNotFound(_logger, _resourceName, assembly.FullName ?? assembly.GetName().Name ?? "Unknown");
            throw new FileNotFoundException($"Embedded SQL script '{_resourceName}' could not be located in assembly.", _resourceName);
        }

        using var reader = new StreamReader(stream);
        string sql = await reader.ReadToEndAsync(cancellationToken);

        LogSchemaLoadedSuccessfully(_logger, sql.Length);
        return sql;
    }

    [LoggerMessage(
        EventId = 701,
        Level = LogLevel.Debug,
        Message = "Attempting to load embedded SQL schema resource '{ResourceName}'")]
    private static partial void LogAttemptingToLoadSchema(ILogger logger, string resourceName);

    [LoggerMessage(
        EventId = 702,
        Level = LogLevel.Error,
        Message = "Embedded resource '{ResourceName}' was not found in assembly '{AssemblyName}'")]
    private static partial void LogEmbeddedResourceNotFound(ILogger logger, string resourceName, string assemblyName);

    [LoggerMessage(
        EventId = 703,
        Level = LogLevel.Information,
        Message = "Successfully loaded embedded schema SQL ({CharacterCount} characters)")]
    private static partial void LogSchemaLoadedSuccessfully(ILogger logger, int characterCount);
}
