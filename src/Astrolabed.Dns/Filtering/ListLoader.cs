// File: src/Astrolabed.Dns/Filtering/ListLoader.cs
using System.Text;

using Astrolabed.Data.Models;
using Astrolabed.Data.Repositories;
using Astrolabed.Dns.Options;

using Microsoft.Extensions.Logging;

namespace Astrolabed.Dns.Filtering;

/// <summary>
/// Loads domain filter rules from HTTP endpoints or local filesystem files into storage.
/// </summary>
/// <param name="httpClient">HTTP client instance.</param>
/// <param name="parser">Filter list parser instance.</param>
/// <param name="ruleStore">Target filter rule store instance.</param>
/// <param name="dnsList">List repository.</param>
/// <param name="logger">Structured logger instance.</param>
public sealed partial class ListLoader(
    HttpClient httpClient,
    IFilterListParser parser,
    IFilterRuleStore ruleStore,
    IDnsListRepository dnsList,
    ILogger<ListLoader> logger) : IListLoader
{
    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    private readonly IFilterListParser _parser = parser ?? throw new ArgumentNullException(nameof(parser));
    private readonly IFilterRuleStore _ruleStore = ruleStore ?? throw new ArgumentNullException(nameof(ruleStore));
    private readonly IDnsListRepository _dnsList = dnsList ?? throw new ArgumentNullException(nameof(dnsList));
    private readonly ILogger<ListLoader> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task LoadAndApplyListAsync(ListSource source, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(source.Path);

        DnsListEntity et = new DnsListEntity()
        {
            Id = source.Id,
            Name = source.Name,
            Path = source.Path
        };

        await _dnsList.UpsertAsync(et, cancellationToken);

        IReadOnlyList<FilterRule> parsedRules;

        if (source.Path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            source.Path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            LogDownloadingHttpList(_logger, source.Path);
            using HttpResponseMessage response = await _httpClient.GetAsync(source.Path, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            parsedRules = await _parser.ParseAsync(reader, source.Id, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            string filePath = ResolveFilePath(source.Path);
            LogReadingFileContent(_logger, filePath);

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Filter list file not found: {filePath}", filePath);
            }

            await using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
            using var fileReader = new StreamReader(fileStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            parsedRules = await _parser.ParseAsync(fileReader, source.Id, cancellationToken).ConfigureAwait(false);
        }

        _ruleStore.UpdateListRules(source.Id, parsedRules);
    }

    private static string ResolveFilePath(string uriOrPath)
    {
        if (Uri.TryCreate(uriOrPath, UriKind.Absolute, out Uri? uri) && uri.IsFile)
        {
            return Uri.UnescapeDataString(uri.LocalPath);
        }

        return Path.GetFullPath(uriOrPath);
    }

    [LoggerMessage(EventId = 501, Level = LogLevel.Information, Message = "Downloading DNS filter list: {Uri}")]
    private static partial void LogDownloadingHttpList(ILogger logger, string uri);

    [LoggerMessage(EventId = 502, Level = LogLevel.Information, Message = "Reading DNS filter file: {FilePath}")]
    private static partial void LogReadingFileContent(ILogger logger, string filePath);
}
