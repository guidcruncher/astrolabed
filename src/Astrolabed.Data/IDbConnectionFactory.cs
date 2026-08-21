using System.Data;

namespace Astrolabed.Data;

/// <summary>
/// Factory abstraction responsible for creating active database connections.
/// </summary>
public interface IDbConnectionFactory
{
    Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default);
}
