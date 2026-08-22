namespace Astrolabed.Dhcp.Services;

using System.Net;
using System.Text;

using Astrolabed.Data.Repositories;
using Astrolabed.Dhcp.Options;
using Astrolabed.Dhcp.Protocol;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
        string macAddress = GetMacAddressString(request.ClientHardwareAddress);
        string clientId = GetClientId(request, macAddress);
        string clientName = GetClientName(request);

        _logger.LogInformation("Processing DHCP DISCOVER - ClientID: {ClientId}, Name: {ClientName}, MAC: {Mac}", clientId, clientName, macAddress);

        IPAddress? requestedIp = GetRequestedIp(request);
        IPAddress selectedIp = config.GetStartIpAddress();

        if (requestedIp != null && config.IsIpInPool(requestedIp) && await _leaseRepository.IsIpAvailableAsync(requestedIp, clientId, cancellationToken))
        {
            selectedIp = requestedIp;
        }
        else
        {
            var existingLease = await _leaseRepository.GetLeaseByClientIdOrMacAsync(clientId, macAddress, cancellationToken);
            if (existingLease != null && config.IsIpInPool(existingLease.IpAddress))
            {
                selectedIp = existingLease.IpAddress;
            }
        }

        var lease = await _leaseRepository.AllocateOrUpdateLeaseAsync(
            clientId,
            clientName,
            macAddress,
            selectedIp,
            TimeSpan.FromSeconds(config.LeaseTimeSeconds),
            cancellationToken);

        return CreateReply(request, config, DhcpMessageType.Offer, lease.IpAddress);
    }

    private async Task<DhcpMessage> HandleRequestAsync(DhcpMessage request, DhcpServerOptions config, CancellationToken cancellationToken)
    {
        string macAddress = GetMacAddressString(request.ClientHardwareAddress);
        string clientId = GetClientId(request, macAddress);
        string clientName = GetClientName(request);

        _logger.LogInformation("Processing DHCP REQUEST - ClientID: {ClientId}, Name: {ClientName}, MAC: {Mac}", clientId, clientName, macAddress);

        IPAddress targetIp = GetRequestedIp(request) ?? request.ClientIpAddress;

        if (targetIp.Equals(IPAddress.Any) || !config.IsIpInPool(targetIp))
        {
            _logger.LogWarning("Rejecting DHCP REQUEST for IP {TargetIp} - Outside allocated pool.", targetIp);
            return CreateNakReply(request, config, "Requested IP address is outside configured pool range.");
        }

        bool isAvailable = await _leaseRepository.IsIpAvailableAsync(targetIp, clientId, cancellationToken);
        if (!isAvailable)
        {
            _logger.LogWarning("Rejecting DHCP REQUEST for IP {TargetIp} - Assigned to another client.", targetIp);
            return CreateNakReply(request, config, "Requested IP address is already leased to another client.");
        }

        var lease = await _leaseRepository.AllocateOrUpdateLeaseAsync(
            clientId,
            clientName,
            macAddress,
            targetIp,
            TimeSpan.FromSeconds(config.LeaseTimeSeconds),
            cancellationToken);

        return CreateReply(request, config, DhcpMessageType.Ack, lease.IpAddress);
    }

    private async Task<DhcpMessage?> HandleReleaseAsync(DhcpMessage request, CancellationToken cancellationToken)
    {
        string macAddress = GetMacAddressString(request.ClientHardwareAddress);
        string clientId = GetClientId(request, macAddress);

        _logger.LogInformation("Processing DHCP RELEASE - ClientID: {ClientId}, MAC: {Mac}", clientId, macAddress);
        await _leaseRepository.ReleaseLeaseAsync(clientId, macAddress, cancellationToken);
        return null;
    }

    private static string GetMacAddressString(byte[] hardwareAddress)
    {
        return Convert.ToHexString(hardwareAddress, 0, Math.Min(hardwareAddress.Length, 6));
    }

    private static string GetClientId(DhcpMessage request, string fallbackMac)
    {
        var clientIdOption = request.Options.FirstOrDefault(o => o.Code == DhcpOptionCode.ClientIdentifier);
        if (clientIdOption != null && clientIdOption.Data.Length > 0)
        {
            return Convert.ToHexString(clientIdOption.Data);
        }
        return fallbackMac;
    }

    private static string GetClientName(DhcpMessage request)
    {
        var hostNameOption = request.Options.FirstOrDefault(o => o.Code == DhcpOptionCode.HostName);
        if (hostNameOption != null && hostNameOption.Data.Length > 0)
        {
            return Encoding.ASCII.GetString(hostNameOption.Data).TrimEnd('\0');
        }
        return string.Empty;
    }

    private static IPAddress? GetRequestedIp(DhcpMessage request)
    {
        var requestedIpOpt = request.Options.FirstOrDefault(o => o.Code == DhcpOptionCode.RequestedIpAddress);
        if (requestedIpOpt != null && requestedIpOpt.Data.Length == 4)
        {
            return new IPAddress(requestedIpOpt.Data);
        }
        return null;
    }

    private static HashSet<DhcpOptionCode> GetRequestedOptionCodes(DhcpMessage request)
    {
        var requestedCodes = new HashSet<DhcpOptionCode>();
        var prlOpt = request.Options.FirstOrDefault(o => o.Code == DhcpOptionCode.ParameterRequestList);
        if (prlOpt != null)
        {
            foreach (byte code in prlOpt.Data)
            {
                requestedCodes.Add((DhcpOptionCode)code);
            }
        }
        return requestedCodes;
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

        var requestedCodes = GetRequestedOptionCodes(request);
        bool hasParameterList = requestedCodes.Count > 0;

        reply.Options.Add(DhcpOption.CreateByte(DhcpOptionCode.DhcpMessageType, (byte)replyType));
        reply.Options.Add(DhcpOption.CreateIpAddress(DhcpOptionCode.ServerIdentifier, config.GetServerIp()));
        reply.Options.Add(DhcpOption.CreateInt32(DhcpOptionCode.AddressLeaseTime, config.LeaseTimeSeconds));

        if (!hasParameterList || requestedCodes.Contains(DhcpOptionCode.SubnetMask))
        {
            reply.Options.Add(DhcpOption.CreateIpAddress(DhcpOptionCode.SubnetMask, config.GetSubnetMask()));
        }

        if (!hasParameterList || requestedCodes.Contains(DhcpOptionCode.Router))
        {
            reply.Options.Add(DhcpOption.CreateIpAddress(DhcpOptionCode.Router, config.GetRouter()));
        }

        if (!hasParameterList || requestedCodes.Contains(DhcpOptionCode.DnsServer))
        {
            reply.Options.Add(DhcpOption.CreateIpAddress(DhcpOptionCode.DnsServer, config.GetDnsServer()));
        }

        if ((!hasParameterList || requestedCodes.Contains(DhcpOptionCode.NtpServer)) && !string.IsNullOrWhiteSpace(config.NtpServer))
        {
            reply.Options.Add(DhcpOption.CreateIpAddress(DhcpOptionCode.NtpServer, config.GetNtpServer()));
        }

        if ((!hasParameterList || requestedCodes.Contains(DhcpOptionCode.DomainName)) && !string.IsNullOrWhiteSpace(config.DomainName))
        {
            reply.Options.Add(new DhcpOption(DhcpOptionCode.DomainName, Encoding.ASCII.GetBytes(config.DomainName)));
        }

        int t1Seconds = (int)(config.LeaseTimeSeconds * 0.5);
        int t2Seconds = (int)(config.LeaseTimeSeconds * 0.875);

        if (!hasParameterList || requestedCodes.Contains(DhcpOptionCode.RenewalTimeValue))
        {
            reply.Options.Add(DhcpOption.CreateInt32(DhcpOptionCode.RenewalTimeValue, t1Seconds));
        }

        if (!hasParameterList || requestedCodes.Contains(DhcpOptionCode.RebindingTimeValue))
        {
            reply.Options.Add(DhcpOption.CreateInt32(DhcpOptionCode.RebindingTimeValue, t2Seconds));
        }

        return reply;
    }

    private static DhcpMessage CreateNakReply(DhcpMessage request, DhcpServerOptions config, string message)
    {
        var nak = new DhcpMessage
        {
            Operation = DhcpOpCode.BootReply,
            HardwareType = request.HardwareType,
            HardwareAddressLength = request.HardwareAddressLength,
            TransactionId = request.TransactionId,
            Flags = request.Flags,
            ClientHardwareAddress = request.ClientHardwareAddress,
            YourIpAddress = IPAddress.Any,
            ServerIpAddress = config.GetServerIp()
        };

        nak.Options.Add(DhcpOption.CreateByte(DhcpOptionCode.DhcpMessageType, (byte)DhcpMessageType.Nak));
        nak.Options.Add(DhcpOption.CreateIpAddress(DhcpOptionCode.ServerIdentifier, config.GetServerIp()));

        if (!string.IsNullOrWhiteSpace(message))
        {
            nak.Options.Add(new DhcpOption(DhcpOptionCode.Message, Encoding.ASCII.GetBytes(message)));
        }

        return nak;
    }
}
