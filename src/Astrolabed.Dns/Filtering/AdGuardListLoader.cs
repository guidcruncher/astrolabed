// File: src/Astrolabed.Dns/Filtering/AdGuardListLoader.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Astrolabed.Dns.Filtering;

public sealed class AdGuardListLoader : IListLoader
{
    private readonly HttpClient _httpClient;
    private readonly IDomainFilterRuleStore _ruleStore;
    private readonly ILogger<AdGuardListLoader> _logger;

    public AdGuardListLoader(
        HttpClient httpClient,
        IDomainFilterRuleStore ruleStore,
        ILogger<AdGuardListLoader> logger)
    {
        _httpClient = httpClient;
        _ruleStore = ruleStore;
        _logger = logger;
    }

    public async Task<(List<string> AllowRules, List<string> BlockRules)> LoadRulesAsync(string uriOrPath, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(uriOrPath);

        string content;
        if (uriOrPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            uriOrPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Downloading AdGuard rule list from HTTP endpoint: {Uri}", uriOrPath);
            using var response = await _httpClient.GetAsync(uriOrPath, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        }
        else
        {
            var filePath = ResolveFilePath(uriOrPath);
            _logger.LogInformation("Reading AdGuard rule list from filesystem path: {FilePath}", filePath);
            content = await File.ReadAllTextAsync(filePath, ct).ConfigureAwait(false);
        }

        return ParseAdGuardRules(content);
    }

    public async Task LoadAndApplyListAsync(string uriOrPath, CancellationToken ct = default)
    {
        var (allowRules, blockRules) = await LoadRulesAsync(uriOrPath, ct).ConfigureAwait(false);
        _ruleStore.UpdateRules(allowRules, blockRules);

        _logger.LogInformation("Successfully updated IDomainFilterRuleStore with rules loaded from {Source}.", uriOrPath);
    }

    private static string ResolveFilePath(string uriOrPath)
    {
        string rawPath = uriOrPath;

        if (uriOrPath.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
        {
            rawPath = uriOrPath["file://".Length..];
        }

        return Path.GetFullPath(rawPath);
    }

    private (List<string> AllowRules, List<string> BlockRules) ParseAdGuardRules(string rawContent)
    {
        var allowRules = new List<string>();
        var blockRules = new List<string>();

        using var reader = new StringReader(rawContent);
        string? line;

        while ((line = reader.ReadLine()) != null)
        {
            var rule = line.Trim();

            if (string.IsNullOrWhiteSpace(rule) || rule.StartsWith('!') || rule.StartsWith('#'))
            {
                continue;
            }

            if (rule.Contains("##") || rule.Contains("#$#") || rule.Contains("#?#"))
            {
                continue;
            }

            bool isAllow = false;

            if (rule.StartsWith("@@"))
            {
                isAllow = true;
                rule = rule[2..];
            }

            int modifierIndex = rule.IndexOf('$');
            if (modifierIndex >= 0)
            {
                rule = rule[..modifierIndex];
            }

            rule = rule.Trim();
            if (string.IsNullOrEmpty(rule)) continue;

            string? parsedDomain = null;

            if (rule.StartsWith("||"))
            {
                int endIdx = rule.IndexOf('^');
                parsedDomain = endIdx >= 0 ? rule[2..endIdx] : rule[2..];
            }
            else if (rule.StartsWith("0.0.0.0 ") || rule.StartsWith("127.0.0.1 "))
            {
                var parts = rule.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length >= 2)
                {
                    parsedDomain = parts[1];
                }
            }
            else if (!rule.StartsWith('/') && !rule.StartsWith('|'))
            {
                parsedDomain = rule.TrimEnd('^');
            }

            if (!string.IsNullOrWhiteSpace(parsedDomain))
            {
                if (isAllow)
                {
                    allowRules.Add(parsedDomain);
                }
                else
                {
                    blockRules.Add(parsedDomain);
                }
            }
        }

        _logger.LogInformation("Parsed AdGuard rules. Loaded {AllowCount} allow rules and {BlockCount} block rules.", allowRules.Count, blockRules.Count);
        return (allowRules, blockRules);
    }
}
