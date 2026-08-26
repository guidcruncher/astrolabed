namespace Astrolabed.Data.Pagination;

using Microsoft.Extensions.Logging;

/// <summary>
/// Extended pagination utilities for unindexed collections, database queries, and memory spans.
/// </summary>
public static class AdvancedPaginationExtensions
{
    /// <summary>
    /// Converts non-indexable collections with a known size (e.g., HashSet, Queue, Stack) into a <see cref="PagedResult{T}"/>.
    /// Uses O(1) total count discovery and LINQ Skip/Take for slicing.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="source">The source read-only collection.</param>
    /// <param name="pageNumber">1-based page index.</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="logger">Optional logger instance.</param>
    /// <returns>A populated <see cref="PagedResult{T}"/> instance.</returns>
    public static PagedResult<T> ToPagedResult<T>(
        this IReadOnlyCollection<T> source,
        int pageNumber,
        int pageSize,
        ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);

        int totalCount = source.Count;

        if (totalCount == 0)
        {
            logger?.LogDebug("IReadOnlyCollection is empty. Returning empty PagedResult.");
            return PagedResult<T>.Empty(pageNumber, pageSize);
        }

        int skipCount = (pageNumber - 1) * pageSize;

        if (skipCount >= totalCount)
        {
            logger?.LogDebug("Requested page {PageNumber} exceeds size {TotalCount}. Returning empty page.", pageNumber, totalCount);
            return PagedResult<T>.Create([], totalCount, pageNumber, pageSize);
        }

        List<T> pageItems = source
            .Skip(skipCount)
            .Take(pageSize)
            .ToList();

        logger?.LogDebug(
            "Paginated IReadOnlyCollection<{TypeName}>. Total: {TotalCount}, Page: {PageNumber}, Size: {PageSize}, Yielded: {YieldedCount}",
            typeof(T).Name,
            totalCount,
            pageNumber,
            pageSize,
            pageItems.Count);

        return PagedResult<T>.Create(pageItems, totalCount, pageNumber, pageSize);
    }

    /// <summary>
    /// Converts a provider-backed queryable stream (e.g., Entity Framework Core) into a <see cref="PagedResult{T}"/>.
    /// Executes SQL COUNT and OFFSET/FETCH directly on the database engine.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The deferred LINQ queryable expression.</param>
    /// <param name="pageNumber">1-based page index.</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="logger">Optional logger instance.</param>
    /// <returns>A populated <see cref="PagedResult{T}"/> instance.</returns>
    public static PagedResult<T> ToPagedResult<T>(
        this IQueryable<T> query,
        int pageNumber,
        int pageSize,
        ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);

        int totalCount = query.Count();

        if (totalCount == 0)
        {
            logger?.LogDebug("IQueryable source returned 0 items from database.");
            return PagedResult<T>.Empty(pageNumber, pageSize);
        }

        int skipCount = (pageNumber - 1) * pageSize;

        List<T> pageItems = query
            .Skip(skipCount)
            .Take(pageSize)
            .ToList();

        logger?.LogDebug(
            "Paginated IQueryable<{TypeName}> via SQL. Total: {TotalCount}, Page: {PageNumber}, Size: {PageSize}, Yielded: {YieldedCount}",
            typeof(T).Name,
            totalCount,
            pageNumber,
            pageSize,
            pageItems.Count);

        return PagedResult<T>.Create(pageItems, totalCount, pageNumber, pageSize);
    }

    /// <summary>
    /// Slices contiguous memory blocks (<see cref="ReadOnlySpan{T}"/>) without allocating new intermediate memory buffers.
    /// </summary>
    /// <typeparam name="T">The type of elements in the span.</typeparam>
    /// <param name="span">The continuous memory view.</param>
    /// <param name="pageNumber">1-based page index.</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <returns>A populated <see cref="PagedResult{T}"/> instance containing a copy of the slice.</returns>
    public static PagedResult<T> ToPagedResult<T>(
        this ReadOnlySpan<T> span,
        int pageNumber,
        int pageSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);

        int totalCount = span.Length;

        if (totalCount == 0)
        {
            return PagedResult<T>.Empty(pageNumber, pageSize);
        }

        int skipCount = (pageNumber - 1) * pageSize;

        if (skipCount >= totalCount)
        {
            return PagedResult<T>.Create([], totalCount, pageNumber, pageSize);
        }

        int takeCount = Math.Min(pageSize, totalCount - skipCount);

        // Uses Span.Slice to extract the target window zero-copy before converting to array for the record
        T[] pageItems = span.Slice(skipCount, takeCount).ToArray();

        return PagedResult<T>.Create(pageItems, totalCount, pageNumber, pageSize);
    }
}
