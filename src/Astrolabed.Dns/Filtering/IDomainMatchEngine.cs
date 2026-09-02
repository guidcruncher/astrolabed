// File: src/Astrolabed.Dns/Filtering/IDomainMatchEngine.cs
namespace Astrolabed.Dns.Filtering;

/// <summary>
/// Defines domain matching operations using dual allow/block collections.
/// </summary>
public interface IDomainMatchEngine
{

    /// <summary>
    /// Suspend Blocking until the Date/time specified
    /// </summary>
    DateTimeOffset? DisableBlockingUntil { get; set; }

    /// <summary>
    /// Suspend Blocking for the given Timspan from UTC now
    /// </summary>
    /// <param name="duration">The TimeSpan duration to disable for</param>
    /// <returns>The DateTimeOffset when Blocking will be re-enabled</returns>
    void DisableBlocking(TimeSpan duration);

    /// <summary>
    /// Resume Blocking Immediately.
    /// </summary>
    void ResumeBlocking();

    /// <summary>
    /// Scans rule collections for the first match against a target domain.
    /// Allow matches take absolute priority over block matches.
    /// </summary>
    /// <param name="domain">The domain name to check.</param>
    /// <param name="matchedRule">Outputs the matched filter rule if found; otherwise <c>null</c>.</param>
    /// <returns><c>true</c> if a matching rule was found; otherwise <c>false</c>.</returns>
    bool TryMatch(string domain, out FilterRule? matchedRule);
}
