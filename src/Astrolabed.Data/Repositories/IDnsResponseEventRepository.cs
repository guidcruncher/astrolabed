using Astrolabed.Data.Models;
using Astrolabed.Data.Pagination;

namespace Astrolabed.Data.Repositories;

/// <summary>
/// Repository abstraction for performing persistent CRUD and cleanup operations on DNS response event records.
/// </summary>
public interface IDnsResponseEventRepository
{
    /// <summary>
    /// Asynchronously persists a new DNS response event entity.
    /// </summary>
    /// <param name="entity">The DNS response event record to persist.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous save operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> is null.</exception>
    Task AddAsync(
        DnsResponseEventEntity entity,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves a DNS response event record by its unique identifier.
    /// </summary>
    /// <param name="id">The unique event identifier.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task representing the asynchronous operation, containing the matching <see cref="DnsResponseEventEntity"/> if found;
    /// otherwise, <see langword="null"/>.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="id"/> is null or whitespace.</exception>
    Task<DnsResponseEventEntity?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves a paged collection of DNS response event records ordered chronologically.
    /// </summary>
    /// <param name="pageNumber">The 1-based index of the target page.</param>
    /// <param name="pageSize">The maximum number of records to return per page.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation, containing the <see cref="PagedResult{T}"/> for the requested page.</returns>
    Task<PagedResult<DnsResponseEventEntity>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously deletes a DNS response event record by its unique identifier.
    /// </summary>
    /// <param name="id">The unique event identifier of the record to remove.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task representing the asynchronous operation, containing <see langword="true"/> if a record was removed;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="id"/> is null or whitespace.</exception>
    Task<bool> DeleteAsync(
        string id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously removes historical DNS response events older than the default retention period or specified threshold.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous cleanup operation.</returns>
    Task CleanOldDataAsync(CancellationToken cancellationToken = default);
}
