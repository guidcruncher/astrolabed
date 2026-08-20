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

    public async Task LoadAndApplyListAsync(string uriOrPath, CancellationToken ct = default)
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
        else if (uriOrPath.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
        {
            var fileUri = new Uri(uriOrPath);
            _logger.LogInformation("Reading AdGuard rule list from local file URI: {FilePath}", fileUri.LocalPath);
            content = await File.ReadAllTextAsync(fileUri.LocalPath, ct).ConfigureAwait(false);
        }
        else
        {
            _logger.LogInformation("Reading AdGuard rule list from local file path: {FilePath}", uriOrPath);
            content = await File.ReadAllTextAsync(uriOrPath, ct).ConfigureAwait(false);
        }

        var (allowRules, blockRules) = ParseAdGuardRules(content);

        _ruleStore.UpdateRules(allowRules, blockRules);

        _logger.LogInformation("Successfully updated IDomainFilterRuleStore with rules loaded from {Source}.", uriOrPath);
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

            // Skip empty lines and AdGuard / Hostfile comments
            if (string.IsNullOrWhiteSpace(rule) || rule.StartsWith('!') || rule.StartsWith('#'))
            {
                continue;
            }

            // Ignore element hiding or CSS injection rules (containing #$#, ##, #?#)
            if (rule.Contains("##") || rule.Contains("#$#") || rule.Contains("#?#"))
            {
                continue;
            }

            bool isAllow = false;

            // Handle AdGuard exception rule syntax @@
            if (rule.StartsWith("@@"))
            {
                isAllow = true;
                rule = rule[2..];
            }

            // Strip AdGuard options ($important, $dnstype, etc.)
            int modifierIndex = rule.IndexOf('$');
            if (modifierIndex >= 0)
            {
                rule = rule[..modifierIndex];
            }

            rule = rule.Trim();
            if (string.IsNullOrEmpty(rule)) continue;

            string? parsedDomain = null;

            // AdGuard Domain Anchor syntax: ||example.com^
            if (rule.StartsWith("||"))
            {
                int endIdx = rule.IndexOf('^');
                parsedDomain = endIdx >= 0 ? rule[2..endIdx] : rule[2..];
            }
            // Standard Hosts file format line (e.g., 0.0.0.0 example.com or 127.0.0.1 example.com)
            else if (rule.StartsWith("0.0.0.0 ") || rule.StartsWith("127.0.0.1 "))
            {
                var parts = rule.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length >= 2)
                {
                    parsedDomain = parts[1];
                }
            }
            // Plain domain or wildcard/regex pattern
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
