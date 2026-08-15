namespace Astrolabed;


public enum DatabaseProvider
{
    Sqlite,
    Postgres
}

/// <summary>
/// Options pattern configuration for SQLite storage context.
/// </summary>
public class DbOptions
{

    public const string SectionName = "DbOptions";

    public DatabaseProvider Provider { get; set; }

    public string ConnectionString { get; set; } = "Data Source=dns_events.db;";

}
