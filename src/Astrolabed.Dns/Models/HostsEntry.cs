// File: src/Astrolabed.Dns/Models/HostsEntry.cs
using System.Net;

namespace Astrolabed.Dns.Models;

public sealed record HostsEntry(string Hostname, IReadOnlyList<IPAddress> Addresses);
