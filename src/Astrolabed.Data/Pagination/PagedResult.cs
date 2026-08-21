using System.Collections.ObjectModel;

namespace Astrolabed.Data.Pagination;

/// <summary>
/// Represents a thread-safe, immutable page-number-based pagination result container.
/// </summary>
/// <typeparam name="T">The type of item contained in the paged collection.</typeparam>
public sealed record PagedResult<T>
{
    /// <summary>
    /// Gets the collection of items for the current page.
    /// </summary>
    public IReadOnlyList<T> Items { get; init; }

    /// <summary>
    /// Gets the total count of items across all pages.
    /// </summary>
    public long TotalCount { get; init; }

    /// <summary>
    /// Gets the total number of available pages based on <see cref="TotalCount"/> and <see cref="PageSize"/>.
    /// </summary>
    public int TotalPages { get; init; }

    /// <summary>
    /// Gets the current 1-based page number.
    /// </summary>
    public int CurrentPage { get; init; }

    /// <summary>
    /// Gets the number of items configured per page.
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// Gets a value indicating whether there is a previous page available.
    /// </summary>
    public bool HasPreviousPage => CurrentPage > 1;

    /// <summary>
    /// Gets a value indicating whether there is a next page available.
    /// </summary>
    public bool HasNextPage => CurrentPage < TotalPages;

    /// <summary>
    /// Initializes a new instance of the <see cref="PagedResult{T}"/> record.
    /// </summary>
    /// <param name="items">The items for the current page.</param>
    /// <param name="totalCount">The total item count across all pages.</param>
    /// <param name="pageNumber">The current 1-based page number.</param>
    /// <param name="pageSize">The requested number of items per page.</param>
    public PagedResult(IReadOnlyList<T> items, long totalCount, int pageNumber, int pageSize)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentOutOfRangeException.ThrowIfNegative(totalCount);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);

        Items = items;
        TotalCount = totalCount;
        CurrentPage = pageNumber;
        PageSize = pageSize;
        TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
    }

    /// <summary>
    /// Creates a new <see cref="PagedResult{T}"/> instance.
    /// </summary>

    /// <param name="items">The items on the current page.</param>
    /// <param name="totalCount">The total number of items in the underlying dataset.</param>
    /// <param name="pageNumber">The 1-based current page number.</param>
    /// <param name="pageSize">The page size.</param>
    /// <returns>A populated <see cref="PagedResult{T}"/> instance.</returns>
    public static PagedResult<T> Create(IEnumerable<T> items, long totalCount, int pageNumber, int pageSize)
    {
        ArgumentNullException.ThrowIfNull(items);

        IReadOnlyList<T> readOnlyItems = items switch
        {
            IReadOnlyList<T> list => list,
            _ => new ReadOnlyCollection<T>(items.ToList())
        };

        return new PagedResult<T>(readOnlyItems, totalCount, pageNumber, pageSize);
    }

    /// <summary>
    /// Creates an empty <see cref="PagedResult{T}"/> instance.
    /// </summary>
    /// <returns>An empty paged result set.</returns>
    public static PagedResult<T> Empty(int pageNumber = 1, int pageSize = 10)
    {
        return new PagedResult<T>(Array.Empty<T>(), 0, pageNumber, pageSize);
    }
}
