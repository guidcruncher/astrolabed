namespace Astrolabed.Data.Options;

/// <summary>
/// Defines supported relational database storage engines.
/// </summary>
public enum Provider
{
    /// <summary>
    /// Represents an embedded or file-based SQLite database.
    /// </summary>
    Sqlite,

    /// <summary>
    /// Represents a remote or local PostgreSQL database instance.
    /// </summary>
    PostgreSql
}

/// <summary>
/// Configuration options for setting up database connection behavior and runtime properties.
/// </summary>
public sealed class DatabaseOptions
{
    /// <summary>
    /// The configuration section key under application configuration files.
    /// </summary>
    public const string SectionName = "Database";

    /// <summary>
    /// Gets or sets the target relational database provider engine.
    /// </summary>
    /// <value>A <see cref="Provider"/> enum value. Defaults to <see cref="Provider.Sqlite"/>.</value>
    public Provider Provider { get; set; } = Provider.Sqlite;

    /// <summary>
    /// Gets or sets the connection string used to connect to the configured database provider.
    /// </summary>
    /// <value>A standard database provider connection string. Defaults to an empty string.</value>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the maximum execution timeout duration in seconds for database commands.
    /// </summary>
    /// <value>Timeout threshold in seconds. Defaults to <c>30</c>.</value>
    public int CommandTimeoutSeconds { get; set; } = 30;
}
