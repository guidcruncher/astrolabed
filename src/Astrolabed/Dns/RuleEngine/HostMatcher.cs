using System;
using System.Collections.Generic;
using System.Net;

namespace Astrolabed.Dns.RuleEngine;

internal sealed class HostMatcher
{
    private enum HostPatternKind : byte
    {
        Exact,
        Suffix,
        Prefix,
        WildcardSubstring
    }

    private readonly record struct HostPattern(
        string Core,
        IPAddress Address,
        int Specificity,
        HostPatternKind Kind);

    private readonly Dictionary<string, IPAddress> _exact = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<HostPattern> _nonExact = new();
    private readonly object _lock = new();
    private bool _isSorted;

    public void Add(string host, IPAddress ip)
    {
        var pattern = host.Trim();
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return;
        }

        lock (_lock)
        {
            _isSorted = false;

            if (!pattern.Contains('*'))
            {
                var core = pattern.ToLowerInvariant();
                _exact[core] = ip;
                return;
            }

            if (pattern.StartsWith("*.", StringComparison.Ordinal))
            {
                var core = pattern[2..].ToLowerInvariant();
                _nonExact.Add(new HostPattern(core, ip, core.Length, HostPatternKind.Suffix));
                return;
            }

            if (pattern.EndsWith(".*", StringComparison.Ordinal) && !pattern.StartsWith("*.", StringComparison.Ordinal))
            {
                var core = pattern[..^2].ToLowerInvariant();
                _nonExact.Add(new HostPattern(core, ip, core.Length, HostPatternKind.Prefix));
                return;
            }

            var trimmed = pattern.Trim('*').ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                return;
            }

            _nonExact.Add(new HostPattern(trimmed, ip, trimmed.Length, HostPatternKind.WildcardSubstring));
        }
    }

    public IPAddress? MatchMostSpecific(string domain)
    {
        if (_exact.Count == 0 && _nonExact.Count == 0)
        {
            return null;
        }

        string lower = ToLowerFast(domain);

        if (_exact.TryGetValue(lower, out var exactIp))
        {
            return exactIp;
        }

        if (_nonExact.Count == 0)
        {
            return null;
        }

        EnsureSorted();

        for (int i = 0; i < _nonExact.Count; i++)
        {
            var p = _nonExact[i];
            if (IsMatch(p, lower))
            {
                return p.Address;
            }
        }

        return null;
    }

    private void EnsureSorted()
    {
        if (_isSorted)
        {
            return;
        }

        lock (_lock)
        {
            if (!_isSorted)
            {
                _nonExact.Sort((a, b) => b.Specificity.CompareTo(a.Specificity));
                _isSorted = true;
            }
        }
    }

    private static bool IsMatch(in HostPattern p, string domain)
    {
        return p.Kind switch
        {
            HostPatternKind.Exact =>
                string.Equals(domain, p.Core, StringComparison.Ordinal),

            HostPatternKind.Suffix =>
                domain.EndsWith(p.Core, StringComparison.Ordinal),

            HostPatternKind.Prefix =>
                domain.StartsWith(p.Core, StringComparison.Ordinal),

            HostPatternKind.WildcardSubstring =>
                domain.Contains(p.Core, StringComparison.Ordinal),

            _ => false
        };
    }

    private static string ToLowerFast(string s)
    {
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (c >= 'A' && c <= 'Z')
            {
                return s.ToLowerInvariant();
            }
        }
        return s;
    }
}
