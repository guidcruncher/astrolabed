using System.Net;
using System.Net.NetworkInformation;

namespace Astrolabed.Dhcp;

public interface IDhcpServerEngine
{
    Task RunAsync(CancellationToken ct);
}
