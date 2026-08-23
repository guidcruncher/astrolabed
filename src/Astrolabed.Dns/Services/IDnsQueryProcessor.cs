using System.Net;

namespace Astrolabed.Dns.Services;

/// <summary>
/// Defines the processing engine contract for parsing raw inbound DNS wire-format packets and generating response payloads.
/// </summary>
public interface IDnsQueryProcessor
{
    /// <summary>
    /// Asynchronously processes an incoming raw DNS query packet and produces a wire-format response.
    /// </summary>
    /// <param name="rawPacket">A read-only memory buffer slice containing the complete wire-format DNS request packet.</param>
    /// <param name="clientEndpoint">The remote network endpoint from which the query originated.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task representing the asynchronous query processing operation. The task result contains a byte array 
    /// containing the encoded DNS response packet, or <see langword="null"/> if no response should be returned (e.g., silent dropped queries).
    /// </returns>
    Task<byte[]?> ProcessRequestAsync(ReadOnlyMemory<byte> rawPacket, EndPoint clientEndpoint, CancellationToken ct = default);
}
