using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

using Astrolabed.Dhcp;

using Xunit;

namespace Astrolabed.Dhcp.Tests;

public class LeaseEngineTests
{
    private static PhysicalAddress Mac(int id) =>
        new PhysicalAddress(new byte[] { 0, 1, 2, 3, 4, (byte)id });

    [Fact]
    public async Task AllocateWithArpCheck_ShouldAllocateNewLease()
    {
        var store = new InMemoryDhcpLeaseStore();
        var pool = new CidrPoolAllocator("192.168.10.0/29");
        var engine = new DhcpLeaseEngine(store, pool);
        var arp = new ArpConflictDetector(IPAddress.Parse("127.0.0.1"));

        var lease = await engine.AllocateWithArpCheckAsync("test", "host", Mac(1), TimeSpan.FromHours(1), arp);

        Assert.Equal("192.168.10.1", lease.Ip.ToString());
        Assert.Single(store.GetActiveLeases());
    }

    [Fact]
    public async Task AllocateWithArpCheck_ShouldRenewExistingLease()
    {
        var store = new InMemoryDhcpLeaseStore();
        var pool = new CidrPoolAllocator("192.168.10.0/29");
        var engine = new DhcpLeaseEngine(store, pool);
        var arp = new ArpConflictDetector(IPAddress.Parse("127.0.0.1"));

        var first = await engine.AllocateWithArpCheckAsync("test", "host1", Mac(1), TimeSpan.FromHours(1), arp);
        var second = await engine.AllocateWithArpCheckAsync("test", "host2", Mac(1), TimeSpan.FromHours(1), arp);

        Assert.Equal(first.Ip, second.Ip);
    }

    [Fact]
    public async Task Release_ShouldRemoveLease()
    {
        var store = new InMemoryDhcpLeaseStore();
        var pool = new CidrPoolAllocator("192.168.10.0/29");
        var engine = new DhcpLeaseEngine(store, pool);

        await store.SaveAsync(new DhcpLease
        {
            Mac = Mac(1),
            Ip = IPAddress.Parse("192.168.10.1"),
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        });

        await engine.ReleaseAsync(Mac(1));

        Assert.Empty(store.GetActiveLeases());
    }

    [Fact]
    public async Task Decline_ShouldQuarantineIp()
    {
        var store = new InMemoryDhcpLeaseStore();
        var pool = new CidrPoolAllocator("192.168.10.0/29");
        var engine = new DhcpLeaseEngine(store, pool);

        var ip = IPAddress.Parse("192.168.10.5");

        await engine.DeclineAsync(ip);

        Assert.Contains(ip, store.GetBadIps());
    }
}
