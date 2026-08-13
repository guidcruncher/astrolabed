using System.Text;

using Astrolabed.Data.Repositories;
using Astrolabed.Events;

namespace Astrolabed.Metrics;

public sealed class MetricsRepository
{

    private readonly IDnsResponseEventRepository _dnsResponseRepo;

    public MetricsRepository(IDnsResponseEventRepository dnsResponseRepo)
    {
        _dnsResponseRepo = dnsResponseRepo;
    }

    public void RecordDnsResponse(DnsResponseEvent evt)
    {
        _dnsResponseRepo.Add(evt);
    }

}
