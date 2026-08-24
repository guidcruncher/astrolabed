using System.Data.Common;

namespace Astrolabed.Data;

/// <summary>
/// Factory abstraction responsible for asynchronously creating and opening active database connections.
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>
    /// Asynchronously creates, opens, and returns an active database connection.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task representing the asynchronous operation, containing the opened <see cref="DbConnection"/> instance.
    /// </returns>
    Task<DbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default);
}
