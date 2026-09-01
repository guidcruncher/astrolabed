namespace Astrolabed.Data.Repositories;

using Astrolabed.Data.Models;

/// <summary>
/// Defines data access and persistence operations for <see cref="DnsListEntity"/> records.
/// </summary>
public interface IDnsListRepository
{
    /// <summary>
    /// Asynchronously retrieves a single DNS list entity by its unique identifier.
    /// </summary>
    /// <param name="id">The unique integer primary key of the DNS list.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The matching <see cref="DnsListEntity"/> if found; otherwise, <see langword="null"/>.</returns>
    Task<DnsListEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves all configured DNS list entities.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A read-only list containing all retrieved <see cref="DnsListEntity"/> records.</returns>
    Task<IReadOnlyList<DnsListEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously persists a new DNS list entity into the database.
    /// </summary>
    /// <param name="entity">The entity instance to add.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddAsync(DnsListEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously updates an existing DNS list entity in the database.
    /// </summary>
    /// <param name="entity">The entity instance containing updated values.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns><see langword="true"/> if the record was successfully updated; otherwise, <see langword="false"/>.</returns>
    Task<bool> UpdateAsync(DnsListEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously inserts a DNS list entity if it does not exist, or updates its existing fields if a primary key conflict occurs.
    /// </summary>
    /// <param name="entity">The entity instance to insert or update.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpsertAsync(DnsListEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously deletes a DNS list entity from the database by its unique identifier.
    /// </summary>
    /// <param name="id">The unique integer primary key of the DNS list to delete.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns><see langword="true"/> if a record was found and deleted; otherwise, <see langword="false"/>.</returns>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
