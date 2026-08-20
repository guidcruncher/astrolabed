// File: src/Astrolabed.Dns/Resolvers/IHostsFileReader.cs
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Astrolabed.Dns.Resolvers;

public interface IHostsFileReader
{
    Task<IReadOnlyDictionary<string, List<IPAddress>>> ReadHostsAsync(string sourceLocation, CancellationToken ct = default);
}
