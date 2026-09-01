namespace Astrolabed.Data.Models;

/// <summary>
/// Represents the database record layout stored in the 'dns_lists' table.
/// </summary>
public sealed class DnsListEntity
{
    /// <summary>
    /// Gets or sets the unique integer primary key identifier for the DNS list.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the display name or label for the DNS list.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file system or URL resource path pointing to the DNS list source file.
    /// </summary>
    public string Path { get; set; } = string.Empty;
}
