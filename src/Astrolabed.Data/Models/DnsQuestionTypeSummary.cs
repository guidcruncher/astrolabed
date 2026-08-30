namespace Astrolabed.Data.Models;

/// <summary>
/// Represents an aggregated summary of DNS queries grouped by their question type.
/// </summary>
public sealed record DnsQuestionTypeSummary
{
    /// <summary>
    /// Gets the total number of DNS queries recorded for this question type.
    /// </summary>
    public int Total { get; init; }

    /// <summary>
    /// Gets the DNS question type identifier or name (for example: A, AAAA, MX, CNAME).
    /// </summary>
    public string QuestionType { get; init; } = string.Empty;
}
