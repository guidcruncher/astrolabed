using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Astrolabed.Data.Entities;
using Astrolabed.Events;

namespace Astrolabed.Data.Repositories;

/// <summary>
/// Data access abstraction for persisting and querying DNS response events using LiteDB or relational stores.
/// </summary>
public interface IDnsResponseEventRepository
{
    /// <summary>
    /// Inserts a single DNS response event synchronously.
    /// </summary>
    void Add(DnsResponseEvent responseEvent);

    /// <summary>
    /// Inserts a single DNS response event asynchronously.
    /// </summary>
    Task AddAsync(DnsResponseEvent responseEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a high-performance batch insert of multiple DNS response events.
    /// </summary>
    void AddBatch(IEnumerable<DnsResponseEvent> responseEvents);

    /// <summary>
    /// Retrieves records matching the specified time frame with pagination.
    /// </summary>
    PagedResult<DnsResponseEventDto> GetByTimeRange(DateTimeOffset start, DateTimeOffset end, int pageNumber = 1, int pageSize = 100);

    /// <summary>
    /// Retrieves records filtered by the requesting client's IP address with pagination.
    /// </summary>
    PagedResult<DnsResponseEventDto> GetByClientIp(IPAddress clientIp, int pageNumber = 1, int pageSize = 100);

    /// <summary>
    /// Retrieves records matching a specific DNS response status (e.g. NOERROR, NXDOMAIN) with pagination.
    /// </summary>
    PagedResult<DnsResponseEventDto> GetByStatus(string status, int pageNumber = 1, int pageSize = 100);

    /// <summary>
    /// Retrieves all records with pagination.
    /// </summary>
    PagedResult<DnsResponseEventDto> GetAll(int pageNumber = 1, int pageSize = 100);

    /// <summary>
    /// Purges events older than the specified cutoff timestamp.
    /// </summary>
    int DeleteOlderThan(DateTimeOffset cutoff);
}
