using System.Net;

using Astrolabed;
using Astrolabed.Data.Entities;
using Astrolabed.Events;

namespace Astrolabed.Data.Repositories;

/// <summary>
/// Data access abstraction for persisting and querying DNS response events using LiteDB.
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
    /// Retrieves records matching the specified time frame.
    /// </summary>
    IEnumerable<DnsResponseEvent> GetByTimeRange(DateTimeOffset start, DateTimeOffset end, int limit = 100);

    /// <summary>
    /// Retrieves records filtered by the requesting client's IP address.
    /// </summary>
    IEnumerable<DnsResponseEvent> GetByClientIp(IPAddress clientIp, int limit = 100);

    /// <summary>
    /// Retrieves records matching a specific DNS response status (e.g. NOERROR, NXDOMAIN).
    /// </summary>
    IEnumerable<DnsResponseEvent> GetByStatus(string status, int limit = 100);

    /// <summary>
    /// Retrieves all records
    /// </summary>
    IEnumerable<DnsResponseEvent> GetAll(int limit = 100);

    /// <summary>
    /// Purges events older than the specified cutoff timestamp.
    /// </summary>
    int DeleteOlderThan(DateTimeOffset cutoff);
}
