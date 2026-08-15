using System.Data;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Astrolabed.Data;

public class DbConnectionFactory : IDbConnectionFactory
{
    private readonly DbOptions _options;

    public DbConnectionFactory(IOptions<DbOptions> dbOptions)
    {
        _options = dbOptions.Value;
    }

    public IDbConnection Create()
    {
        return _options.Provider switch
        {
            DatabaseProvider.Sqlite => CreateSqlite(),
            DatabaseProvider.Postgres => CreatePostgres(),
            _ => throw new NotSupportedException("Unknown database provider")
        };
    }

    private IDbConnection CreatePostgres()
    {
	var conn = new NpgsqlConnection(_options.ConnectionString);
	return conn;
    }

    private IDbConnection CreateSqlite()
    {
        var conn = new SqliteConnection(_options.ConnectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "PRAGMA journal_mode=WAL;";
        cmd.ExecuteNonQuery();

        return conn;
    }
}
