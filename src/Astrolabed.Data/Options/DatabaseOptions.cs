namespace Astrolabed.Data.Options;

public enum Provider
{
    Sqlite,
    PostgreSql
}

/// <summary>
/// Configuration options for setting up database connection behavior.
/// </summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public Provider Provider { get; set; } = Provider.Sqlite;

    public string ConnectionString { get; set; } = string.Empty;

    public int CommandTimeoutSeconds { get; set; } = 30;
}
