using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Astrolabed.Api.Services;

public interface INtpService
{
    Task<NtpResponse> QueryAsync(CancellationToken cancellationToken = default);
    Task<NtpResponse> QueryServerAsync(IPEndPoint endpoint, CancellationToken cancellationToken = default);
}
