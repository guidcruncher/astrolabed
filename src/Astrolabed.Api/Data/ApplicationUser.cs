using Microsoft.AspNetCore.Identity;

namespace Astrolabed.Api.Data;

/// <summary>
/// Represents an application user within the Identity framework.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// Gets or sets the display name shown in the UI.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the user account was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the UTC timestamp when the user last logged in.
    /// </summary>
    public DateTimeOffset? LastLoginAt { get; set; }
}
