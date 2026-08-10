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
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    private readonly IPEndPoint _endpoint;
    private readonly TimeSpan _timeout;

    public UdpDnsClient(IPEndPoint endpoint, TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        _endpoint = endpoint;
        _timeout = timeout ?? DefaultTimeout;
    }

    public async Task<byte[]> QueryAsync(byte[] request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(_timeout);
        CancellationToken token = cts.Token;

        try
        {
            using var socket = new Socket(_endpoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            socket.Connect(_endpoint);

            await socket.SendAsync(request, SocketFlags.None, token).ConfigureAwait(false);

            byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(MaxDnsUdpPacketSize);
            try
            {
                int received = await socket.ReceiveAsync(rentedBuffer, SocketFlags.None, token).ConfigureAwait(false);

                if (received < 12)
                {
                    return DnsResponseBuilder.BuildServfail(request);
                }

                // Check DNS Header Truncation bit (TC bit is Bit 1 of Byte 2: mask 0x02)
                bool isTruncated = (rentedBuffer[2] & 0x02) != 0;
                if (isTruncated)
                {
                    // Fallback to TCP to retrieve full payload
                    var tcpClient = new TcpDnsClient(_endpoint, _timeout);
                    return await tcpClient.QueryAsync(request, ct).ConfigureAwait(false);
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
            // Fallback attempt or network error failure returns SERVFAIL
            return DnsResponseBuilder.BuildServfail(request);
        }
    }
}
