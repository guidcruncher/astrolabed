namespace Astrolabed.Data.Pagination;

using Microsoft.Extensions.Logging;

/// <summary>
/// High-performance extension methods for paginating list-based collections.
/// </summary>
public static class ListPaginationExtensions
{
    /// <summary>
    /// Converts an <see cref="IReadOnlyList{T}"/> into a <see cref="PagedResult{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of items in the list.</typeparam>
    /// <param name="source">The read-only list instance.</param>
    /// <param name="pageNumber">1-based page index.</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="logger">Optional logger instance.</param>
    /// <returns>A populated <see cref="PagedResult{T}"/> instance.</returns>
    public static PagedResult<T> ToPagedResult<T>(
        this IReadOnlyList<T> source,
        int pageNumber,
        int pageSize,
        ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);

        return PaginateIndexableCollection(
            count: source.Count,
            indexer: index => source[index],
            pageNumber: pageNumber,
            pageSize: pageSize,
            typeName: typeof(T).Name,
            logger: logger);
    }

    /// <summary>
    /// Converts an <see cref="IList{T}"/> into a <see cref="PagedResult{T}"/>.
    /// Direct overload ensuring types implementing only <see cref="IList{T}"/> do not require explicit casting.
    /// </summary>
    /// <typeparam name="T">The type of items in the list.</typeparam>
    /// <param name="source">The list instance.</param>
    /// <param name="pageNumber">1-based page index.</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="logger">Optional logger instance.</param>
    /// <returns>A populated <see cref="PagedResult{T}"/> instance.</returns>
    public static PagedResult<T> ToPagedResult<T>(
        this IList<T> source,
        int pageNumber,
        int pageSize,
        ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);

        return PaginateIndexableCollection(
            count: source.Count,
            indexer: index => source[index],
            pageNumber: pageNumber,
            pageSize: pageSize,
            typeName: typeof(T).Name,
            logger: logger);
    }

    /// <summary>
    /// Core high-performance pagination engine using O(1) indexers.
    /// </summary>
    private static PagedResult<T> PaginateIndexableCollection<T>(
        int count,
        Func<int, T> indexer,
        int pageNumber,
        int pageSize,
        string typeName,
        ILogger? logger)
    {
        if (count == 0)
        {
            logger?.LogDebug("Source collection is empty. Returning empty PagedResult.");
            return PagedResult<T>.Empty(pageNumber, pageSize);
        }

        int skipCount = (pageNumber - 1) * pageSize;

        if (skipCount >= count)
        {
            logger?.LogDebug("Requested page number {PageNumber} exceeds total items count ({Count}). Returning empty page.", pageNumber, count);
            return PagedResult<T>.Create([], count, pageNumber, pageSize);
        }

        int takeCount = Math.Min(pageSize, count - skipCount);

        List<T> pageItems = new(takeCount);
        for (int i = 0; i < takeCount; i++)
        {
            pageItems.Add(indexer(skipCount + i));
        }

        logger?.LogDebug(
            "Paginated collection of {TypeName}. Total: {TotalCount}, Page: {PageNumber}, Size: {PageSize}, Yielded: {YieldedCount}",
            typeName,
            count,
            pageNumber,
            pageSize,
            pageItems.Count);

        return PagedResult<T>.Create(pageItems, count, pageNumber, pageSize);
    }
}
