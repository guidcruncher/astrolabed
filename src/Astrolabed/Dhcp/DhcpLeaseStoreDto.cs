namespace Astrolabed.Dhcp;

internal sealed class DhcpLeaseStoreDto
{
    public List<DhcpLeaseDto> Leases { get; set; } = new();
    public List<byte[]> BadIps { get; set; } = new();
}
