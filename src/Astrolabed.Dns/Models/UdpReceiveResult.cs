// File: src/Astrolabed.Dns/Models/UdpReceiveResult.cs
using System.Net;
using System.Net.Sockets;

namespace Astrolabed.Dns.Models;

public readonly record struct UdpReceiveResult(byte[] Buffer, EndPoint RemoteEndPoint, Socket ServerSocket);
