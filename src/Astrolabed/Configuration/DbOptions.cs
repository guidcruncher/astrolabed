namespace Astrolabed;

/// <summary>
/// Options pattern configuration for SQLite storage context.
/// </summary>
public class DbOptions
{

    /// <summary>
    /// SQLite connection string (e.g., "Data Source=dns_events.db;Cache=Shared").
    /// </summary>
    public string ConnectionString { get; set; } = "Data Source=dns_events.db;";

}
