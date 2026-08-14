using System;
using System.Collections;
using System.Collections.Generic;

namespace Astrolabed.Utilities;

/// <summary>
/// Read-only concrete implementation of <see cref="IPagedEnumerable{T}"/>.
/// </summary>
/// <typeparam name="T">The type of items contained within the page.</typeparam>
public class PagedEnumerable<T> : IPagedEnumerable<T>
{
    public int PageIndex { get; }
    public int PageSize { get; }
    public long TotalCount { get; }
    public int TotalPages { get; }
    public bool HasPreviousPage => PageIndex > 1;
    public bool HasNextPage => PageIndex < TotalPages;
    public IReadOnlyCollection<T> Items { get; }

    public PagedEnumerable(IReadOnlyCollection<T> items, long totalCount, int pageIndex, int pageSize)
    {
        ArgumentNullException.ThrowIfNull(items);

        if (pageIndex < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageIndex), "Page index must be greater than or equal to 1.");
        }

        if (pageSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than or equal to 1.");
        }

        if (totalCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalCount), "Total count cannot be negative.");
        }

        Items = items;
        TotalCount = totalCount;
        PageIndex = pageIndex;
        PageSize = pageSize;
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
    }

    public IEnumerator<T> GetEnumerator() => Items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
