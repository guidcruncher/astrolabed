namespace Astrolabed.Utilities;

/// <summary>
/// Configuration options for pagination behavior across the application.
/// </summary>
public class PaginationOptions
{
    public const string SectionName = "Pagination";

    /// <summary>
    /// Gets or sets the default page size if none is specified in requests.
    /// </summary>
    public int DefaultPageSize { get; set; } = 20;

    /// <summary>
    /// Gets or sets the maximum allowable page size to prevent memory exhaustion attacks.
    /// </summary>
    public int MaxPageSize { get; set; } = 100;
}
