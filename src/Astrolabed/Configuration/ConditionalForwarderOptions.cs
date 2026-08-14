namespace Astrolabed;

public class ConditionalForwarderOptions
{

    public const string SectionName = "Dns:ConditionalForwarding";

    public bool Enabled { get; set; } = false;
    public string DhcpServerIp { get; set; } = "192.168.1.1";
    public int DhcpServerPort { get; set; } = 53;
    public string LocalDomain { get; set; } = "lan";
    public string LocalSubnetCidr { get; set; } = "192.168.1.0/24";
    public bool ForwardNonFqdn { get; set; } = true;
}
