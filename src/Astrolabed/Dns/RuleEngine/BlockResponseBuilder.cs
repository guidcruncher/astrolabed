using System;
using System.Net;
using Astrolabed.Dns.Core;
using Microsoft.Extensions.Options;

namespace Astrolabed.Dns.RuleEngine;

internal sealed class BlockResponseBuilder
{
    private readonly DnsForwarderOptions _options;

    private const ushort EdeCodeBlocked = 15;
    private const ushort EdeCodeFiltered = 17;

    public BlockResponseBuilder(IOptions<DnsForwarderOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
    }

    public byte[] BuildBlockResponse(byte[] rawRequest, ReadOnlySpan<byte> upstreamEdeBytes = default)
    {
        if (rawRequest == null || rawRequest.Length < 12)
        {
            return DnsResponseBuilder.BuildServfail(rawRequest ?? Array.Empty<byte>());
        }

        if (!upstreamEdeBytes.IsEmpty)
        {
            byte[] baseResp = DnsResponseBuilder.BuildRcodeResponse(rawRequest, 2); // SERVFAIL base
            return EdeForwarder.AttachUpstreamEde(baseResp, upstreamEdeBytes);
        }

        string mode = _options.BlockResponse?.Mode ?? "NxDomain";

        return mode.ToLowerInvariant() switch
        {
            "nxdomain" => BuildRcodeResponseWithEde(rawRequest, 3, EdeCodeBlocked),
            "refused" => BuildRcodeResponseWithEde(rawRequest, 5, EdeCodeBlocked),
            "filtered" => BuildRcodeResponseWithEde(rawRequest, 5, EdeCodeFiltered),
            "nodata" => BuildRcodeResponseWithEde(rawRequest, 0, EdeCodeBlocked),
            "servfail" => DnsResponseBuilder.BuildServfail(rawRequest),
            "zeroip" => BuildZeroIpResponse(rawRequest),
            "customip" => BuildCustomIpResponse(rawRequest),
            _ => BuildRcodeResponseWithEde(rawRequest, 3, EdeCodeBlocked)
        };
    }

    public static byte[] BuildServfail(byte[] rawRequest)
    {
        return DnsResponseBuilder.BuildServfail(rawRequest);
    }

    private static byte[] BuildRcodeResponseWithEde(byte[] rawRequest, int rcode, ushort edeCode)
    {
        byte[] baseResponse = DnsResponseBuilder.BuildRcodeResponse(rawRequest, rcode);
        if (baseResponse.Length < 12)
        {
            return baseResponse;
        }

        return EdeForwarder.AttachEdeOption(baseResponse, edeCode);
    }

    private byte[] BuildZeroIpResponse(byte[] rawRequest)
    {
        var msg = DnsMessage.TryParse(rawRequest);
        if (msg == null)
        {
            return BuildRcodeResponseWithEde(rawRequest, 3, EdeCodeBlocked);
        }

        string qType = msg.QuestionType;

        if (string.Equals(qType, "A", StringComparison.OrdinalIgnoreCase) || qType == "1")
        {
            return DnsResponseBuilder.BuildStaticIpResponse(rawRequest, IPAddress.Any);
        }

        if (string.Equals(qType, "AAAA", StringComparison.OrdinalIgnoreCase) || qType == "28")
        {
            return DnsResponseBuilder.BuildStaticIpResponse(rawRequest, IPAddress.IPv6Any);
        }

        return BuildRcodeResponseWithEde(rawRequest, 3, EdeCodeBlocked);
    }

    private byte[] BuildCustomIpResponse(byte[] rawRequest)
    {
        var msg = DnsMessage.TryParse(rawRequest);
        if (msg == null)
        {
            return BuildRcodeResponseWithEde(rawRequest, 3, EdeCodeBlocked);
        }

        string customIp = _options.BlockResponse?.StaticIp ?? "0.0.0.0";
        if (!IPAddress.TryParse(customIp, out var ip))
        {
            ip = IPAddress.Any;
        }

        string qType = msg.QuestionType;

        if ((string.Equals(qType, "A", StringComparison.OrdinalIgnoreCase) || qType == "1") &&
            ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            return DnsResponseBuilder.BuildStaticIpResponse(rawRequest, ip);
        }

        if ((string.Equals(qType, "AAAA", StringComparison.OrdinalIgnoreCase) || qType == "28") &&
            ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            return DnsResponseBuilder.BuildStaticIpResponse(rawRequest, ip);
        }

        return BuildRcodeResponseWithEde(rawRequest, 3, EdeCodeBlocked);
    }
}
