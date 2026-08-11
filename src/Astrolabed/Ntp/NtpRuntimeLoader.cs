using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Ntp.Bootstrap;

public sealed class NtpRuntimeLoader : IHostedService
{
    private readonly ILogger<NtpRuntimeLoader> _logger;
    private readonly NtpServerOptions _options;
    private readonly INtpTimeSource _timeSource;

    public NtpRuntimeLoader(
        ILogger<NtpRuntimeLoader> logger,
        IOptions<NtpServerOptions> options,
        INtpTimeSource timeSource)
    {
        ArgumentNullException.ThrowIfNull(options);

        _logger = logger;
        _options = options.Value;
        _timeSource = timeSource;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogWarning("NTP Server is disabled. Runtime loader will not initialize.");
            return;
        }

        _logger.LogInformation("NTP Runtime Loader starting.");

        var result = await _timeSource.GetTimeAsync(cancellationToken);
        _logger.LogInformation("Reference time initialized: {RefUtc}", result.ReferenceUtc);

        await Task.Delay(10, cancellationToken);

        _logger.LogInformation("NTP Runtime Loader completed.");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("NTP Runtime Loader stopping.");
        return Task.CompletedTask;
    }
}
