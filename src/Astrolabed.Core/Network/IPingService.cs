namespace Astrolabed.Core.Network;

/// <summary>
/// Provides network ping functionality to test reachability of remote hosts.
/// </summary>
public interface IPingService
{
    /// <summary>
    /// Asynchronously pings a specified host to determine if it is reachable.
    /// </summary>
    /// <param name="host">The host name or IP address to ping.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains <see langword="true"/> 
    /// if the host responded successfully; otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="System.ArgumentException">Thrown when <paramref name="host"/> is null, empty, or white space.</exception>
    Task<bool> PingAsync(string host, CancellationToken cancellationToken = default);
}
