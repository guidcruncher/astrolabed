using System.Net;

using Astrolabed.Data.Models;
using Astrolabed.Data.Pagination;

namespace Astrolabed.Data.Repositories;

/// <summary>
/// Contract for managing LAN device discovery persistence operations.
/// </summary>
public interface IDiscoveredLanDeviceRepository
{
    /// <summary>
    /// Upserts or adds a single LAN device discovery record.
    /// </summary>
    Task UpsertAsync(DiscoveredLanDevice device, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a bulk upsert operation for multiple LAN device discovery records in a single batch query execution.
    /// </summary>
    Task BulkUpsertAsync(IEnumerable<DiscoveredLanDevice> devices, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a discovered LAN device record by MAC address.
    /// </summary>
    Task<DiscoveredLanDevice?> GetByMacAddressAsync(string macAddress, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a discovered LAN device record by IP address.
    /// </summary>
    Task<DiscoveredLanDevice?> GetByIpAddressAsync(IPAddress ipAddress, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a discovered LAN device record by PTR address.
    /// </summary>
    Task<DiscoveredLanDevice?> GetByPtrAddressAsync(string ptrAddress, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a paged list of all discovered LAN devices.
    /// </summary>
    Task<PagedResult<DiscoveredLanDevice>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a LAN device discovery record by MAC address.
    /// </summary>
    Task<bool> DeleteByMacAddressAsync(string macAddress, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes records for devices not seen since a specified cutoff epoch timestamp.
    /// </summary>
    Task CleanOldDataAsync(DateTimeOffset cutoff, CancellationToken cancellationToken = default);
}
