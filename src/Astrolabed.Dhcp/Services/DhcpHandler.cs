using System.Net;
using System.Text;

using Astrolabed.Data.Repositories;
using Astrolabed.Dhcp.Options;
using Astrolabed.Dhcp.Protocol;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Dhcp.Services;

/// <summary>
/// Core handler service responsible for processing incoming DHCP client DISCOVER, REQUEST, 
/// and RELEASE messages and generating corresponding OFFER, ACK, and NAK replies.
/// </summary>
/// <param name="leaseRepository">Database repository for evaluating and persisting IP lease allocations.</param>
/// <param name="options">Monitored options instance holding server configuration settings.</param>
/// <param name="logger">Structured logger instance for recording packet handling state transitions.</param>
public sealed partial class DhcpHandler(
    IDhcpLeaseRepository leaseRepository,
    IOptionsMonitor<DhcpServerOptions> options,
    ILogger<DhcpHandler> logger) : IDhcpHandler
{
    private readonly IDhcpLeaseRepository _leaseRepository = leaseRepository ?? throw new ArgumentNullException(nameof(leaseRepository));
    private readonly IOptionsMonitor<DhcpServerOptions> _options = options ?? throw new ArgumentNullException(nameof(options));
    private readonly ILogger<DhcpHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task<DhcpMessage?> ProcessMessageAsync(DhcpMessage request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        DhcpMessageType? messageType = request.GetMessageType();
        if (messageType is null)
        {
            LogMissingMessageTypeWarning(_logger);
            return null;
        }

        DhcpServerOptions config = _options.CurrentValue;

        return messageType switch
        {
            DhcpMessageType.Discover => await HandleDiscoverAsync(request, config, cancellationToken).ConfigureAwait(false),
            DhcpMessageType.Request => await HandleRequestAsync(request, config, cancellationToken).ConfigureAwait(false),
            DhcpMessageType.Release => await HandleReleaseAsync(request, cancellationToken).ConfigureAwait(false),
            _ => null
        };
    }

    private async Task<DhcpMessage> HandleDiscoverAsync(DhcpMessage request, DhcpServerOptions config, CancellationToken cancellationToken)
    {
        string macAddress = GetMacAddressString(request.ClientHardwareAddress);
        string clientId = GetClientId(request, macAddress);
        string clientName = GetClientName(request);

        LogProcessingDiscover(_logger, clientId, clientName, macAddress);

        IPAddress? requestedIp = GetRequestedIp(request);
        IPAddress selectedIp = config.GetStartIpAddress();

        if (requestedIp is not null && config.IsIpInPool(requestedIp) && await _leaseRepository.IsIpAvailableAsync(requestedIp, clientId, cancellationToken).ConfigureAwait(false))
        {
            selectedIp = requestedIp;
        }
        else
        {
            var existingLease = await _leaseRepository.GetLeaseByClientIdOrMacAsync(clientId, macAddress, cancellationToken).ConfigureAwait(false);
            if (existingLease is not null && config.IsIpInPool(existingLease.IpAddress))
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
            cancellationToken).ConfigureAwait(false);

        return CreateReply(request, config, DhcpMessageType.Offer, lease.IpAddress);
    }

    private async Task<DhcpMessage> HandleRequestAsync(DhcpMessage request, DhcpServerOptions config, CancellationToken cancellationToken)
    {
        string macAddress = GetMacAddressString(request.ClientHardwareAddress);
        string clientId = GetClientId(request, macAddress);
        string clientName = GetClientName(request);

        LogProcessingRequest(_logger, clientId, clientName, macAddress);

        IPAddress targetIp = GetRequestedIp(request) ?? request.ClientIpAddress;

        if (targetIp.Equals(IPAddress.Any) || !config.IsIpInPool(targetIp))
        {
            LogRejectingRequestOutsidePool(_logger, targetIp);
            return CreateNakReply(request, config, "Requested IP address is outside configured pool range.");
        }

        bool isAvailable = await _leaseRepository.IsIpAvailableAsync(targetIp, clientId, cancellationToken).ConfigureAwait(false);
        if (!isAvailable)
        {
            LogRejectingRequestAlreadyLeased(_logger, targetIp);
            return CreateNakReply(request, config, "Requested IP address is already leased to another client.");
        }

        var lease = await _leaseRepository.AllocateOrUpdateLeaseAsync(
            clientId,
            clientName,
            macAddress,
            targetIp,
            TimeSpan.FromSeconds(config.LeaseTimeSeconds),
            cancellationToken).ConfigureAwait(false);

        return CreateReply(request, config, DhcpMessageType.Ack, lease.IpAddress);
    }

    private async Task<DhcpMessage?> HandleReleaseAsync(DhcpMessage request, CancellationToken cancellationToken)
    {
        string macAddress = GetMacAddressString(request.ClientHardwareAddress);
        string clientId = GetClientId(request, macAddress);

        LogProcessingRelease(_logger, clientId, macAddress);
        await _leaseRepository.ReleaseLeaseAsync(clientId, macAddress, cancellationToken).ConfigureAwait(false);
        return null;
    }

    private static string GetMacAddressString(ReadOnlySpan<byte> hardwareAddress)
    {
        int length = Math.Min(hardwareAddress.Length, 6);
        return Convert.ToHexStringLower(hardwareAddress[..length]);
    }

    private static string GetClientId(DhcpMessage request, string fallbackMac)
    {
        DhcpOption? clientIdOption = request.Options.FirstOrDefault(o => o.Code == DhcpOptionCode.ClientIdentifier);
        if (clientIdOption is not null && clientIdOption.Data.Length > 0)
        {
            return Convert.ToHexStringLower(clientIdOption.Data);
        }

        return fallbackMac;
    }

    private static string GetClientName(DhcpMessage request)
    {
        DhcpOption? hostNameOption = request.Options.FirstOrDefault(o => o.Code == DhcpOptionCode.HostName);
        if (hostNameOption is not null && hostNameOption.Data.Length > 0)
        {
            return Encoding.ASCII.GetString(hostNameOption.Data).TrimEnd('\0');
        }

        return string.Empty;
    }

    private static IPAddress? GetRequestedIp(DhcpMessage request)
    {
        DhcpOption? requestedIpOpt = request.Options.FirstOrDefault(o => o.Code == DhcpOptionCode.RequestedIpAddress);
        if (requestedIpOpt is not null && requestedIpOpt.Data.Length == 4)
        {
            return new IPAddress(requestedIpOpt.Data);
        }

        return null;
    }

    private static HashSet<DhcpOptionCode> GetRequestedOptionCodes(DhcpMessage request)
    {
        var requestedCodes = new HashSet<DhcpOptionCode>();
        DhcpOption? prlOpt = request.Options.FirstOrDefault(o => o.Code == DhcpOptionCode.ParameterRequestList);

        if (prlOpt is not null)
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

        HashSet<DhcpOptionCode> requestedCodes = GetRequestedOptionCodes(request);
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

    [LoggerMessage(
        EventId = 201,
        Level = LogLevel.Warning,
        Message = "Received DHCP message without MessageType option.")]
    private static partial void LogMissingMessageTypeWarning(ILogger logger);

    [LoggerMessage(
        EventId = 202,
        Level = LogLevel.Information,
        Message = "Processing DHCP DISCOVER - ClientID: {ClientId}, Name: {ClientName}, MAC: {Mac}")]
    private static partial void LogProcessingDiscover(ILogger logger, string clientId, string clientName, string mac);

    [LoggerMessage(
        EventId = 203,
        Level = LogLevel.Information,
        Message = "Processing DHCP REQUEST - ClientID: {ClientId}, Name: {ClientName}, MAC: {Mac}")]
    private static partial void LogProcessingRequest(ILogger logger, string clientId, string clientName, string mac);

    [LoggerMessage(
        EventId = 204,
        Level = LogLevel.Information,
        Message = "Processing DHCP RELEASE - ClientID: {ClientId}, MAC: {Mac}")]
    private static partial void LogProcessingRelease(ILogger logger, string clientId, string mac);

    [LoggerMessage(
        EventId = 205,
        Level = LogLevel.Warning,
        Message = "Rejecting DHCP REQUEST for IP {TargetIp} - Outside allocated pool.")]
    private static partial void LogRejectingRequestOutsidePool(ILogger logger, IPAddress targetIp);

    [LoggerMessage(
        EventId = 206,
        Level = LogLevel.Warning,
        Message = "Rejecting DHCP REQUEST for IP {TargetIp} - Assigned to another client.")]
    private static partial void LogRejectingRequestAlreadyLeased(ILogger logger, IPAddress targetIp);
}
