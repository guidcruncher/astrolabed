using Astrolabed.Api.Options;

using Microsoft.Extensions.Options;

namespace Astrolabed.Api.Services;

/// <summary>
/// Core business logic implementation for Astrolabed.
/// </summary>
public sealed class AstrolabedService : IAstrolabedService
{
    private readonly ApiOptions _options;
    private readonly ILogger<AstrolabedService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AstrolabedService"/> class.
    /// </summary>
    /// <param name="options">The configured API options accessor.</param>
    /// <param name="logger">The application logging instance.</param>
    public AstrolabedService(
        IOptions<ApiOptions> options,
        ILogger<AstrolabedService> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public string GetSystemStatus()
    {
        _logger.LogInformation("Retrieving system status using endpoint: {Endpoint}", _options.ApiEndpoint);

        return $"Astrolabed API Operational | Target Endpoint: {_options.ApiEndpoint} | Timeout: {_options.TimeoutSeconds}s";
    }
}
