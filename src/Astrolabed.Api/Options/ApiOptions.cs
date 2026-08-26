namespace Astrolabed.Api.Options;

/// <summary>
/// Configuration options for the Astrolabed API service module.
/// </summary>
public sealed class ApiOptions
{
    /// <summary>
    /// The default configuration section key in appsettings.json.
    /// </summary>
    public const string SectionName = "Api";

    /// <summary>
    /// Gets or sets the base external API endpoint URL.
    /// </summary>
    public string ApiEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the request timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets a value indicating whether detailed error responses are enabled.
    /// </summary>
    public bool EnableDetailedErrors { get; set; } = false;
}
