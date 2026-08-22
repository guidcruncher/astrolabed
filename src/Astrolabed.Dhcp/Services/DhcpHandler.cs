using System.Net;
using Astrolabed.Data;
using Astrolabed.Dhcp.Options;
using Astrolabed.Dhcp.Protocol;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Dhcp.Services;

public class DhcpHandler : IDhcpHandler
{
    private readonly IDhcpLeaseRepository _leaseRepository;
    private readonly IOptionsMonitor<DhcpServerOptions> _options;
    private readonly ILogger<DhcpHandler> _logger;

    public DhcpHandler(
        IDhcpLeaseRepository leaseRepository,
        IOptionsMonitor<DhcpServerOptions> options,
        ILogger<DhcpHandler> logger)
    {
        _leaseRepository = leaseRepository;
        _options = options;
        _logger = logger;
    }

    public async Task<DhcpMessage?> ProcessMessageAsync(DhcpMessage request, CancellationToken cancellationToken = default)
    {
        var messageType = request.GetMessageType();
        if (messageType == null)
        {
            _logger.LogWarning("Received DHCP message without MessageType option.");
            return null;
        }

        var config = _options.CurrentValue;

        return messageType switch
        {
            DhcpMessageType.Discover => await HandleDiscoverAsync(request, config, cancellationToken),
            DhcpMessageType.Request => await HandleRequestAsync(request, config, cancellationToken),
            DhcpMessageType.Release => await HandleReleaseAsync(request, cancellationToken),
            _ => null
        };
    }

    private async Task<DhcpMessage> HandleDiscoverAsync(DhcpMessage request, DhcpServerOptions config, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing DHCP DISCOVER for MAC: {Mac}", Convert.ToHexString(request.ClientHardwareAddress));

        var allocatedIp = config.GetStartIpAddress();
        var lease = await _leaseRepository.AllocateOrUpdateLeaseAsync(
            request.ClientHardwareAddress,
            allocatedIp,
            TimeSpan.FromSeconds(config.LeaseTimeSeconds),
            cancellationToken);

        return CreateReply(request, config, DhcpMessageType.Offer, lease.IpAddress);
    }

    private async Task<DhcpMessage> HandleRequestAsync(DhcpMessage request, DhcpServerOptions config, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing DHCP REQUEST for MAC: {Mac}", Convert.ToHexString(request.ClientHardwareAddress));

        var existingLease = await _leaseRepository.GetLeaseByMacAsync(request.ClientHardwareAddress, cancellationToken);
        var offeredIp = existingLease?.IpAddress ?? config.GetStartIpAddress();

        var lease = await _leaseRepository.AllocateOrUpdateLeaseAsync(
            request.ClientHardwareAddress,
            offeredIp,
            TimeSpan.FromSeconds(config.LeaseTimeSeconds),
            cancellationToken);

        return CreateReply(request, config, DhcpMessageType.Ack, lease.IpAddress);
    }

    private async Task<DhcpMessage?> HandleReleaseAsync(DhcpMessage request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing DHCP RELEASE for MAC: {Mac}", Convert.ToHexString(request.ClientHardwareAddress));
        await _leaseRepository.ReleaseLeaseAsync(request.ClientHardwareAddress, cancellationToken);
        return null;
    }

    private static DhcpMessage CreateReply(DhcpMessage request, DhcpServerOptions config, DhcpMessageType replyType, IPAddress assignedIp)
    {
        var reply = new DhcpMessage
        {
            Operation = DhcpOpCode.BootReply,
            HardwareType = request.HardwareType,
            HardwareAddressLength = request.HardwareAddressLength,
            TransactionId = request.TransactionId,
            Flags = request.Flags,
            ClientHardwareAddress = request.ClientHardwareAddress,
            YourIpAddress = assignedIp,
            ServerIpAddress = config.GetServerIp()
        };

        reply.Options.Add(DhcpOption.CreateByte(DhcpOptionCode.DhcpMessageType, (byte)replyType));
        reply.Options.Add(DhcpOption.CreateIpAddress(DhcpOptionCode.ServerIdentifier, config.GetServerIp()));
        reply.Options.Add(DhcpOption.CreateIpAddress(DhcpOptionCode.SubnetMask, config.GetSubnetMask()));
        reply.Options.Add(DhcpOption.CreateIpAddress(DhcpOptionCode.Router, config.GetRouter()));
        reply.Options.Add(DhcpOption.CreateIpAddress(DhcpOptionCode.DnsServer, config.GetDnsServer()));
        reply.Options.Add(DhcpOption.CreateInt32(DhcpOptionCode.AddressLeaseTime, config.LeaseTimeSeconds));

        return reply;
    }
}
