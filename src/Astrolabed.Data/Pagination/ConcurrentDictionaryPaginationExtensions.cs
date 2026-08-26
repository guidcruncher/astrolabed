namespace Astrolabed.Data.Pagination;

using System.Collections.Concurrent;

using Microsoft.Extensions.Logging;

/// <summary>
/// Provides extension methods for paginating in-memory dictionary collections.
/// </summary>
public static class ConcurrentDictionaryPaginationExtensions
{
    /// <summary>
    /// Converts the values of a <see cref="ConcurrentDictionary{TKey, TValue}"/> into a <see cref="PagedResult{TValue}"/>.
    /// </summary>
    /// <typeparam name="TKey">The key type in the dictionary.</typeparam>
    /// <typeparam name="TValue">The value type in the dictionary.</typeparam>
    /// <param name="dictionary">The concurrent dictionary to paginate.</param>
    /// <param name="pageNumber">The 1-based index of the target page.</param>
    /// <param name="pageSize">The number of items to include per page.</param>
    /// <param name="logger">Optional logger instance to log pagination details.</param>
    /// <returns>A populated <see cref="PagedResult{TValue}"/> record.</returns>
    public static PagedResult<TValue> ToPagedResult<TKey, TValue>(
        this ConcurrentDictionary<TKey, TValue> dictionary,
        int pageNumber,
        int pageSize,
        ILogger? logger = null)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(dictionary);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);

        // Snapshot key-value pairs or values to prevent modification during iteration
        long totalCount = dictionary.Count;

        if (totalCount == 0)
        {
            logger?.LogDebug("Dictionary is empty. Returning empty PagedResult.");
            return PagedResult<TValue>.Empty(pageNumber, pageSize);
        }

        int skipCount = (pageNumber - 1) * pageSize;

        // Take a paginated slice of dictionary values
        List<TValue> pageItems = dictionary.Values
            .Skip(skipCount)
            .Take(pageSize)
            .ToList();

        logger?.LogDebug(
            "Paginated ConcurrentDictionary. Total: {TotalCount}, Page: {PageNumber}, Size: {PageSize}, Yielded: {YieldedCount}",
            totalCount,
            pageNumber,
            pageSize,
            pageItems.Count);

        return PagedResult<TValue>.Create(pageItems, totalCount, pageNumber, pageSize);
    }
}
