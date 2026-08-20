// File: src/Astrolabed.Dns/Services/IDnsQueryProcessor.cs
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Astrolabed.Dns.Services;

public interface IDnsQueryProcessor
{
    Task<byte[]?> ProcessRequestAsync(byte[] rawPacket, EndPoint clientEndpoint, CancellationToken ct);
}


