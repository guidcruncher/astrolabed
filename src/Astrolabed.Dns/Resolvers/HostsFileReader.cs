// File: src/Astrolabed.Dns/Resolvers/HostsFileReader.cs
using System.Net;

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

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, IReadOnlyList<IPAddress>>> ReadHostsAsync(string sourceLocation, CancellationToken ct = default)
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

    private async Task<IReadOnlyDictionary<string, IReadOnlyList<IPAddress>>> ParseHostsContentAsync(TextReader reader, CancellationToken ct)
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

                hostnameSpan = hostnameSpan.TrimEnd('.');

                if (!IsValidHostname(hostnameSpan))
                {
                    LogSkippingInvalidHostname(_logger, hostnameSpan.ToString());
                    continue;
                }

                string hostname = hostnameSpan.ToString();

                if (!map.TryGetValue(hostname, out List<IPAddress>? ipList))
                {
                    ipList = [];
                    map[hostname] = ipList;
                }

                if (!ipList.Contains(ipAddress))
                {
                    ipList.Add(ipAddress);
                }
            }
        }

        return map.ToDictionary(
            kvp => kvp.Key,
            kvp => (IReadOnlyList<IPAddress>)kvp.Value,
            StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsValidHostname(ReadOnlySpan<char> hostname)
    {
        if (hostname.IsEmpty || hostname.Length > 255)
        {
            return false;
        }

        int labelLength = 0;
        for (int i = 0; i < hostname.Length; i++)
        {
            char c = hostname[i];

            if (c == '.')
            {
                if (labelLength is 0 or > 63)
                {
                    return false;
                }
                labelLength = 0;
                continue;
            }

            bool isValidChar = char.IsAsciiLetterOrDigit(c) || c == '-';
            if (!isValidChar)
            {
                return false;
            }

            labelLength++;
        }

        return labelLength is > 0 and <= 63;
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
