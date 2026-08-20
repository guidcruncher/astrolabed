// File: src/Astrolabed.Dns/Filtering/DomainFilterRuleStore.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using Microsoft.Extensions.Logging;

namespace Astrolabed.Dns.Filtering;

public sealed class DomainFilterRuleStore : IDomainFilterRuleStore
{
    private readonly ILogger<DomainFilterRuleStore> _logger;
    private readonly object _syncRoot = new();

    private HashSet<string> _exactAllows = new(StringComparer.OrdinalIgnoreCase);
    private HashSet<string> _exactBlocks = new(StringComparer.OrdinalIgnoreCase);
    private List<Regex> _regexAllows = [];
    private List<Regex> _regexBlocks = [];

    public DomainFilterRuleStore(ILogger<DomainFilterRuleStore> logger)
    {
        _logger = logger;
    }

    public IReadOnlySet<string> ExactAllows
    {
        get
        {
            lock (_syncRoot)
            {
                return new HashSet<string>(_exactAllows, StringComparer.OrdinalIgnoreCase);
            }
        }
    }

    public IReadOnlySet<string> ExactBlocks
    {
        get
        {
            lock (_syncRoot)
            {
                return new HashSet<string>(_exactBlocks, StringComparer.OrdinalIgnoreCase);
            }
        }
    }

    public IReadOnlyList<string> RegexAllows
    {
        get
        {
            lock (_syncRoot)
            {
                return _regexAllows.Select(r => r.ToString()).ToList();
            }
        }
    }

    public IReadOnlyList<string> RegexBlocks
    {
        get
        {
            lock (_syncRoot)
            {
                return _regexBlocks.Select(r => r.ToString()).ToList();
            }
        }
    }

    public void UpdateRules(IEnumerable<string> allowRules, IEnumerable<string> blockRules)
    {
        var (exactAllows, regexAllows) = ProcessRules(allowRules);
        var (exactBlocks, regexBlocks) = ProcessRules(blockRules);

        lock (_syncRoot)
        {
            _exactAllows = exactAllows;
            _regexAllows = regexAllows;
            _exactBlocks = exactBlocks;
            _regexBlocks = regexBlocks;
        }

        _logger.LogInformation(
            "Domain filter rule store updated. Allows: {ExactAllowsCount} exact, {RegexAllowsCount} regex. Blocks: {ExactBlocksCount} exact, {RegexBlocksCount} regex.",
            exactAllows.Count, regexAllows.Count, exactBlocks.Count, regexBlocks.Count);
    }

    public (HashSet<string> ExactAllows, List<Regex> RegexAllows, HashSet<string> ExactBlocks, List<Regex> RegexBlocks) GetCompiledSnapshot()
    {
        lock (_syncRoot)
        {
            return (_exactAllows, _regexAllows, _exactBlocks, _regexBlocks);
        }
    }

    private (HashSet<string> ExactMatches, List<Regex> RegexMatches) ProcessRules(IEnumerable<string> rules)
    {
        var exactMatches = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var rawRegexPatterns = new HashSet<string>(StringComparer.Ordinal);
        var regexMatches = new List<Regex>();

        foreach (var rawRule in rules)
        {
            if (string.IsNullOrWhiteSpace(rawRule)) continue;

            var rule = rawRule.Trim();
            if (rule.StartsWith('#')) continue; // Skip comments

            if (IsRegexRule(rule, out var pattern))
            {
                rawRegexPatterns.Add(pattern);
            }
            else
            {
                exactMatches.Add(NormalizeDomain(rule));
            }
        }

        foreach (var pattern in rawRegexPatterns)
        {
            try
            {
                var regex = new Regex(
                    pattern,
                    RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
                    TimeSpan.FromMilliseconds(100));

                regexMatches.Add(regex);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Invalid regex pattern skipped: {Pattern}", pattern);
            }
        }

        return (exactMatches, regexMatches);
    }

    private static bool IsRegexRule(string rule, out string pattern)
    {
        if (rule.StartsWith('/') && rule.EndsWith('/') && rule.Length > 2)
        {
            pattern = rule[1..^1];
            return true;
        }

        if (rule.Contains('*') || rule.Contains('?') || rule.Contains('^') || rule.Contains('$'))
        {
            pattern = rule;
            return true;
        }

        pattern = string.Empty;
        return false;
    }

    private static string NormalizeDomain(string domain)
    {
        return domain.Trim().TrimEnd('.').ToLowerInvariant();
    }
}
