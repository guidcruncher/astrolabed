using System.Net.NetworkInformation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Astrolabed.Core.Options;

namespace Astrolabed.Core.Network;

/// <summary>
/// Provides network ping services to verify host reachability.
/// </summary>
public class PingService : IPingService
{
    private readonly ILogger<PingService> _logger;
    private readonly PingServiceOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="PingService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance for operational output.</param>
    /// <param name="options">The configured options for ping operations.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> or <paramref name="options"/> is null.</exception>
    public PingService(
        ILogger<PingService> logger,
        IOptions<PingServiceOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Asynchronously pings a specified host to determine if it is reachable.
    /// </summary>
    /// <param name="host">The host name or IP address to ping.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains <see langword="true"/> 
    /// if the host responded successfully; otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="host"/> is null, empty, or consists only of white-space characters.</exception>
    public async Task<bool> PingAsync(string host, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            throw new ArgumentException("Host cannot be null or empty.", nameof(host));
        }

        using var ping = new Ping();
        var pingOptions = new PingOptions(_options.Ttl, _options.DontFragment);
        byte[] buffer = new byte[32];

        try
        {
            _logger.LogInformation("Sending ping request to {Host} with timeout {Timeout}ms.", host, _options.TimeoutMilliseconds);

            PingReply reply = await ping.SendPingAsync(
                host,
                _options.TimeoutMilliseconds,
                buffer,
                pingOptions);

            if (reply.Status == IPStatus.Success)
            {
                _logger.LogInformation("Ping to {Host} succeeded. Time: {RoundtripTime}ms, IP: {Address}", host, reply.RoundtripTime, reply.Address);
                return true;
            }

            _logger.LogWarning("Ping to {Host} failed. Status: {Status}", host, reply.Status);
            return false;
        }
        catch (PingException ex)
        {
            _logger.LogError(ex, "An error occurred while pinging host {Host}.", host);
            return false;
        }
    }
}
