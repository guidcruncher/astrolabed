namespace Astrolabed.Core.Network;

/// <summary>
/// Represents vendor information associated with a Media Access Control (MAC) Organizationally Unique Identifier (OUI).
/// </summary>
/// <param name="Oui">The raw MAC address prefix or OUI string from the IEEE registry.</param>
/// <param name="VendorName">The registered name of the vendor or organization.</param>
/// <param name="IsPrivate">Indicates whether the assignment is marked as private in the registry.</param>
/// <param name="BlockType">The IEEE block allocation type (e.g., MA-L, MA-M, MA-S, IAB).</param>
/// <param name="LastUpdate">The date string indicating when the record was last updated in the registry.</param>
public sealed record MacVendorInfo(
    string Oui,
    string VendorName,
    bool IsPrivate,
    string? BlockType,
    string? LastUpdate);
