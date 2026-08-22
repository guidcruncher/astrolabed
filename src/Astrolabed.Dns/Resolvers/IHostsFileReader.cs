// File: src/Astrolabed.Dns/Resolvers/IHostsFileReader.cs
using System.Net;

namespace Astrolabed.Dns.Resolvers;

public interface IHostsFileReader
{
    Task<IReadOnlyDictionary<string, List<IPAddress>>> ReadHostsAsync(string sourceLocation, CancellationToken ct = default);
}
