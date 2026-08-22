// File: src/Astrolabed.Dns/Services/IDnsQueryProcessor.cs
using System.Net;

namespace Astrolabed.Dns.Services;

public interface IDnsQueryProcessor
{
    Task<byte[]?> ProcessRequestAsync(byte[] rawPacket, EndPoint clientEndpoint, CancellationToken ct);
}


