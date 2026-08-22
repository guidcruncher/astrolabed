// File: src/Astrolabed.Dns/Services/ClientNameResolver.cs
using System.Text;

using Astrolabed.Data.Repositories;
using Astrolabed.Dns.Models;
using Astrolabed.Dns.Resolvers;
using Astrolabed.Dns.Serialization;
using Astrolabed.Dns.Upstream;

using Microsoft.Extensions.Logging;

namespace Astrolabed.Dns.Services;

public sealed class ClientNameResolver : IClientNameResolver
{
    private readonly IPtrResolver _ptrResolver;
    private readonly IUpstreamClientFactory _upstreamClientFactory;
    private readonly ILogger<ClientNameResolver> _logger;
    private readonly IDiscoveredLanDeviceRepository _repository;

    public ClientNameResolver(
        IPtrResolver ptrResolver,
        IUpstreamClientFactory upstreamClientFactory,
    IDiscoveredLanDeviceRepository repository,
        ILogger<ClientNameResolver> logger)
    {
        _ptrResolver = ptrResolver;
        _repository = repository;
        _upstreamClientFactory = upstreamClientFactory;
        _logger = logger;
    }

    public async Task<string> ResolveClientNameAsync(string question, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            return string.Empty;
        }

        // 1. Static Overrides Match
        if (_ptrResolver.TryResolvePtr(question, out var staticDomain) && !string.IsNullOrEmpty(staticDomain))
        {
            return staticDomain;
        }

	// 2. Built-in DHCP lease Match

        // 3. ARP scan match
        var device = await _repository.GetByPtrAddressAsync(question, ct);
        if (device != null)
        {
            if (!string.IsNullOrEmpty(device.HostName))
            {
                return device.HostName;
            }
        }

        // 3. Conditional PTR Subnet Forwarding Match
        if (_ptrResolver is PtrResolver concreteResolver &&
            concreteResolver.TryGetConditionalForwarder(question, out var targetResolverIp) &&
            targetResolverIp is not null)
        {
            try
            {
                var rawQueryPacket = QuestionBuilder.BuildPtrQuery(question);

                var upstreamMessage = await _upstreamClientFactory
                    .ExecuteQueryAsync(targetResolverIp.ToString(), rawQueryPacket, ct)
                    .ConfigureAwait(false);

                if (upstreamMessage?.Answers is { Count: > 0 })
                {
                    foreach (var answer in upstreamMessage.Answers)
                    {
                        if (answer.Type == DnsType.PTR && answer.Data is { Length: > 0 })
                        {
                            var hostname = DecodeDomainName(answer.Data);
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
                _logger.LogWarning(ex, "Failed to resolve client name via conditional forwarder for query {Question}", question);
            }
        }

        return string.Empty;
    }

    private static string DecodeDomainName(ReadOnlySpan<byte> data)
    {
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
}
