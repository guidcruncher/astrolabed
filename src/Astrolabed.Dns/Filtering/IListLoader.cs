// File: src/Astrolabed.Dns/Filtering/IListLoader.cs
using System.Threading;
using System.Threading.Tasks;

namespace Astrolabed.Dns.Filtering;

public interface IListLoader
{
    Task LoadAndApplyListAsync(string uriOrPath, CancellationToken ct = default);
}
