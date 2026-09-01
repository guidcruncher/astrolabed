// File: src/Astrolabed.Dns/Filtering/AdGuardListLoader.cs
using Astrolabed.Data.Models;
using Astrolabed.Data.Repositories;

using Astrolabed.Dns.Options;

using Microsoft.Extensions.Logging;
namespace Astrolabed.Dns.Filtering;

/// <summary>
/// Provides streaming loading and parsing of AdGuard and standard Hosts format domain filter lists.
/// </summary>
/// <param name="httpClient">HttpClient instance for fetching remote lists.</param>
/// <param name="ruleStore">Domain filter rule store to update.</param>
/// <param name="logger">Structured logger instance.</param>
public sealed partial class AdGuardListLoader(
    HttpClient httpClient,
    IDomainFilterRuleStore ruleStore,
    IDnsListRepository listRepository,
    ILogger<AdGuardListLoader> logger) : IListLoader
{
    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    private readonly IDomainFilterRuleStore _ruleStore = ruleStore ?? throw new ArgumentNullException(nameof(ruleStore));
    private readonly IDnsListRepository _listRepository = listRepository ?? throw new ArgumentNullException(nameof(listRepository));
    private readonly ILogger<AdGuardListLoader> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task<(IReadOnlyList<string> AllowRules, IReadOnlyList<string> BlockRules)> LoadRulesAsync(ListSource source, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source.Path);

        var adGuardList = new DnsListEntity
        {
            Id = source.Id,
            Name = source.Name ?? "",
            Path = source.Path
        };
        await _listRepository.UpsertAsync(adGuardList, ct);

        if (source.Path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            source.Path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            LogDownloadingHttpList(_logger, source.Path);

            using HttpResponseMessage response = await _httpClient.GetAsync(source.Path, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            await using Stream stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var reader = new StreamReader(stream);
            return await ParseAdGuardRulesAsync(reader, ct).ConfigureAwait(false);
        }

        string filePath = ResolveFilePath(source.Path);
        LogReadingFileContent(_logger, filePath);

        await using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        using var fileReader = new StreamReader(fileStream);
        return await ParseAdGuardRulesAsync(fileReader, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task LoadAndApplyListAsync(ListSource source, CancellationToken ct = default)
    {
        var (allowRules, blockRules) = await LoadRulesAsync(source, ct).ConfigureAwait(false);
        _ruleStore.UpdateRules(allowRules, blockRules);

        LogUpdatedRuleStore(_logger, source.Path, allowRules.Count, blockRules.Count);
    }

    private static string ResolveFilePath(string uriOrPath)
    {
        if (Uri.TryCreate(uriOrPath, UriKind.Absolute, out Uri? uri) && uri.IsFile)
        {
            return uri.LocalPath;
        }

        string rawPath = uriOrPath;
        if (uriOrPath.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
        {
            rawPath = uriOrPath["file://".Length..];
        }

        return Path.GetFullPath(rawPath);
    }

    private async Task<(IReadOnlyList<string> AllowRules, IReadOnlyList<string> BlockRules)> ParseAdGuardRulesAsync(TextReader reader, CancellationToken ct)
    {
        var allowRules = new List<string>(1000);
        var blockRules = new List<string>(10000);

        string? line;
        while ((line = await reader.ReadLineAsync(ct).ConfigureAwait(false)) is not null)
        {
            ReadOnlySpan<char> span = line.AsSpan().Trim();

            if (span.IsEmpty || span[0] is '!' or '#')
            {
                continue;
            }

            // Ignore element hiding and cosmetic rules
            if (span.Contains("##".AsSpan(), StringComparison.Ordinal) ||
                span.Contains("#$#".AsSpan(), StringComparison.Ordinal) ||
                span.Contains("#?#".AsSpan(), StringComparison.Ordinal))
            {
                continue;
            }

            bool isAllow = false;

            if (span.StartsWith("@@".AsSpan(), StringComparison.Ordinal))
            {
                isAllow = true;
                span = span[2..];
            }

            int modifierIndex = span.IndexOf('$');
            if (modifierIndex >= 0)
            {
                span = span[..modifierIndex];
            }

            span = span.Trim();
            if (span.IsEmpty)
            {
                continue;
            }

            ReadOnlySpan<char> parsedDomain = default;

            if (span.StartsWith("||".AsSpan(), StringComparison.Ordinal))
            {
                int endIdx = span.IndexOf('^');
                parsedDomain = endIdx >= 0 ? span[2..endIdx] : span[2..];
            }
            else if (span.StartsWith("0.0.0.0 ".AsSpan(), StringComparison.Ordinal) ||
                     span.StartsWith("127.0.0.1 ".AsSpan(), StringComparison.Ordinal))
            {
                int firstSpace = span.IndexOf(' ');
                if (firstSpace >= 0)
                {
                    parsedDomain = span[(firstSpace + 1)..].Trim();
                }
            }
            else if (span[0] is not '/' and not '|')
            {
                parsedDomain = span.TrimEnd('^');
            }

            if (!parsedDomain.IsEmpty)
            {
                string domain = parsedDomain.ToString();
                if (isAllow)
                {
                    allowRules.Add(domain);
                }
                else
                {
                    blockRules.Add(domain);
                }
            }
        }

        LogParsedRulesCount(_logger, allowRules.Count, blockRules.Count);
        return (allowRules, blockRules);
    }

    [LoggerMessage(
        EventId = 101,
        Level = LogLevel.Information,
        Message = "Downloading AdGuard rule list from HTTP endpoint: {Uri}")]
    private static partial void LogDownloadingHttpList(ILogger logger, string uri);

    [LoggerMessage(
        EventId = 102,
        Level = LogLevel.Information,
        Message = "Reading AdGuard rule list from filesystem path: {FilePath}")]
    private static partial void LogReadingFileContent(ILogger logger, string filePath);

    [LoggerMessage(
        EventId = 103,
        Level = LogLevel.Information,
        Message = "Parsed AdGuard rules. Loaded {AllowCount} allow rules and {BlockCount} block rules.")]
    private static partial void LogParsedRulesCount(ILogger logger, int allowCount, int blockCount);

    [LoggerMessage(
        EventId = 104,
        Level = LogLevel.Information,
        Message = "Successfully updated IDomainFilterRuleStore with {AllowCount} allow and {BlockCount} block rules loaded from {Source}.")]
    private static partial void LogUpdatedRuleStore(ILogger logger, string source, int allowCount, int blockCount);
}
