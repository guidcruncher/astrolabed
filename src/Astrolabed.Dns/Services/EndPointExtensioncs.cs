namespace Astrolabed.Dns.Services;

using System.Diagnostics.CodeAnalysis;
using System.Net;

/// <summary>
/// High-performance extension methods for extracting IP address information from <see cref="EndPoint"/> instances.
/// </summary>
public static class EndPointExtensions
{
    /// <summary>
    /// Attempts to extract the <see cref="IPAddress"/> from an <see cref="EndPoint"/> instance.
    /// </summary>
    /// <param name="endPoint">The network endpoint to evaluate.</param>
    /// <param name="ipAddress">The extracted IP address if the endpoint represents an IP or host IP string; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if an IP address was successfully retrieved or parsed; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetIPAddress(this EndPoint? endPoint, [NotNullWhen(true)] out IPAddress? ipAddress)
    {
        switch (endPoint)
        {
            case IPEndPoint ipEndPoint:
                ipAddress = ipEndPoint.Address;
                return true;

            case DnsEndPoint dnsEndPoint when IPAddress.TryParse(dnsEndPoint.Host, out IPAddress? parsedIp):
                ipAddress = parsedIp;
                return true;

            default:
                ipAddress = null;
                return false;
        }
    }

    /// <summary>
    /// Gets the <see cref="IPAddress"/> from an <see cref="EndPoint"/>, throwing an exception if the endpoint cannot be resolved to an IP.
    /// </summary>
    /// <param name="endPoint">The network endpoint.</param>
    /// <returns>The associated <see cref="IPAddress"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="endPoint"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="endPoint"/> cannot be cast or converted to an <see cref="IPAddress"/>.</exception>
    public static IPAddress GetIPAddress(this EndPoint endPoint)
    {
        ArgumentNullException.ThrowIfNull(endPoint);

        if (endPoint.TryGetIPAddress(out IPAddress? ipAddress))
        {
            return ipAddress;
        }

        throw new InvalidOperationException($"Cannot extract IP address. Endpoint of type '{endPoint.GetType().FullName}' is not a valid IPEndPoint or parseable DnsEndPoint.");
    }
}
