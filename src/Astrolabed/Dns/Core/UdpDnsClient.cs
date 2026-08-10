using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Astrolabed.Dns.Core;

public sealed class UdpDnsClient : IDnsClient
{
    private const int MaxDnsUdpPacketSize = 4096;
    private readonly IPEndPoint _endpoint;

    public UdpDnsClient(IPEndPoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        _endpoint = endpoint;
    }

    public async Task<byte[]> QueryAsync(byte[] request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            using var socket = new Socket(_endpoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            socket.Connect(_endpoint);

            await socket.SendAsync(request, SocketFlags.None, ct).ConfigureAwait(false);

            byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(MaxDnsUdpPacketSize);
            try
            {
                int received = await socket.ReceiveAsync(rentedBuffer, SocketFlags.None, ct).ConfigureAwait(false);

                if (received < 12)
                {
                    return DnsResponseBuilder.BuildServfail(request);
                }

                var response = GC.AllocateUninitializedArray<byte>(received);
                rentedBuffer.AsSpan(0, received).CopyTo(response);
                return response;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rentedBuffer);
            }
        }
        catch
        {
            // Return a safe SERVFAIL on network error, timeout, or malformed response
            return DnsResponseBuilder.BuildServfail(request);
        }
    }
}
