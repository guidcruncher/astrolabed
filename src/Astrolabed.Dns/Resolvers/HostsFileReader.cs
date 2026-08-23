using System.Net;
using System.Text.RegularExpressions;

using Microsoft.Extensions.Logging;

namespace Astrolabed.Dns.Resolvers;

/// <summary>
/// Provides high-performance, streaming hosts file loading and domain-to-IP address mapping.
/// </summary>
/// <param name="httpClient">HttpClient instance for downloading remote hosts files.</param>
/// <param name="logger">Structured logger instance.</param>
public sealed partial class HostsFileReader(
    HttpClient httpClient,
    ILogger<HostsFileReader> logger) : IHostsFileReader
{
    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    private readonly ILogger<HostsFileReader> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    [GeneratedRegex(@"^(?=.{1,255}$)(?:[a-zA-Z0-9](?:[a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?\.)*(?:[a-zA-Z0-9](?:[a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?)$", RegexOptions.Compiled)]
    private static partial Regex HostnameRegex();

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, List<IPAddress>>> ReadHostsAsync(string sourceLocation, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceLocation);

        if (sourceLocation.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            sourceLocation.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            LogDownloadingHostsFromWeb(_logger, sourceLocation);

            using HttpResponseMessage response = await _httpClient.GetAsync(sourceLocation, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            await using Stream stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var reader = new StreamReader(stream);
            return await ParseHostsContentAsync(reader, ct).ConfigureAwait(false);
        }

        string resolvedPath = ResolveFilePath(sourceLocation);
        LogReadingHostsFromFileSystem(_logger, resolvedPath);

        await using FileStream fileStream = new(resolvedPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        using var fileReader = new StreamReader(fileStream);
        return await ParseHostsContentAsync(fileReader, ct).ConfigureAwait(false);
    }

    private static string ResolveFilePath(string sourceLocation)
    {
        if (Uri.TryCreate(sourceLocation, UriKind.Absolute, out Uri? uri) && uri.IsFile)
        {
            return uri.LocalPath;
        }

        string rawPath = sourceLocation;
        if (sourceLocation.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
        {
            rawPath = sourceLocation["file://".Length..];
        }

        return Path.GetFullPath(rawPath);
    }

    private async Task<IReadOnlyDictionary<string, List<IPAddress>>> ParseHostsContentAsync(TextReader reader, CancellationToken ct)
    {
        var map = new Dictionary<string, List<IPAddress>>(StringComparer.OrdinalIgnoreCase);

        string? line;
        while ((line = await reader.ReadLineAsync(ct).ConfigureAwait(false)) is not null)
        {
            ReadOnlySpan<char> span = line.AsSpan();

            int commentIdx = span.IndexOf('#');
            if (commentIdx >= 0)
            {
                span = span[..commentIdx];
            }

            span = span.Trim();
            if (span.IsEmpty)
            {
                continue;
            }

            // Extract IP Address token (first whitespace-delimited segment)
            int firstSpace = span.IndexOfAny(' ', '\t');
            if (firstSpace < 0)
            {
                continue;
            }

            ReadOnlySpan<char> ipSpan = span[..firstSpace].Trim();
            ReadOnlySpan<char> hostnamesSpan = span[firstSpace..].Trim();

            if (!IPAddress.TryParse(ipSpan, out IPAddress? ipAddress))
            {
                LogSkippingInvalidIp(_logger, ipSpan.ToString());
                continue;
            }

            // Slice remaining hostnames without array allocations
            while (!hostnamesSpan.IsEmpty)
            {
                int nextSpace = hostnamesSpan.IndexOfAny(' ', '\t');
                ReadOnlySpan<char> hostnameSpan;

                if (nextSpace >= 0)
                {
                    hostnameSpan = hostnamesSpan[..nextSpace].Trim();
                    hostnamesSpan = hostnamesSpan[nextSpace..].TrimStart(' ').TrimStart('\t');
                }
                else
                {
                    hostnameSpan = hostnamesSpan.Trim();
                    hostnamesSpan = ReadOnlySpan<char>.Empty;
                }

                if (hostnameSpan.IsEmpty)
                {
                    continue;
                }

                string hostname = hostnameSpan.TrimEnd('.').ToString();

                if (!IsValidHostname(hostname))
                {
                    LogSkippingInvalidHostname(_logger, hostname);
                    continue;
                }

                if (!map.TryGetValue(hostname, out List<IPAddress>? ipList))
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

    [LoggerMessage(
        EventId = 401,
        Level = LogLevel.Information,
        Message = "Downloading hosts file from Web source: {Url}")]
    private static partial void LogDownloadingHostsFromWeb(ILogger logger, string url);

    [LoggerMessage(
        EventId = 402,
        Level = LogLevel.Information,
        Message = "Reading hosts file from filesystem path: {Path}")]
    private static partial void LogReadingHostsFromFileSystem(ILogger logger, string path);

    [LoggerMessage(
        EventId = 403,
        Level = LogLevel.Warning,
        Message = "Skipping invalid IP address in hosts entry: {Token}")]
    private static partial void LogSkippingInvalidIp(ILogger logger, string token);

    [LoggerMessage(
        EventId = 404,
        Level = LogLevel.Warning,
        Message = "Skipping invalid hostname in hosts entry: {Hostname}")]
    private static partial void LogSkippingInvalidHostname(ILogger logger, string hostname);
}
