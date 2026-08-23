using Astrolabed.Ntp.Options;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Ntp.Services;

/// <summary>
/// Composite time resolver that delegates time queries to either local system clock 
/// or upstream NTP servers based on configured server operational modes.
/// </summary>
/// <param name="localTimeResolver">Resolver serving local system clock time.</param>
/// <param name="upstreamTimeResolver">Resolver fetching time from external primary NTP stratum sources.</param>
/// <param name="optionsMonitor">Monitored options instance for dynamically updated server configuration.</param>
/// <param name="logger">Structured logger instance.</param>
public sealed partial class CompositeTimeResolver(
    LocalTimeResolver localTimeResolver,
    UpstreamTimeResolver upstreamTimeResolver,
    IOptionsMonitor<NtpServerOptions> optionsMonitor,
    ILogger<CompositeTimeResolver> logger) : ITimeResolver
{
    private readonly LocalTimeResolver _localTimeResolver = localTimeResolver ?? throw new ArgumentNullException(nameof(localTimeResolver));
    private readonly UpstreamTimeResolver _upstreamTimeResolver = upstreamTimeResolver ?? throw new ArgumentNullException(nameof(upstreamTimeResolver));
    private readonly IOptionsMonitor<NtpServerOptions> _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
    private readonly ILogger<CompositeTimeResolver> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async ValueTask<DateTimeOffset> GetCurrentTimeAsync(CancellationToken cancellationToken = default)
    {
        NtpServerOptions options = _optionsMonitor.CurrentValue;

        if (options.ResolverMode == TimeResolverMode.Upstream)
        {
            try
            {
                return await _upstreamTimeResolver.GetCurrentTimeAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogUpstreamTimeResolutionFailed(_logger, ex);
                return await _localTimeResolver.GetCurrentTimeAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        return await _localTimeResolver.GetCurrentTimeAsync(cancellationToken).ConfigureAwait(false);
    }

    [LoggerMessage(
        EventId = 301,
        Level = LogLevel.Warning,
        Message = "Failed to resolve time from upstream NTP server. Falling back to local time resolver.")]
    private static partial void LogUpstreamTimeResolutionFailed(ILogger logger, Exception exception);
}
