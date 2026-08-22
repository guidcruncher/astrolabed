// File: src/Astrolabed.Dns/Services/IDnsListener.cs
using System.Net;

namespace Astrolabed.Dns.Services;

public interface IDnsListener
{
    Task ListenAsync(IPAddress address, int port, CancellationToken ct);
}
