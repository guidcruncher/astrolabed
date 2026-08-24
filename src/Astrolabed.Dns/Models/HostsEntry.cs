// File: src/Astrolabed.Dns/Models/HostsEntry.cs
using System.Net;

namespace Astrolabed.Dns.Models;

/// <summary>
/// Represents a parsed host entry mapping a hostname to one or more associated IP addresses.
/// </summary>
/// <param name="Hostname">The fully qualified domain name or hostname entry.</param>
/// <param name="Addresses">The read-only collection of IP addresses assigned to the hostname.</param>
public sealed record HostsEntry(string Hostname, IReadOnlyList<IPAddress> Addresses);
