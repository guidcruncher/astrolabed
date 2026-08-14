using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Astrolabed.Dns.Core;

namespace Astrolabed.Api.Services;

public interface IDnsRequestHandler
{
    /// <summary>
    /// Processes a raw DNS request buffer through the RuleEngine and produces a structured result.
    /// </summary>
    Task<DnsHandlerResult> HandleAsync(
        byte[] request,
        IPEndPoint remote,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes a raw DNS request buffer through the RuleEngine and returns a managed pooled response buffer.
    /// </summary>
    Task<PooledBuffer?> ProcessAsync(
        byte[] request,
        IPEndPoint remote,
        CancellationToken cancellationToken = default);
}
