using System.Buffers;
using System.Net;
using System.Net.Sockets;

using Astrolabed.Ntp.Options;
using Astrolabed.Ntp.Protocol;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Ntp.Services;

/// <summary>
/// High-throughput background service that listens for incoming UDP NTP client requests
/// and dispatches precise responses using zero-allocation socket operations.
/// </summary>
/// <param name="handler">The NTP packet response crafting handler.</param>
/// <param name="timeResolver">The high-precision time resolution service.</param>
/// <param name="optionsMonitor">Monitored server options configuration.</param>
/// <param name="logger">Structured logger instance.</param>
public sealed partial class NtpServerEngine(
    INtpServerHandler handler,
    ITimeResolver timeResolver,
    IOptionsMonitor<NtpServerOptions> optionsMonitor,
    ILogger<NtpServerEngine> logger) : BackgroundService
{
    private const int MaxNtpPacketSize = 512;

    private readonly INtpServerHandler _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    private readonly ITimeResolver _timeResolver = timeResolver ?? throw new ArgumentNullException(nameof(timeResolver));
    private readonly IOptionsMonitor<NtpServerOptions> _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
    private readonly ILogger<NtpServerEngine> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        NtpServerOptions options = _optionsMonitor.CurrentValue;
        IPAddress ipAddress = IPAddress.Parse(options.ListenAddress.Address);
        var localEndPoint = new IPEndPoint(ipAddress, options.ListenAddress.Port);

        using var socket = new Socket(localEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
        socket.Bind(localEndPoint);

        LogNtpServerStarted(_logger, localEndPoint);

        byte[] receiveBuffer = ArrayPool<byte>.Shared.Rent(MaxNtpPacketSize);
        byte[] sendBuffer = ArrayPool<byte>.Shared.Rent(MaxNtpPacketSize);

        try
        {
            EndPoint remoteEndPoint = new IPEndPoint(localEndPoint.AddressFamily == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any, 0);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    SocketReceiveFromResult receiveResult = await socket.ReceiveFromAsync(
                        receiveBuffer.AsMemory(0, MaxNtpPacketSize),
                        SocketFlags.None,
                        remoteEndPoint,
                        stoppingToken).ConfigureAwait(false);

                    DateTimeOffset receiveTime = await _timeResolver.GetCurrentTimeAsync(stoppingToken).ConfigureAwait(false);

                    if (receiveResult.ReceivedBytes < NtpPacketSerializer.HeaderSize)
                    {
                        LogInvalidPacketSize(_logger, receiveResult.RemoteEndPoint, receiveResult.ReceivedBytes);
                        continue;
                    }

                    ReadOnlySpan<byte> incomingPayload = receiveBuffer.AsSpan(0, receiveResult.ReceivedBytes);
                    if (!NtpPacketSerializer.TryDeserialize(incomingPayload, out NtpPacket? requestPacket) || requestPacket is null)
                    {
                        LogPacketDeserializationFailed(_logger, receiveResult.RemoteEndPoint);
                        continue;
                    }

                    if (requestPacket.Mode != NtpMode.Client)
                    {
                        LogNonClientModeReceived(_logger, requestPacket.Mode, receiveResult.RemoteEndPoint);
                        continue;
                    }

                    DateTimeOffset transmitTime = await _timeResolver.GetCurrentTimeAsync(stoppingToken).ConfigureAwait(false);
                    NtpPacket responsePacket = _handler.CreateResponse(requestPacket, receiveTime, transmitTime);

                    if (NtpPacketSerializer.TrySerialize(responsePacket, sendBuffer, out int bytesWritten))
                    {
                        await socket.SendToAsync(
                            sendBuffer.AsMemory(0, bytesWritten),
                            SocketFlags.None,
                            receiveResult.RemoteEndPoint,
                            stoppingToken).ConfigureAwait(false);

                        LogTraceResponseSent(_logger, receiveResult.RemoteEndPoint);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    LogProcessingError(_logger, ex);
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(receiveBuffer);
            ArrayPool<byte>.Shared.Return(sendBuffer);
        }

        LogNtpServerStopped(_logger);
    }

    [LoggerMessage(EventId = 101, Level = LogLevel.Information, Message = "NTP Server listening on {Endpoint}")]
    private static partial void LogNtpServerStarted(ILogger logger, IPEndPoint endpoint);

    [LoggerMessage(EventId = 102, Level = LogLevel.Information, Message = "NTP Server service stopped.")]
    private static partial void LogNtpServerStopped(ILogger logger);

    [LoggerMessage(EventId = 201, Level = LogLevel.Warning, Message = "Received invalid UDP datagram from {RemoteEndPoint}: packet too small ({Length} bytes)")]
    private static partial void LogInvalidPacketSize(ILogger logger, EndPoint remoteEndPoint, int length);

    [LoggerMessage(EventId = 202, Level = LogLevel.Warning, Message = "Failed to deserialize NTP packet from {RemoteEndPoint}")]
    private static partial void LogPacketDeserializationFailed(ILogger logger, EndPoint remoteEndPoint);

    [LoggerMessage(EventId = 203, Level = LogLevel.Warning, Message = "Received non-client NTP mode ({Mode}) from {RemoteEndPoint}")]
    private static partial void LogNonClientModeReceived(ILogger logger, NtpMode mode, EndPoint remoteEndPoint);

    [LoggerMessage(EventId = 204, Level = LogLevel.Error, Message = "Error occurred while processing NTP request.")]
    private static partial void LogProcessingError(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 301, Level = LogLevel.Trace, Message = "Responded to NTP client {RemoteEndPoint}")]
    private static partial void LogTraceResponseSent(ILogger logger, EndPoint remoteEndPoint);
}
