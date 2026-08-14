using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Utilities;

/// <summary>
/// Extension methods for generating paginated collections and setting up Dependency Injection services.
/// </summary>
public static class PagedEnumerableExtensions
{
    /// <summary>
    /// Creates an <see cref="IPagedEnumerable{T}"/> from an in-memory <see cref="IEnumerable{T}"/>.
    /// </summary>
    public static IPagedEnumerable<T> ToPagedEnumerable<T>(
        this IEnumerable<T> source,
        int pageIndex,
        int pageSize,
        ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(source);

        pageIndex = Math.Max(1, pageIndex);
        pageSize = Math.Max(1, pageSize);

        logger?.LogDebug("Paginate in-memory collection: PageIndex={PageIndex}, PageSize={PageSize}", pageIndex, pageSize);

        var list = source as IReadOnlyCollection<T> ?? source.ToList();
        long totalCount = list.Count;

        var items = list
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedEnumerable<T>(items, totalCount, pageIndex, pageSize);
    }

    /// <summary>
    /// Registers pagination configuration options into Microsoft Dependency Injection container.
    /// </summary>
    public static IServiceCollection AddPaginationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<PaginationOptions>(
            configuration.GetSection(PaginationOptions.SectionName));

        return services;
    }
}
