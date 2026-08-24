// File: src/Astrolabed.Dns/Models/DnsUdpReceiveResult.cs
using System.Net;
using System.Net.Sockets;

namespace Astrolabed.Dns.Models;

/// <summary>
/// Encapsulates the raw payload buffer, remote endpoint info, and server socket binding for a received UDP DNS packet.
/// </summary>
/// <param name="Buffer">The raw byte payload buffer containing the DNS packet.</param>
/// <param name="RemoteEndPoint">The remote endpoint that transmitted the packet.</param>
/// <param name="ServerSocket">The local socket that received the packet.</param>
public readonly record struct DnsUdpReceiveResult(byte[] Buffer, EndPoint RemoteEndPoint, Socket ServerSocket);
