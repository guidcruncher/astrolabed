using Microsoft.Data.Sqlite;
using Npgsql;
using System.Data;

namespace Astrolabed.Data;

public interface IDbConnectionFactory
{
    IDbConnection Create();
}
