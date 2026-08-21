using Astrolabed.Data.Models;
using Astrolabed.Data.Pagination;

namespace Astrolabed.Data.Repositories;

/// <summary>
/// Repository abstraction for performing persistent CRUD operations on DNS response event records.
/// </summary>
public interface IDnsResponseEventRepository
{
    Task AddAsync(DnsResponseEventEntity entity, CancellationToken cancellationToken = default);

    Task<DnsResponseEventEntity?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<PagedResult<DnsResponseEventEntity>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);

    Task CleanOldData(CancellationToken cancellationToken = default);
}
