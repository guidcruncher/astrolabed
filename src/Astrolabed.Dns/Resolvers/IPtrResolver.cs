namespace Astrolabed.Dns.Resolvers;

/// <summary>
/// Defines reverse DNS lookup operations for resolving pointer (PTR) record queries.
/// </summary>
public interface IPtrResolver
{
    /// <summary>
    /// Attempts to resolve a reverse DNS PTR query string (e.g., "1.0.0.127.in-addr.arpa") to its associated domain name.
    /// </summary>
    /// <param name="ptrQuery">The reverse lookup domain query string.</param>
    /// <param name="domainName">Outputs the matched domain name if resolved; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if a matching domain name was found; otherwise, <c>false</c>.</returns>
    bool TryResolvePtr(string ptrQuery, out string? domainName);

    /// <summary>
    /// Attempts to resolve a reverse DNS PTR query span (e.g., "1.0.0.127.in-addr.arpa") to its associated domain name.
    /// </summary>
    /// <param name="ptrQuery">The reverse lookup domain query character span.</param>
    /// <param name="domainName">Outputs the matched domain name if resolved; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if a matching domain name was found; otherwise, <c>false</c>.</returns>
    bool TryResolvePtr(ReadOnlySpan<char> ptrQuery, out string? domainName) =>
        TryResolvePtr(ptrQuery.ToString(), out domainName);
}
