using System.Threading;
using System.Threading.Tasks;

namespace Astrolabed.Ntp;

public interface INtpTimeSource
{
    Task<NtpTimeResult> GetTimeAsync(CancellationToken ct);
}
