// File: IAdDomainHeuristicScanner.cs
namespace Astrolabed.Dns.Services;

/// <summary>
/// Represents the result of a domain threat evaluation.
/// </summary>
/// <param name="Domain">The domain name evaluated by the scanner.</param>
/// <param name="IsAdDomain">Indicates whether the domain was identified as an advertisement or tracking domain.</param>
/// <param name="TotalScore">The total threat score calculated during analysis.</param>
/// <param name="TriggeredRules">A collection of rules triggered during the evaluation process.</param>
public sealed record DomainAssessmentResult(
    string Domain,
    bool IsAdDomain,
    double TotalScore,
    IReadOnlyList<string> TriggeredRules
)
{
    /// <summary>
    /// Gets the domain name evaluated by the scanner.
    /// </summary>
    public string Domain { get; init; } = Domain;

    /// <summary>
    /// Gets a value indicating whether the domain was identified as an advertisement or tracking domain.
    /// </summary>
    public bool IsAdDomain { get; init; } = IsAdDomain;

    /// <summary>
    /// Gets the total threat score calculated during analysis.
    /// </summary>
    public double TotalScore { get; init; } = TotalScore;

    /// <summary>
    /// Gets the collection of rules triggered during the evaluation process.
    /// </summary>
    public IReadOnlyList<string> TriggeredRules { get; init; } = TriggeredRules;
}

/// <summary>
/// Service contract for scanning domains using heuristics.
/// </summary>
public interface IDomainHeuristicScanner
{
    /// <summary>
    /// Analyzes a target domain using heuristic evaluation rules to assess whether it is an ad or tracking domain.
    /// </summary>
    /// <param name="domain">The fully qualified domain name to evaluate.</param>
    /// <returns>A <see cref="DomainAssessmentResult"/> containing the threat assessment, score, and triggered rules.</returns>
    DomainAssessmentResult AnalyzeDomain(string domain);
}
