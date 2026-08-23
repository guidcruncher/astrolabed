namespace Astrolabed.Dns.Services;

/// <summary>
/// Defines the contract for resolving client network names or hostnames from DNS questions (such as reverse PTR queries).
/// </summary>
public interface IClientNameResolver
{
    /// <summary>
    /// Asynchronously resolves the hostname or domain name associated with a client PTR DNS query or IP string.
    /// </summary>
    /// <param name="question">The PTR question or IP address string to resolve.</param>
    /// <param name="ct">A cancellation token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains the resolved hostname if found; 
    /// otherwise, an empty string or <see langword="null"/>.
    /// </returns>
    Task<string> ResolveClientNameAsync(string question, CancellationToken ct = default);
}

