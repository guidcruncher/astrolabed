using System.Buffers.Binary;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

using Astrolabed.Events;
using Astrolabed.Utilities;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Dhcp;

public sealed class DhcpServerEngine : IDhcpServerEngine
{
    private readonly ILogger<DhcpServerEngine> _logger;
    private readonly DhcpOptions _config;
    private readonly IDhcpLeaseStore _store;
    private readonly IUdpTransport _transport;
    private readonly IDhcpMetrics _metrics;

    private readonly IDhcpLeaseEngine _leaseEngine;
    private readonly ICidrPoolAllocator _pool;
    private readonly IArpConflictDetector _arp;

    private readonly IPAddress _serverId;
    private readonly IPAddress _router;
    private readonly IPAddress _dns;
    private readonly IPAddress? _ntp;
    private readonly string? _webproxy;
    private readonly IPAddress _subnetMask;
    private readonly TimeSpan _defaultLeaseTime;
    private readonly TimeSpan _maxLeaseTime;

    private readonly bool _testMode;
    private IPEndPoint? _lastClient;

    public DhcpServerEngine(
        ILogger<DhcpServerEngine> logger,
        IOptions<DhcpOptions> configOptions,
        IDhcpLeaseStore store,
        IUdpTransport transport,
        IDhcpMetrics metrics,
        IDhcpLeaseEngine leaseEngine,
        ICidrPoolAllocator pool,
        IArpConflictDetector arp,
        bool testMode = false)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = configOptions?.Value ?? throw new ArgumentNullException(nameof(configOptions));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        _leaseEngine = leaseEngine ?? throw new ArgumentNullException(nameof(leaseEngine));
        _pool = pool ?? throw new ArgumentNullException(nameof(pool));
        _arp = arp ?? throw new ArgumentNullException(nameof(arp));
        _testMode = testMode;

        _serverId = IPAddress.Parse(_config.ServerIdentifier);
        _router = IPAddress.Parse(_config.Router);
        _dns = IPAddress.Parse(_config.DnsServer);
        _subnetMask = ParseSubnetMaskFromCidr(_config.PoolCidr);
        _defaultLeaseTime = TimeSpan.FromHours(1);
        _maxLeaseTime = TimeSpan.FromDays(7);

        if (!string.IsNullOrWhiteSpace(_config.NtpServer))
            _ntp = IPAddress.Parse(_config.NtpServer);
        else
            _ntp = null;

        if (!string.IsNullOrWhiteSpace(_config.WebProxyServerUrl))
            _webproxy = _config.WebProxyServerUrl;
        else
            _webproxy = null;
    }

    private static IPAddress ParseSubnetMaskFromCidr(string? cidr)
    {
        if (string.IsNullOrWhiteSpace(cidr))
            throw new ArgumentException("Pool CIDR configuration cannot be null or empty.", nameof(cidr));

        var parts = cidr.Split('/');
        if (parts.Length < 2 || !int.TryParse(parts[1], out int prefix) || prefix < 0 || prefix > 32)
        {
            prefix = 24;
        }

        uint mask = prefix switch
        {
            0 => 0,
            32 => uint.MaxValue,
            _ => uint.MaxValue << (32 - prefix)
        };

        Span<byte> bytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(bytes, mask);
        return new IPAddress(bytes);
    }

    private TimeSpan GetRequestedLeaseTime(DhcpPacket req)
    {
        var opt = req.Options.FirstOrDefault(o => o.Code == 51);
        if (opt != null && opt.Data.Length >= 4)
        {
            uint requestedSeconds = BinaryPrimitives.ReadUInt32BigEndian(opt.Data);
            if (requestedSeconds > 0)
            {
                var requestedSpan = TimeSpan.FromSeconds(requestedSeconds);
                if (requestedSpan > _maxLeaseTime)
                    return _maxLeaseTime;

                return requestedSpan;
            }
        }

        return _defaultLeaseTime;
    }

    private static IPAddress? DetermineRequestedIp(DhcpPacket req)
    {
        var optIp = req.GetRequestedIp();
        if (optIp != null)
            return optIp;

        if (!req.Ciaddr.Equals(IPAddress.Any))
            return req.Ciaddr;

        return null;
    }

    private void LogClientName(DhcpPacket req, PhysicalAddress mac)
    {
        var hostOpt = req.Options.FirstOrDefault(o => o.Code == 12);
        var fqdnOpt = req.Options.FirstOrDefault(o => o.Code == 81);

        string? host = hostOpt != null
            ? Encoding.ASCII.GetString(hostOpt.Data)
            : null;

        string? fqdn = fqdnOpt != null
            ? Encoding.ASCII.GetString(fqdnOpt.Data)
            : null;

        if (!string.IsNullOrEmpty(fqdn))
        {
            _logger.LogTrace("Client {Mac} FQDN: {Fqdn}", mac, fqdn);
        }
        else if (!string.IsNullOrEmpty(host))
        {
            _logger.LogTrace("Client {Mac} hostname: {Host}", mac, host);
        }
        else
        {
            _logger.LogTrace("Client {Mac} did not send hostname", mac);
        }
    }

    public async Task RunAsync(CancellationToken ct)
    {
        _logger.LogInformation("DHCP server listening on {Address}:{Port}",
            _config.ListenAddress, _config.ListenPort);

        while (!ct.IsCancellationRequested)
        {
            UdpReceiveResult result;

            try
            {
                result = await _transport.ReceiveAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading from UDP transport");
                continue;
            }

            _lastClient = result.RemoteEndPoint;

            DhcpPacket req;
            try
            {
                req = DhcpPacketCodec.Parse(result.Buffer);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse incoming DHCP packet from {Endpoint}", result.RemoteEndPoint);
                continue;
            }

            var type = req.GetMessageType();
            var mac = new PhysicalAddress(req.Chaddr.Take(req.Hlen).ToArray());

            switch (type)
            {
                case DhcpMessageType.Discover:
                    LogClientName(req, mac);
                    await HandleDiscoverAsync(req, mac).ConfigureAwait(false);
                    break;

                case DhcpMessageType.Request:
                    LogClientName(req, mac);
                    await HandleRequestAsync(req, mac).ConfigureAwait(false);
                    break;

                case DhcpMessageType.Release:
                    await HandleReleaseAsync(req, mac).ConfigureAwait(false);
                    break;

                case DhcpMessageType.Decline:
                    await HandleDeclineAsync(req, mac).ConfigureAwait(false);
                    break;

                case DhcpMessageType.Inform:
                    LogClientName(req, mac);
                    await HandleInformAsync(req).ConfigureAwait(false);
                    break;

                default:
                    _logger.LogWarning("Unhandled DHCP message type: {Type}", type);
                    break;
            }
        }
    }

    private IPEndPoint DetermineReplyEndpoint(DhcpPacket req)
    {
        if (_testMode && _lastClient is not null)
            return _lastClient;

        if (!req.Giaddr.Equals(IPAddress.Any))
            return new IPEndPoint(req.Giaddr, 67);

        if (!req.Ciaddr.Equals(IPAddress.Any))
            return new IPEndPoint(req.Ciaddr, 68);

        return new IPEndPoint(IPAddress.Broadcast, 68);
    }

    private async Task HandleDiscoverAsync(DhcpPacket req, PhysicalAddress mac)
    {
        _logger.LogTrace("DHCP DISCOVER from {Mac}", mac);

        TimeSpan leaseTime = GetRequestedLeaseTime(req);

        var lease = await _leaseEngine.AllocateWithArpCheckAsync(
            mac,
            leaseTime,
            _arp).ConfigureAwait(false);

        var offer = DhcpPacketCodec.BuildOffer(
            req,
            lease.Ip,
            _serverId,
            _router,
            _dns,
            _subnetMask,
            leaseTime,
            _ntp,
            _webproxy);

        await _transport.SendAsync(offer, offer.Length, DetermineReplyEndpoint(req))
                        .ConfigureAwait(false);

        _logger.LogTrace("Sent OFFER {Ip} to {Mac}", lease.Ip, mac);
    }

    private async Task HandleRequestAsync(DhcpPacket req, PhysicalAddress mac)
    {
        var requestedIp = DetermineRequestedIp(req);
        var serverIdOpt = req.GetServerIdentifier();
        TimeSpan leaseTime = GetRequestedLeaseTime(req);

        _logger.LogTrace("DHCP REQUEST from {Mac} for {Ip}", mac, requestedIp);

        if (serverIdOpt is not null && !serverIdOpt.Equals(_serverId))
        {
            var nak = DhcpPacketCodec.BuildNak(req, _serverId);
            await _transport.SendAsync(nak, nak.Length, DetermineReplyEndpoint(req))
                            .ConfigureAwait(false);

            _metrics.NakSent(new DhcpNakEvent(
                Timestamp: DateTime.UtcNow,
                Mac: mac,
                RequestedIp: requestedIp,
                Reason: "Wrong server identifier"));

            _logger.LogWarning("Sent NAK to {Mac} (wrong server)", mac);
            return;
        }

        if (requestedIp is not null && !_pool.IsInPool(requestedIp))
        {
            var nak = DhcpPacketCodec.BuildNak(req, _serverId);
            await _transport.SendAsync(nak, nak.Length, DetermineReplyEndpoint(req))
                            .ConfigureAwait(false);

            _metrics.NakSent(new DhcpNakEvent(
                Timestamp: DateTime.UtcNow,
                Mac: mac,
                RequestedIp: requestedIp,
                Reason: "Requested IP outside subnet pool"));

            _logger.LogWarning("Sent NAK to {Mac} (requested IP {ReqIp} invalid for subnet)", mac, requestedIp);
            return;
        }

        var lease = await _leaseEngine.AllocateWithArpCheckAsync(
            mac,
            leaseTime,
            _arp).ConfigureAwait(false);

        if (requestedIp is not null && !requestedIp.Equals(lease.Ip))
        {
            var nak = DhcpPacketCodec.BuildNak(req, _serverId);
            await _transport.SendAsync(nak, nak.Length, DetermineReplyEndpoint(req))
                            .ConfigureAwait(false);

            _metrics.NakSent(new DhcpNakEvent(
                Timestamp: DateTime.UtcNow,
                Mac: mac,
                RequestedIp: requestedIp,
                Reason: "Requested IP mismatch"));

            _logger.LogWarning(
                "Sent NAK to {Mac} (requested {ReqIp}, assigned {LeaseIp})",
                mac, requestedIp, lease.Ip);
            return;
        }

        var ack = DhcpPacketCodec.BuildAck(
            req,
            lease.Ip,
            _serverId,
            _router,
            _dns,
            _subnetMask,
            leaseTime,
            _ntp,
            _webproxy);

        await _transport.SendAsync(ack, ack.Length, DetermineReplyEndpoint(req))
                        .ConfigureAwait(false);

        _metrics.LeaseAllocated(new DhcpLeaseAllocatedEvent(
            Timestamp: DateTime.UtcNow,
            ClientIp: lease.Ip,
            Mac: mac,
            ClientName: req.GetHostName() ?? req.GetFqdn(),
            ServerId: _serverId,
            LeaseStart: DateTime.UtcNow,
            LeaseExpiry: DateTime.UtcNow.Add(leaseTime)));

        _logger.LogTrace("Sent ACK {Ip} to {Mac}", lease.Ip, mac);
    }

    private async Task HandleReleaseAsync(DhcpPacket req, PhysicalAddress mac)
    {
        _logger.LogTrace("DHCP RELEASE from {Mac}", mac);
        await _leaseEngine.ReleaseAsync(mac).ConfigureAwait(false);

        _metrics.LeaseReleased(new DhcpLeaseReleasedEvent(
            Timestamp: DateTime.UtcNow,
            Mac: mac,
            ClientIp: req.Ciaddr,
            ClientName: req.GetHostName() ?? req.GetFqdn()));
    }

    private async Task HandleDeclineAsync(DhcpPacket req, PhysicalAddress mac)
    {
        var requestedIp = DetermineRequestedIp(req);
        _logger.LogWarning("DHCP DECLINE from {Mac} for {Ip}", mac, requestedIp);

        await _leaseEngine.ReleaseAsync(mac).ConfigureAwait(false);

        if (requestedIp is not null)
            await _leaseEngine.DeclineAsync(requestedIp).ConfigureAwait(false);

        _metrics.NakSent(new DhcpNakEvent(
            Timestamp: DateTime.UtcNow,
            Mac: mac,
            RequestedIp: requestedIp,
            Reason: "Client declined assigned IP"));
    }

    private async Task HandleInformAsync(DhcpPacket req)
    {
        _logger.LogTrace("DHCP INFORM from client with IP {Ip}", req.Ciaddr);

        var ack = DhcpPacketCodec.BuildInformAck(
            req,
            _serverId,
            _router,
            _dns,
            _subnetMask,
            _ntp,
            _webproxy);

        await _transport.SendAsync(ack, ack.Length, DetermineReplyEndpoint(req))
                        .ConfigureAwait(false);

        _logger.LogTrace("Sent INFORM-ACK to client {Ip}", req.Ciaddr);
    }
}
