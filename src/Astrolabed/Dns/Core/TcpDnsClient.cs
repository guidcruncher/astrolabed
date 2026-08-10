using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Astrolabed.Dns.Core;

/// <summary>
/// DNS-over-TCP client implementing RFC 1035 Section 4.2.2 (2-byte length prefix framing).
/// </summary>
public sealed class TcpDnsClient : IDnsClient
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    private readonly IPEndPoint _endpoint;
    private readonly TimeSpan _timeout;

    public TcpDnsClient(IPEndPoint endpoint, TimeSpan? timeout = null)
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
            using var socket = new Socket(_endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true
            };

            await socket.ConnectAsync(_endpoint, token).ConfigureAwait(false);

            ushort requestLength = (ushort)request.Length;
            byte[] sendBuffer = ArrayPool<byte>.Shared.Rent(2 + request.Length);

            try
            {
                BinaryPrimitives.WriteUInt16BigEndian(sendBuffer.AsSpan(0, 2), requestLength);
                request.CopyTo(sendBuffer.AsSpan(2));

                await socket.SendAsync(sendBuffer.AsMemory(0, 2 + request.Length), SocketFlags.None, token).ConfigureAwait(false);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(sendBuffer);
            }

            byte[] lengthBuffer = ArrayPool<byte>.Shared.Rent(2);
            ushort responseLength;

            try
            {
                await ReadExactAsync(socket, lengthBuffer.AsMemory(0, 2), token).ConfigureAwait(false);
                responseLength = BinaryPrimitives.ReadUInt16BigEndian(lengthBuffer.AsSpan(0, 2));
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(lengthBuffer);
            }

            if (responseLength < 12)
            {
                return DnsResponseBuilder.BuildServfail(request);
            }

            byte[] payloadBuffer = ArrayPool<byte>.Shared.Rent(responseLength);
            try
            {
                await ReadExactAsync(socket, payloadBuffer.AsMemory(0, responseLength), token).ConfigureAwait(false);

                var response = GC.AllocateUninitializedArray<byte>(responseLength);
                payloadBuffer.AsSpan(0, responseLength).CopyTo(response);
                return response;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(payloadBuffer);
            }
        }
        catch
        {
            return DnsResponseBuilder.BuildServfail(request);
        }
    }

    private static async ValueTask ReadExactAsync(Socket socket, Memory<byte> target, CancellationToken ct)
    {
        int totalRead = 0;
        while (totalRead < target.Length)
        {
            int read = await socket.ReceiveAsync(target[totalRead..], SocketFlags.None, ct).ConfigureAwait(false);
            if (read == 0)
            {
                throw new SocketException((int)SocketError.ConnectionReset);
            }
            totalRead += read;
        }
    }
}
