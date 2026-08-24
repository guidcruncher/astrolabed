// File: src/Astrolabed.Dns/Resolvers/IHostsManager.cs
using System.Collections.Frozen;

using Astrolabed.Dns.Models;

namespace Astrolabed.Dns.Resolvers;

/// <summary>
/// Defines management operations for reading, reloading, and accessing aggregated hosts file entries.
/// </summary>
public interface IHostsManager
{
    /// <summary>
    /// Gets the current read-only list snapshot of aggregated and deduplicated host entries.
    /// </summary>
    IReadOnlyList<HostsEntry> Entries { get; }

    /// <summary>
    /// Gets the fast, thread-safe snapshot lookup dictionary mapping normalized hostnames to entries.
    /// </summary>
    FrozenDictionary<string, HostsEntry> Lookup { get; }

    /// <summary>
    /// Asynchronously reloads all configured hosts sources and updates <see cref="Entries"/> and <see cref="Lookup"/> atomically.
    /// </summary>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous reload operation.</returns>
    Task ReloadAsync(CancellationToken ct = default);
}
