using System.Net;
using System.Net.NetworkInformation;

namespace Astrolabed.Dhcp;

public interface IArpConflictDetector
{
    Task<bool> HasConflictAsync(IPAddress candidate, TimeSpan timeout);
}

