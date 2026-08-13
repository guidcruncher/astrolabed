using System.Data;
using System.Net;

using Astrolabed;
using Astrolabed.Data.Entities;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Data.Repositories;

public static class DatabaseBuilder
{
    public static void InitializeDatabase(string connectionString)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        // Configure Write-Ahead Logging (WAL) mode and performance pragmas
        using (var pragmaCmd = connection.CreateCommand())
        {
            pragmaCmd.CommandText = @"
                PRAGMA journal_mode = WAL;
                PRAGMA synchronous = NORMAL;
                PRAGMA foreign_keys = ON;
            ";
            pragmaCmd.ExecuteNonQuery();
        }

        // Initialize schema and query indices
        using (var schemaCmd = connection.CreateCommand())
        {
            schemaCmd.CommandText = $"""
                CREATE TABLE IF NOT EXISTS dns_response_events (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Timestamp TEXT NOT NULL,
                    ClientIp TEXT NOT NULL,
                    ClientName TEXT NULL,
                    QueryName TEXT NOT NULL,
                    QueryType TEXT NOT NULL,
                    Status TEXT NOT NULL,
                    ResponseIp TEXT NULL
                );

                CREATE INDEX IF NOT EXISTS idx_dns_response_events_timestamp ON dns_response_events(Timestamp);
                CREATE INDEX IF NOT EXISTS idx_dns_response_events_clientip ON dns_response_events(ClientIp);
                CREATE INDEX IF NOT EXISTS idx_dns_response_events_queryname ON dns_response_events(QueryName);
                CREATE INDEX IF NOT EXISTS idx_dns_response_events_status ON dns_response_events(Status);
            """;
            schemaCmd.ExecuteNonQuery();
        }

    }

}
