using Astrolabed.Dns.Models;

namespace Astrolabed.Dns.Resolvers;

/// <summary>
/// Defines management operations for reading and reloading aggregated hosts file entries.
/// </summary>
public interface IHostsManager
{
    /// <summary>
    /// Gets the current read-only snapshot of aggregated and deduplicated host entries.
    /// </summary>
    IReadOnlyList<HostsEntry> Entries { get; }

    /// <summary>
    /// Asynchronously reloads all configured hosts sources and updates <see cref="Entries"/> atomically.
    /// </summary>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous reload operation.</returns>
    Task ReloadAsync(CancellationToken ct = default);
}

