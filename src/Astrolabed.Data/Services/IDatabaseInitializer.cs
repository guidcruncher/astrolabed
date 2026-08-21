namespace Astrolabed.Data.Services;

/// <summary>
/// Defines database initialization and schema deployment operations.
/// </summary>
public interface IDatabaseInitializer
{
    /// <summary>
    /// Ensures the underlying database exists and executes the embedded schema initialization script.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task InitializeAsync(CancellationToken cancellationToken = default);
}

