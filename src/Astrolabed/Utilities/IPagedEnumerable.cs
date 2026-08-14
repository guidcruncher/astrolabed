namespace Infrastructure.Pagination;

using System.Collections.Generic;

/// <summary>
/// Represents a paginated sequence of items with pagination metadata.
/// </summary>
/// <typeparam name="T">The type of elements in the collection.</typeparam>
public interface IPagedEnumerable<out T> : IEnumerable<T>
{
    /// <summary>
    /// Gets the current page number (1-based index).
    /// </summary>
    int PageIndex { get; }

    /// <summary>
    /// Gets the number of items per page.
    /// </summary>
    int PageSize { get; }

    /// <summary>
    /// Gets the total number of items across all pages.
    /// </summary>
    long TotalCount { get; }

    /// <summary>
    /// Gets the total number of calculated pages.
    /// </summary>
    int TotalPages { get; }

    /// <summary>
    /// Gets a value indicating whether a previous page exists.
    /// </summary>
    bool HasPreviousPage { get; }

    /// <summary>
    /// Gets a value indicating whether a next page exists.
    /// </summary>
    bool HasNextPage { get; }

    /// <summary>
    /// Gets the items on the current page.
    /// </summary>
    IReadOnlyCollection<T> Items { get; }
}
