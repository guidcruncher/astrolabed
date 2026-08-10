using System;
using System.Collections;
using System.Collections.Generic;

namespace Astrolabed.Dns.RuleEngine;

internal sealed class WildcardBuckets
{
    internal readonly struct BucketEntry
    {
        public string Core { get; }
        public CompiledRule[] Rules { get; }

        public BucketEntry(string core, CompiledRule[] rules)
        {
            Core = core;
            Rules = rules;
        }
    }

    private readonly Dictionary<string, List<CompiledRule>> _dict = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();
    private BucketEntry[]? _frozenBuckets;

    public void Add(string pattern, CompiledRule rule)
    {
        var core = pattern.Trim('*').ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(core))
        {
            return;
        }

        lock (_lock)
        {
            _frozenBuckets = null;

            if (!_dict.TryGetValue(core, out var list))
            {
                list = new List<CompiledRule>();
                _dict[core] = list;
            }

            list.Add(rule);
        }
    }

    public MatchEnumerable MatchAll(string domain)
    {
        var buckets = GetFrozenBuckets();
        return new MatchEnumerable(buckets, domain);
    }

    private BucketEntry[] GetFrozenBuckets()
    {
        var frozen = _frozenBuckets;
        if (frozen != null)
        {
            return frozen;
        }

        lock (_lock)
        {
            if (_frozenBuckets == null)
            {
                var array = new BucketEntry[_dict.Count];
                int idx = 0;
                foreach (var kvp in _dict)
                {
                    array[idx++] = new BucketEntry(kvp.Key, kvp.Value.ToArray());
                }
                _frozenBuckets = array;
            }
            return _frozenBuckets;
        }
    }

    public readonly struct MatchEnumerable : IEnumerable<CompiledRule>
    {
        private readonly BucketEntry[] _buckets;
        private readonly string _domain;

        internal MatchEnumerable(BucketEntry[] buckets, string domain)
        {
            _buckets = buckets;
            _domain = domain;
        }

        public Enumerator GetEnumerator() => new(_buckets, _domain);

        IEnumerator<CompiledRule> IEnumerable<CompiledRule>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<CompiledRule>
        {
            private readonly BucketEntry[] _buckets;
            private readonly string _domainLower;
            private int _bucketIndex;
            private int _ruleIndex;
            private CompiledRule[]? _currentRules;

            internal Enumerator(BucketEntry[] buckets, string domain)
            {
                _buckets = buckets;
                _domainLower = ToLowerFast(domain);
                _bucketIndex = 0;
                _ruleIndex = 0;
                _currentRules = null;
            }

            public CompiledRule Current => _currentRules![_ruleIndex - 1];

            object? IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (_currentRules != null && _ruleIndex < _currentRules.Length)
                {
                    _ruleIndex++;
                    return true;
                }

                while (_bucketIndex < _buckets.Length)
                {
                    var bucket = _buckets[_bucketIndex++];
                    if (_domainLower.Contains(bucket.Core, StringComparison.Ordinal))
                    {
                        var rules = bucket.Rules;
                        if (rules.Length > 0)
                        {
                            _currentRules = rules;
                            _ruleIndex = 1;
                            return true;
                        }
                    }
                }

                return false;
            }

            public void Reset() => throw new NotSupportedException();

            public void Dispose() { }

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
    }
}
