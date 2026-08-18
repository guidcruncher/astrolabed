using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Astrolabed.Data;

namespace Astrolabed.Api.Services;

public interface IDnsService
{
    Task<DnsResponse> QueryAsync(string name, string type = "A", CancellationToken cancellationToken = default);
    Task<DnsResponse> QueryServerAsync(string name, string type, IPEndPoint endpoint, CancellationToken cancellationToken = default);
    PagedResult<DnsResponse> GetCachedResponsesPaged(int page = 1, int pageSize = 10, string? search = null);
    void FlushCache();
}
