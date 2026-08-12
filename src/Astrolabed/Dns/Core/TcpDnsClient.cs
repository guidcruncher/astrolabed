using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Dns.Core;

/// <summary>
/// DNS-over-TCP client implementing RFC 1035 Section 4.2.2 (2-byte length prefix framing).
/// </summary>
public sealed class TcpDnsClient : IDnsClient
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    private readonly IPEndPoint _endpoint;
    private readonly TimeSpan _timeout;
    private readonly ILogger<TcpDnsClient>? _logger;

    public TcpDnsClient(IPEndPoint endpoint, TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        _endpoint = endpoint;
        _timeout = timeout ?? DefaultTimeout;
        _logger = null;
    }

    public TcpDnsClient(
        IPEndPoint endpoint,
        IOptions<DnsForwarderOptions> options,
        ILogger<TcpDnsClient> logger)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _endpoint = endpoint;
        _timeout = TimeSpan.FromMilliseconds(options.Value.UpstreamTimeoutMs);
        _logger = logger;
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
            int totalSendLength = 2 + request.Length;
            byte[] sendBuffer = ArrayPool<byte>.Shared.Rent(totalSendLength);

            try
            {
                BinaryPrimitives.WriteUInt16BigEndian(sendBuffer.AsSpan(0, 2), requestLength);
                request.AsSpan().CopyTo(sendBuffer.AsSpan(2, request.Length));

                await socket.SendAsync(sendBuffer.AsMemory(0, totalSendLength), SocketFlags.None, token).ConfigureAwait(false);
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
                _logger?.LogWarning("Received truncated DNS TCP response under 12-byte header size from {Endpoint}.", _endpoint);
                return DnsResponseBuilder.BuildServfail(request);
            }

            byte[] payloadBuffer = ArrayPool<byte>.Shared.Rent(responseLength);
            try
            {
                await ReadExactAsync(socket, payloadBuffer.AsMemory(0, responseLength), token).ConfigureAwait(false);

                byte[] response = GC.AllocateUninitializedArray<byte>(responseLength);
                payloadBuffer.AsSpan(0, responseLength).CopyTo(response);
                return response;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(payloadBuffer);
            }
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            _logger?.LogWarning("TCP DNS query to {Endpoint} timed out after {Timeout}ms.", _endpoint, _timeout.TotalMilliseconds);
            return DnsResponseBuilder.BuildServfail(request);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to execute TCP DNS query to {Endpoint}.", _endpoint);
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
