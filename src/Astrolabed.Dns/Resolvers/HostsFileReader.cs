// File: src/Astrolabed.Dns/Resolvers/HostsFileReader.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Astrolabed.Dns.Resolvers;

public partial class HostsFileReader : IHostsFileReader
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HostsFileReader> _logger;

    // RFC 952 & RFC 1123 compliant hostname regex:
    // RFC 1123 allows leading digits; labels up to 63 chars; max total length 255 chars.
    [GeneratedRegex(@"^(?=.{1,255}$)(?:[a-zA-Z0-9](?:[a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?\.)*(?:[a-zA-Z0-9](?:[a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?)$", RegexOptions.Compiled)]
    private static partial Regex HostnameRegex();

    public HostsFileReader(HttpClient httpClient, ILogger<HostsFileReader> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyDictionary<string, List<IPAddress>>> ReadHostsAsync(string sourceLocation, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceLocation);

        string content;
        if (sourceLocation.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            sourceLocation.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Downloading hosts file from Web source: {Url}", sourceLocation);
            content = await _httpClient.GetStringAsync(sourceLocation, ct).ConfigureAwait(false);
        }
        else if (sourceLocation.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
        {
            var uri = new Uri(sourceLocation);
            _logger.LogInformation("Reading hosts file from File URI: {Path}", uri.LocalPath);
            content = await File.ReadAllTextAsync(uri.LocalPath, ct).ConfigureAwait(false);
        }
        else
        {
            _logger.LogInformation("Reading hosts file from local filesystem path: {Path}", sourceLocation);
            content = await File.ReadAllTextAsync(sourceLocation, ct).ConfigureAwait(false);
        }

        return ParseHostsContent(content);
    }

    private IReadOnlyDictionary<string, List<IPAddress>> ParseHostsContent(string content)
    {
        var map = new Dictionary<string, List<IPAddress>>(StringComparer.OrdinalIgnoreCase);

        using var reader = new StringReader(content);
        string? line;

        while ((line = reader.ReadLine()) != null)
        {
            // Strip comments starting with '#'
            int commentIdx = line.IndexOf('#');
            if (commentIdx >= 0)
            {
                line = line[..commentIdx];
            }

            line = line.Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            // Space and Tab are valid delimiters according to RFC 952
            string[] tokens = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length < 2)
            {
                continue;
            }

            // First token is the IP Address
            if (!IPAddress.TryParse(tokens[0], out var ipAddress))
            {
                _logger.LogWarning("Skipping invalid IP address in hosts entry: {Token}", tokens[0]);
                continue;
            }

            // Remaining tokens are Canonical Hostname and optional Aliases (RFC 952 / RFC 1123)
            for (int i = 1; i < tokens.Length; i++)
            {
                string hostname = tokens[i].TrimEnd('.'); // Strip trailing DNS root dot if present

                if (!IsValidHostname(hostname))
                {
                    _logger.LogWarning("Skipping invalid hostname in hosts entry: {Hostname}", hostname);
                    continue;
                }

                if (!map.TryGetValue(hostname, out var ipList))
                {
                    ipList = new List<IPAddress>();
                    map[hostname] = ipList;
                }

                if (!ipList.Contains(ipAddress))
                {
                    ipList.Add(ipAddress);
                }
            }
        }

        return map;
    }

    private static bool IsValidHostname(string hostname)
    {
        if (string.IsNullOrWhiteSpace(hostname) || hostname.Length > 255)
        {
            return false;
        }

        return HostnameRegex().IsMatch(hostname);
    }
}
