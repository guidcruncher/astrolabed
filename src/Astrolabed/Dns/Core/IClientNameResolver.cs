using System.Net;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Astrolabed.Dns.Core;

public interface IClientNameResolver
{

    Task<string?> Resolve(IPAddress clientIp, CancellationToken ct);

}
