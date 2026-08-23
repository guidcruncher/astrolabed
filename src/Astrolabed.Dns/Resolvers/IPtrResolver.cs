// File: src/Astrolabed.Dns/Resolvers/IPtrResolver.cs
using System.Net;

namespace Astrolabed.Dns.Resolvers;

/// <summary>
/// Defines contracts for reverse DNS pointer (PTR) record resolution and conditional forwarding.
/// </summary>
public interface IPtrResolver
{
    /// <summary>
    /// Attempts to resolve a static reverse PTR record override for a PTR query domain.
    /// </summary>
    /// <param name="ptrQuery">The PTR query string to check.</param>
    /// <param name="domainName">Outputs the matched host domain name if found.</param>
    /// <returns><c>true</c> if a PTR record match exists; otherwise <c>false</c>.</returns>
    bool TryResolvePtr(string ptrQuery, out string? domainName);

    /// <summary>
    /// Attempts to resolve a conditional forwarder target address for a reverse DNS PTR query.
    /// </summary>
    /// <param name="ptrQuery">The PTR query string to inspect.</param>
    /// <param name="targetResolver">Outputs the designated forwarder IP address if matched; otherwise <c>null</c>.</param>
    /// <returns><c>true</c> if a matching subnet rule was found; otherwise <c>false</c>.</returns>
    bool TryGetConditionalForwarder(string ptrQuery, out IPAddress? targetResolver);
}
