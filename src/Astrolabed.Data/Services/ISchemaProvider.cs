// File: src/Astrolabed.Data/Services/ISchemaProvider.cs
namespace Astrolabed.Data.Services;

/// <summary>
/// Defines a contract for retrieving SQL schema scripts used for database initialization and migrations.
/// </summary>
public interface ISchemaProvider
{
    /// <summary>
    /// Asynchronously retrieves the SQL schema script content.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to signal operation cancellation.</param>
    /// <returns>The raw SQL string defining the schema.</returns>
    Task<string> GetSchemaSqlAsync(CancellationToken cancellationToken = default);
}
