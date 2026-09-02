// File: src/Astrolabed.Dns/Filtering/FilterListParser.cs
using System.Net;
using System.Text.RegularExpressions;

using Microsoft.Extensions.Logging;

namespace Astrolabed.Dns.Filtering;

/// <summary>
/// Provides streaming parsing for standard Hosts files and AdGuard rule list formats.
/// </summary>
/// <param name="logger">Structured logger instance.</param>
public sealed partial class FilterListParser(ILogger<FilterListParser> logger) : IFilterListParser
{
    /// <summary>
    /// The structured logger instance.
    /// </summary>
    private readonly ILogger<FilterListParser> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task<IReadOnlyList<FilterRule>> ParseAsync(TextReader reader, int listId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var rules = new List<FilterRule>();
        var seenPatterns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string? line;

        while ((line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)) is not null)
        {
            ReadOnlySpan<char> span = line.AsSpan().Trim();
            if (span.IsEmpty || span[0] is '!' or '#')
            {
                continue;
            }

            // Skip cosmetic or element hiding rules
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

            // Do not strip '$' modifiers if the span is a bounded regex rule (/pattern/)
            bool isRegexBounded = span.StartsWith('/') && span.EndsWith('/') && span.Length > 2;
            if (!isRegexBounded)
            {
                int modifierIdx = span.IndexOf('$');
                if (modifierIdx >= 0)
                {
                    span = span[..modifierIdx];
                }
            }

            span = span.Trim();
            if (span.IsEmpty)
            {
                continue;
            }

            FilterRule? parsedRule = ParseLineSpan(span, listId, isAllow);
            if (parsedRule is not null && seenPatterns.Add($"{parsedRule.IsAllow}:{parsedRule.Pattern}"))
            {
                rules.Add(parsedRule);
            }
        }

        LogParsingComplete(_logger, rules.Count, listId);
        return rules;
    }

    /// <summary>
    /// Parses a trimmed line span into a filter rule instance.
    /// </summary>
    private FilterRule? ParseLineSpan(ReadOnlySpan<char> span, int listId, bool isAllow)
    {
        // 1. Regex Syntax: /pattern/
        if (span.StartsWith('/') && span.EndsWith('/') && span.Length > 2)
        {
            string rawPattern = span[1..^1].ToString();
            try
            {
                var regex = new Regex(
                    rawPattern,
                    RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
                    TimeSpan.FromMilliseconds(100));

                return new FilterRule(rawPattern, RuleKind.Regex, isAllow, listId, null, regex);
            }
            catch (ArgumentException ex)
            {
                LogInvalidRegexSkipped(_logger, ex, rawPattern);
                return null;
            }
        }

        IPAddress? ipAddress = null;

        // 2. Hosts Syntax: 127.0.0.1 domain.com or ::1 domain.com
        int spaceIndex = span.IndexOfAny(' ', '\t');
        if (spaceIndex > 0)
        {
            ReadOnlySpan<char> ipSpan = span[..spaceIndex].Trim();
            ReadOnlySpan<char> domainSpan = span[(spaceIndex + 1)..].Trim();

            if (IPAddress.TryParse(ipSpan, out IPAddress? parsedIp) && !domainSpan.IsEmpty)
            {
                ipAddress = parsedIp;
                span = domainSpan;
            }
        }

        // 3. AdGuard Hierarchy Syntax: ||domain.com^
        if (span.StartsWith("||".AsSpan(), StringComparison.Ordinal))
        {
            int endIdx = span.IndexOf('^');
            string domain = (endIdx >= 0 ? span[2..endIdx] : span[2..]).ToString().Trim().TrimEnd('.').ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(domain))
            {
                return null;
            }

            return new FilterRule(domain, RuleKind.Hierarchy, isAllow, listId, ipAddress);
        }

        // 4. Wildcard / Asterisk Rules
        if (span.Contains('*') || span.Contains('?'))
        {
            string rawRule = span.ToString().Trim();
            string escaped = Regex.Escape(rawRule).Replace(@"\*", ".*").Replace(@"\?", ".");
            string regexPattern = $"^{escaped}$";

            try
            {
                var regex = new Regex(
                    regexPattern,
                    RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
                    TimeSpan.FromMilliseconds(100));

                return new FilterRule(regexPattern, RuleKind.Regex, isAllow, listId, ipAddress, regex);
            }
            catch (ArgumentException ex)
            {
                LogInvalidRegexSkipped(_logger, ex, regexPattern);
                return null;
            }
        }

        // 5. Exact Domain Rule
        string exactDomain = span.ToString().TrimEnd('^').Trim().TrimEnd('.').ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(exactDomain))
        {
            return null;
        }

        return new FilterRule(exactDomain, RuleKind.Exact, isAllow, listId, ipAddress);
    }

    [LoggerMessage(EventId = 301, Level = LogLevel.Information, Message = "Successfully parsed {Count} rules for ListId {ListId}.")]
    private static partial void LogParsingComplete(ILogger logger, int count, int listId);

    [LoggerMessage(EventId = 302, Level = LogLevel.Warning, Message = "Skipped invalid regex rule: {Pattern}")]
    private static partial void LogInvalidRegexSkipped(ILogger logger, Exception ex, string pattern);
}
