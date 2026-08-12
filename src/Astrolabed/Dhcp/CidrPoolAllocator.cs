using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Net;

using Microsoft.Extensions.Options;

namespace Astrolabed.Dhcp;

public sealed class CidrPoolAllocator : ICidrPoolAllocator
{
    private readonly IPAddress _network;
    private readonly IPAddress _netmask;
    private readonly uint _first;
    private readonly uint _last;

    public CidrPoolAllocator(IOptions<DhcpOptions> options)
        : this(options.Value.PoolCidr)
    {
    }

    public CidrPoolAllocator(string cidr)
    {
        ArgumentException.ThrowIfNullOrEmpty(cidr);

        var parts = cidr.Split('/');
        var ip = IPAddress.Parse(parts[0]);
        var prefix = int.Parse(parts[1]);

        uint mask = prefix == 0 ? 0 : uint.MaxValue << (32 - prefix);
        _netmask = FromUInt32(mask);

        uint net = ToUInt32(ip) & mask;
        _network = FromUInt32(net);

        _first = net + 1;
        _last = (net | ~mask) - 1;
    }

    public bool IsInPool(IPAddress ip)
    {
        ArgumentNullException.ThrowIfNull(ip);
        uint value = ToUInt32(ip);
        return value >= _first && value <= _last;
    }

    public IEnumerable<IPAddress> AllocationSequence(IEnumerable<IPAddress> used)
    {
        ArgumentNullException.ThrowIfNull(used);
        var usedSet = used.Select(ToUInt32).ToHashSet();

        for (uint i = _first; i <= _last; i++)
        {
            if (!usedSet.Contains(i))
            {
                yield return FromUInt32(i);
            }
        }
    }

    public IPAddress? Allocate(IEnumerable<IPAddress> used)
    {
        ArgumentNullException.ThrowIfNull(used);
        var usedSet = used.Select(ToUInt32).ToHashSet();

        for (uint i = _first; i <= _last; i++)
        {
            if (!usedSet.Contains(i))
            {
                return FromUInt32(i);
            }
        }

        return null;
    }

    private static uint ToUInt32(IPAddress ip)
    {
        Span<byte> bytes = stackalloc byte[4];
        if (!ip.TryWriteBytes(bytes, out _))
        {
            throw new ArgumentException("Invalid IPv4 address length", nameof(ip));
        }
        return BinaryPrimitives.ReadUInt32BigEndian(bytes);
    }

    private static IPAddress FromUInt32(uint value)
    {
        Span<byte> bytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(bytes, value);
        return new IPAddress(bytes);
    }
}
