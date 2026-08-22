// File: src/Astrolabed.Dns/Models/DnsUdpReceiveResult.cs
using System.Net;
using System.Net.Sockets;

namespace Astrolabed.Dns.Models;

public readonly record struct DnsUdpReceiveResult(byte[] Buffer, EndPoint RemoteEndPoint, Socket ServerSocket);
