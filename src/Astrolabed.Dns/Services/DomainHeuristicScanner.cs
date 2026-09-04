// File: DomainHeuristicScanner.cs
using System.Text.RegularExpressions;

using Astrolabed.Dns.Options;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Dns.Services;

/// <summary>
/// Performs heuristic analysis on domain names to identify potential ad-serving or tracking domains.
/// </summary>
public sealed class DomainHeuristicScanner : IDomainHeuristicScanner
{
    private readonly ILogger<DomainHeuristicScanner> _logger;
    private readonly HeuristicOptions _options;
    private readonly HashSet<string> _keywords;
    private readonly HashSet<string> _whitelist;

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainHeuristicScanner"/> class.
    /// </summary>
    /// <param name="logger">The logger instance used for recording operation logs.</param>
    /// <param name="options">The configured options containing heuristic thresholds, keywords, and whitelists.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> or <paramref name="options"/> is <see langword="null"/>.</exception>
    public DomainHeuristicScanner(
        ILogger<DomainHeuristicScanner> logger,
        IOptions<HeuristicOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ArgumentNullException.ThrowIfNull(options);

        _options = options.Value;
        _keywords = new HashSet<string>(_options.SuspiciousKeywords, StringComparer.OrdinalIgnoreCase);
        _whitelist = new HashSet<string>(_options.Whitelist, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Analyzes a domain name using multiple heuristic rules to determine if it is an ad or tracking domain.
    /// </summary>
    /// <param name="domain">The fully qualified domain name to evaluate.</param>
    /// <returns>
    /// A <see cref="DomainAssessmentResult"/> containing the final decision, threat score, and triggered heuristic rules.
    /// </returns>
    public DomainAssessmentResult AnalyzeDomain(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
        {
            return new DomainAssessmentResult(domain ?? string.Empty, false, 0.0, Array.Empty<string>());
        }

        string normalizedDomain = domain.Trim().ToLowerInvariant();

        // Step 1: Whitelist check
        if (_whitelist.Contains(normalizedDomain) || IsWhitelistedSuffix(normalizedDomain))
        {
            _logger.LogDebug("Domain '{Domain}' matched whitelist. Skipping heuristic scanning.", normalizedDomain);
            return new DomainAssessmentResult(normalizedDomain, false, 0.0, new[] { "Whitelisted" });
        }

        double score = 0.0;
        var triggeredRules = new List<string>();

        // Heuristic Rule 1: Structural Keyword Matching
        double keywordScore = EvaluateKeywordMatches(normalizedDomain, triggeredRules);
        score += keywordScore;

        // Heuristic Rule 2: Shannon Entropy (Randomness in Subdomains)
        double entropyScore = EvaluateSubdomainEntropy(normalizedDomain, triggeredRules);
        score += entropyScore;

        // Heuristic Rule 3: Subdomain Depth / Structure Analysis
        double structuralScore = EvaluateDomainStructure(normalizedDomain, triggeredRules);
        score += structuralScore;

        bool isAdDomain = score >= _options.ThreatThreshold;

        _logger.LogInformation(
            "Analyzed domain '{Domain}'. Result: IsAdDomain={IsAdDomain}, Score={Score}/{Threshold}. Rules: [{Rules}]",
            normalizedDomain,
            isAdDomain,
            score,
            _options.ThreatThreshold,
            string.Join(", ", triggeredRules));

        return new DomainAssessmentResult(normalizedDomain, isAdDomain, score, triggeredRules);
    }

    /// <summary>
    /// Determines whether a given domain ends with a whitelisted domain suffix.
    /// </summary>
    /// <param name="domain">The normalized domain name to check.</param>
    /// <returns><see langword="true"/> if the domain ends with a whitelisted suffix; otherwise, <see langword="false"/>.</returns>
    private bool IsWhitelistedSuffix(string domain)
    {
        return _whitelist.Any(allowed => domain.EndsWith($".{allowed}", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Evaluates domain segments against known ad and tracking keywords.
    /// </summary>
    /// <param name="domain">The normalized domain name to check.</param>
    /// <param name="triggeredRules">The list of triggered rules to append matches to.</param>
    /// <returns>The calculated threat score derived from keyword matches.</returns>
    private double EvaluateKeywordMatches(string domain, List<string> triggeredRules)
    {
        double score = 0.0;
        string[] parts = domain.Split('.', StringSplitOptions.RemoveEmptyEntries);

        foreach (string part in parts)
        {
            if (_keywords.Contains(part))
            {
                score += 35.0;
                triggeredRules.Add($"Exact Keyword Match: '{part}' (+35)");
            }
            else
            {
                foreach (string keyword in _keywords)
                {
                    if (part.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    {
                        score += 15.0;
                        triggeredRules.Add($"Partial Keyword Match: '{part}' contains '{keyword}' (+15)");
                    }
                }
            }
        }

        return score;
    }

    /// <summary>
    /// Evaluates the Shannon entropy of subdomains to detect random character sequences often used in tracking hashes.
    /// </summary>
    /// <param name="domain">The normalized domain name to analyze.</param>
    /// <param name="triggeredRules">The list of triggered rules to append entropy warnings to.</param>
    /// <returns>The calculated threat score based on subdomain character randomness.</returns>
    private static double EvaluateSubdomainEntropy(string domain, List<string> triggeredRules)
    {
        string[] parts = domain.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length <= 2)
        {
            return 0.0;
        }

        // Exclude TLD and SLD (e.g., example.com) to target subdomains only
        string subdomainsOnly = string.Join("", parts.Take(parts.Length - 2));
        double entropy = CalculateShannonEntropy(subdomainsOnly);

        // High entropy (e.g. > 3.8) in subdomains indicates dynamic tracking hashes
        if (entropy > 3.8 && subdomainsOnly.Length > 8)
        {
            triggeredRules.Add($"High Subdomain Entropy: {entropy:F2} (+25)");
            return 25.0;
        }

        return 0.0;
    }

    /// <summary>
    /// Evaluates structural elements of a domain, such as subdomain nesting depth and numeric tracking patterns.
    /// </summary>
    /// <param name="domain">The normalized domain name to analyze.</param>
    /// <param name="triggeredRules">The list of triggered rules to append structural findings to.</param>
    /// <returns>The calculated threat score based on domain structure.</returns>
    private static double EvaluateDomainStructure(string domain, List<string> triggeredRules)
    {
        double score = 0.0;
        string[] parts = domain.Split('.', StringSplitOptions.RemoveEmptyEntries);

        // Deeply nested subdomains (e.g. ad.tracker.cdn.example.com)
        if (parts.Length >= 5)
        {
            score += 15.0;
            triggeredRules.Add($"Excessive Domain Depth: {parts.Length} segments (+15)");
        }

        // Numeric-heavy subdomains commonly used by ad impression endpoints
        if (parts.Length > 2 && Regex.IsMatch(parts[0], @"\d{4,}"))
        {
            score += 20.0;
            triggeredRules.Add($"Numeric Tracking Identifier in Subdomain: '{parts[0]}' (+20)");
        }

        return score;
    }

    /// <summary>
    /// Calculates the Shannon entropy value of an input string to measure information density and randomness.
    /// </summary>
    /// <param name="input">The string segment to evaluate.</param>
    /// <returns>The Shannon entropy calculated using base-2 logarithm.</returns>
    private static double CalculateShannonEntropy(string input)
    {
        if (string.IsNullOrEmpty(input)) return 0.0;

        var map = new Dictionary<char, int>();
        foreach (char c in input)
        {
            if (!map.ContainsKey(c)) map[c] = 0;
            map[c]++;
        }

        double entropy = 0.0;
        double len = input.Length;

        foreach (var item in map)
        {
            double frequency = item.Value / len;
            entropy -= frequency * Math.Log2(frequency);
        }

        return entropy;
    }
}
