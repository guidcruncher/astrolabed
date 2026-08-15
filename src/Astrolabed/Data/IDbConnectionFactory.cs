using System.Data;

using Microsoft.Data.Sqlite;

using Npgsql;

namespace Astrolabed.Data;

public interface IDbConnectionFactory
{
    IDbConnection Create();
}
