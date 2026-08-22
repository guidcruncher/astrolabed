using Astrolabed.Ntp.Options;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Ntp.Services;

public class CompositeTimeResolver : ITimeResolver
{
    private readonly LocalTimeResolver _localTimeResolver;
    private readonly UpstreamTimeResolver _upstreamTimeResolver;
    private readonly IOptionsMonitor<NtpServerOptions> _optionsMonitor;
    private readonly ILogger<CompositeTimeResolver> _logger;

    public CompositeTimeResolver(
        LocalTimeResolver localTimeResolver,
        UpstreamTimeResolver upstreamTimeResolver,
        IOptionsMonitor<NtpServerOptions> optionsMonitor,
        ILogger<CompositeTimeResolver> logger)
    {
        _localTimeResolver = localTimeResolver;
        _upstreamTimeResolver = upstreamTimeResolver;
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    public async ValueTask<DateTimeOffset> GetCurrentTimeAsync(CancellationToken cancellationToken = default)
    {
        NtpServerOptions options = _optionsMonitor.CurrentValue;

        return options.ResolverMode switch
        {
            TimeResolverMode.Upstream => await _upstreamTimeResolver.GetCurrentTimeAsync(cancellationToken),
            TimeResolverMode.Local => await _localTimeResolver.GetCurrentTimeAsync(cancellationToken),
            _ => await _localTimeResolver.GetCurrentTimeAsync(cancellationToken)
        };
    }
}
