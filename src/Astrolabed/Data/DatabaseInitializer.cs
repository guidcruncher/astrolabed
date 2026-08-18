using System.Data;

using Astrolabed;

using Dapper;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Npgsql;

namespace Astrolabed.Data;

/// <summary>
/// Provider‑aware database initializer supporting SQLite and PostgreSQL.
/// </summary>
public class DatabaseBuilder : IDatabaseInitializer
{
    private readonly IDbConnectionFactory _factory;
    private readonly DbOptions _options;
    private readonly ILogger<DatabaseBuilder> _logger;
    private static bool _initialized = false;
    private static readonly object _lock = new();

    public DatabaseBuilder(
        IDbConnectionFactory factory,
        IOptions<DbOptions> dbOptions,
        ILogger<DatabaseBuilder> logger)
    {
        _factory = factory;
        _options = dbOptions.Value;
        _logger = logger;
    }

    public void Initialize()
    {
        lock (_lock)
        {
            if (_initialized)
                return;

            // run schema creation here
            InitializeSchema();

            _initialized = true;
        }
    }

    private void InitializeSchema()
    {
        using var conn = _factory.Create();

        switch (_options.Provider)
        {
            case DatabaseProvider.Sqlite:
                InitializeSqlite(conn);
                break;

            case DatabaseProvider.Postgres:
                InitializePostgres(conn);
                break;

            default:
                throw new NotSupportedException("Unknown database provider");
        }
    }

    // ----------------------------------------------------------------------
    // SQLite schema + WAL mode
    // ----------------------------------------------------------------------
    private void InitializeSqlite(IDbConnection conn)
    {
        // WAL + performance pragmas
        conn.Execute("""
            PRAGMA journal_mode = WAL;
            PRAGMA synchronous = NORMAL;
            PRAGMA foreign_keys = ON;
        """);

        // Schema + indexes
        conn.Execute("""
            CREATE TABLE IF NOT EXISTS dns_response_events (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Timestamp TEXT NOT NULL,
                TimestampEpoch BIGINT NOT NULL,
                ClientIp TEXT NOT NULL,
                ClientName TEXT NULL,
                QueryName TEXT NOT NULL,
                QueryType TEXT NOT NULL,
                Status TEXT NOT NULL,
                ResponseIp TEXT NULL,
                IsBlocked INTEGER NOT NULL DEFAULT 0
            );
 
            CREATE INDEX IF NOT EXISTS idx_dns_response_events_timestamp ON dns_response_events(Timestamp);
            CREATE INDEX IF NOT EXISTS idx_dns_response_events_timestampepoch ON dns_response_events(TimestampEpoch);
            CREATE INDEX IF NOT EXISTS idx_dns_response_events_clientip ON dns_response_events(ClientIp);
            CREATE INDEX IF NOT EXISTS idx_dns_response_events_queryname ON dns_response_events(QueryName);
            CREATE INDEX IF NOT EXISTS idx_dns_response_events_status ON dns_response_events(Status);
        """);

        _logger.LogInformation("SQLite schema initialized.");
    }

    // ----------------------------------------------------------------------
    // PostgreSQL schema
    // ----------------------------------------------------------------------
    private void InitializePostgres(IDbConnection conn)
    {
        conn.Execute("""
            CREATE TABLE IF NOT EXISTS dns_response_events (
                Id SERIAL PRIMARY KEY,
                Timestamp TIMESTAMPTZ NOT NULL,
                ClientIp INET NOT NULL,
                ClientName TEXT,
                QueryName TEXT NOT NULL,
                QueryType TEXT NOT NULL,
                Status TEXT NOT NULL,
                ResponseIp INET
            );

            CREATE INDEX IF NOT EXISTS idx_dns_response_events_timestamp ON dns_response_events(Timestamp);
            CREATE INDEX IF NOT EXISTS idx_dns_response_events_clientip ON dns_response_events(ClientIp);
            CREATE INDEX IF NOT EXISTS idx_dns_response_events_queryname ON dns_response_events(QueryName);
            CREATE INDEX IF NOT EXISTS idx_dns_response_events_status ON dns_response_events(Status);
        """);

        _logger.LogInformation("PostgreSQL schema initialized.");
    }
}
