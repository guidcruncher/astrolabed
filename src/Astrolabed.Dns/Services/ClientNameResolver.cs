using System.Net;
using System.Text;

using Astrolabed.Data.Repositories;
using Astrolabed.Dns.Models;
using Astrolabed.Dns.Resolvers;
using Astrolabed.Dns.Serialization;
using Astrolabed.Dns.Upstream;

using Microsoft.Extensions.Logging;

namespace Astrolabed.Dns.Services;

/// <summary>
/// Asynchronously resolves reverse DNS questions to client hostnames via local static entries, ARP tables, or conditional forwarding.
/// </summary>
/// <param name="ptrResolver">PTR record resolver service.</param>
/// <param name="upstreamClientFactory">Upstream DNS query execution factory.</param>
/// <param name="repository">LAN device repository for ARP/DHCP device lookups.</param>
/// <param name="logger">Structured logger instance.</param>
public sealed partial class ClientNameResolver(
    IPtrResolver ptrResolver,
    IUpstreamClientFactory upstreamClientFactory,
    IDiscoveredLanDeviceRepository repository,
    ILogger<ClientNameResolver> logger) : IClientNameResolver
{
    private readonly IPtrResolver _ptrResolver = ptrResolver ?? throw new ArgumentNullException(nameof(ptrResolver));
    private readonly IUpstreamClientFactory _upstreamClientFactory = upstreamClientFactory ?? throw new ArgumentNullException(nameof(upstreamClientFactory));
    private readonly IDiscoveredLanDeviceRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    private readonly ILogger<ClientNameResolver> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task<string> ResolveClientNameAsync(string question, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            return string.Empty;
        }

        // 1. Static Overrides Match
        if (_ptrResolver.TryResolvePtr(question, out string? staticDomain) && !string.IsNullOrEmpty(staticDomain))
        {
            return staticDomain;
        }

        // 2. ARP / Discovered LAN Device Match
        var device = await _repository.GetByPtrAddressAsync(question, ct).ConfigureAwait(false);
        if (device is not null && !string.IsNullOrEmpty(device.HostName))
        {
            return device.HostName;
        }

        // 3. Conditional PTR Subnet Forwarding Match
        if (_ptrResolver is PtrResolver concreteResolver &&
            concreteResolver.TryGetConditionalForwarder(question, out IPAddress? targetResolverIp) &&
            targetResolverIp is not null)
        {
            try
            {
                byte[] rawQueryPacket = QuestionBuilder.BuildPtrQuery(question);

                var upstreamMessage = await _upstreamClientFactory
                    .ExecuteQueryAsync(targetResolverIp.ToString(), rawQueryPacket, ct)
                    .ConfigureAwait(false);

                if (upstreamMessage?.Answers is { Count: > 0 })
                {
                    foreach (DnsAnswer answer in upstreamMessage.Answers)
                    {
                        if (answer.Type == DnsType.PTR && answer.Data is { Length: > 0 })
                        {
                            string hostname = DecodeDomainName(answer.Data);
                            if (!string.IsNullOrEmpty(hostname))
                            {
                                return hostname;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogConditionalForwarderFailed(_logger, ex, question);
            }
        }

        return string.Empty;
    }

    private static string DecodeDomainName(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        int offset = 0;

        while (offset < data.Length)
        {
            byte length = data[offset++];
            if (length == 0)
            {
                break;
            }

            // Stop processing if compression pointer (0xC0) is encountered
            if ((length & 0xC0) == 0xC0)
            {
                break;
            }

            if (offset + length > data.Length)
            {
                break;
            }

            if (sb.Length > 0)
            {
                sb.Append('.');
            }

            sb.Append(Encoding.ASCII.GetString(data.Slice(offset, length)));
            offset += length;
        }

        return sb.ToString();
    }

    [LoggerMessage(
        EventId = 801,
        Level = LogLevel.Warning,
        Message = "Failed to resolve client name via conditional forwarder for query {Question}")]
    private static partial void LogConditionalForwarderFailed(ILogger logger, Exception exception, string question);
}
