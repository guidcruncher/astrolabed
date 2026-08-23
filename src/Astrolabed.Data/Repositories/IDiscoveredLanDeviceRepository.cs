using System.Net;

using Astrolabed.Data.Models;
using Astrolabed.Data.Pagination;

namespace Astrolabed.Data.Repositories;

/// <summary>
/// Defines the persistence contract for LAN device discovery operations, high-throughput network sweeps,
/// and device lifecycle management.
/// </summary>
public interface IDiscoveredLanDeviceRepository
{
    /// <summary>
    /// Asynchronously inserts or updates a single LAN device discovery record.
    /// </summary>
    /// <param name="device">The LAN device discovery record to insert or update.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous upsert operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="device"/> is null.</exception>
    Task UpsertAsync(
        DiscoveredLanDevice device,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously performs a bulk upsert operation for multiple LAN device discovery records in a single batch query execution.
    /// </summary>
    /// <param name="devices">The read-only collection of LAN device discovery records to process in bulk.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous bulk upsert operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="devices"/> is null.</exception>
    Task BulkUpsertAsync(
        IReadOnlyCollection<DiscoveredLanDevice> devices,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves a discovered LAN device record by its hardware MAC address.
    /// </summary>
    /// <param name="macAddress">The hardware MAC address of the device.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task representing the asynchronous operation, containing the matching <see cref="DiscoveredLanDevice"/> if found;
    /// otherwise, <see langword="null"/>.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="macAddress"/> is null or whitespace.</exception>
    Task<DiscoveredLanDevice?> GetByMacAddressAsync(
        string macAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves a discovered LAN device record by its assigned IP address.
    /// </summary>
    /// <param name="ipAddress">The target <see cref="IPAddress"/> of the device.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task representing the asynchronous operation, containing the matching <see cref="DiscoveredLanDevice"/> if found;
    /// otherwise, <see langword="null"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="ipAddress"/> is null.</exception>
    Task<DiscoveredLanDevice?> GetByIpAddressAsync(
        IPAddress ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves a discovered LAN device record by its Reverse DNS PTR domain name string.
    /// </summary>
    /// <param name="ptrAddress">The PTR domain name address string (e.g., "1.0.168.192.in-addr.arpa").</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task representing the asynchronous operation, containing the matching <see cref="DiscoveredLanDevice"/> if found;
    /// otherwise, <see langword="null"/>.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="ptrAddress"/> is null or whitespace.</exception>
    Task<DiscoveredLanDevice?> GetByPtrAddressAsync(
        string ptrAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves a paged result set of discovered LAN devices.
    /// </summary>
    /// <param name="pageNumber">The 1-based page index to retrieve.</param>
    /// <param name="pageSize">The number of items to include on each page.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation, containing the <see cref="PagedResult{T}"/> containing the device records.</returns>
    Task<PagedResult<DiscoveredLanDevice>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously removes a LAN device discovery record matching the specified hardware MAC address.
    /// </summary>
    /// <param name="macAddress">The hardware MAC address of the device to delete.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task representing the asynchronous operation, containing <see langword="true"/> if a record was removed;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="macAddress"/> is null or whitespace.</exception>
    Task<bool> DeleteByMacAddressAsync(
        string macAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously removes stale LAN device records that have not been observed since the specified cutoff timestamp.
    /// </summary>
    /// <param name="cutoff">The UTC timestamp threshold prior to which records are considered stale and eligible for deletion.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous cleanup operation.</returns>
    Task CleanOldDataAsync(
        DateTimeOffset cutoff,
        CancellationToken cancellationToken = default);
}

