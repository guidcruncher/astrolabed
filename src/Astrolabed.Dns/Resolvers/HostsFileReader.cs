// File: src/Astrolabed.Dns/Resolvers/HostsFileReader.cs
using System.Net;
using System.Text.RegularExpressions;

using Microsoft.Extensions.Logging;

namespace Astrolabed.Dns.Resolvers;

public partial class HostsFileReader : IHostsFileReader
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HostsFileReader> _logger;

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
        string resolvedPath = sourceLocation;

        if (sourceLocation.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            sourceLocation.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Downloading hosts file from Web source: {Url}", sourceLocation);
            content = await _httpClient.GetStringAsync(sourceLocation, ct).ConfigureAwait(false);
        }
        else
        {
            // Resolve file:// or standard local relative paths cleanly
            if (sourceLocation.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            {
                resolvedPath = sourceLocation["file://".Length..];
            }

            resolvedPath = Path.GetFullPath(resolvedPath);
            _logger.LogInformation("Reading hosts file from filesystem path: {Path}", resolvedPath);
            content = await File.ReadAllTextAsync(resolvedPath, ct).ConfigureAwait(false);
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

            string[] tokens = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length < 2)
            {
                continue;
            }

            if (!IPAddress.TryParse(tokens[0], out var ipAddress))
            {
                _logger.LogWarning("Skipping invalid IP address in hosts entry: {Token}", tokens[0]);
                continue;
            }

            for (int i = 1; i < tokens.Length; i++)
            {
                string hostname = tokens[i].TrimEnd('.');

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
