namespace Astrolabed.Core.Network;

/// <summary>
/// Defines a service for performing fast, in-memory lookups of MAC address vendor information.
/// </summary>
public interface IMacVendorLookupService
{
    /// <summary>
    /// Finds the vendor information corresponding to the given MAC address or OUI prefix.
    /// </summary>
    /// <param name="macAddress">The MAC address or OUI prefix string to look up.</param>
    /// <returns>The matching <see cref="MacVendorInfo"/> if found; otherwise, <see langword="null"/>.</returns>
    MacVendorInfo? FindVendor(string macAddress);

    /// <summary>
    /// Attempts to find the vendor information corresponding to the given MAC address or OUI prefix.
    /// </summary>
    /// <param name="macAddress">The MAC address or OUI prefix string to look up.</param>
    /// <param name="vendor">When this method returns, contains the matching <see cref="MacVendorInfo"/> if found; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if a vendor match was found; otherwise, <see langword="false"/>.</returns>
    bool TryGetVendor(string macAddress, out MacVendorInfo? vendor);
}
