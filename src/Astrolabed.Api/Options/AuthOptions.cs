namespace Astrolabed.Api.Options;

/// <summary>
/// Represents configuration settings for authentication and cookie management.
/// </summary>
public sealed class AuthOptions
{
    /// <summary>
    /// The configuration section key in appsettings.json.
    /// </summary>
    public const string SectionName = "AuthenticationSettings";

    /// <summary>
    /// Gets or sets the name of the authentication cookie.
    /// </summary>
    public string CookieName { get; set; } = ".Aetrolabed.Auth";

    /// <summary>
    /// Gets or sets the expiration period for the cookie in days.
    /// </summary>
    public int ExpireDays { get; set; } = 7;

    /// <summary>
    /// Gets or sets a value indicating whether a new cookie with an updated expiration time 
    /// should be issued dynamically with each request.
    /// </summary>
    public bool SlidingExpiration { get; set; } = true;
}
