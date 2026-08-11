namespace Astrolabed.Dhcp;

internal sealed class DhcpLeaseDto
{
    public byte[] Mac { get; set; } = Array.Empty<byte>();
    public byte[] Ip { get; set; } = Array.Empty<byte>();
    public DateTimeOffset ExpiresAt { get; set; }
}
