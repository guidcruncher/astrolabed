using System;
using System.Collections;
using System.Collections.Generic;

namespace Astrolabed.Dns.RuleEngine;

internal sealed class SuffixTrie
{
    private sealed class Node
    {
        public Dictionary<string, Node> Children { get; } = new(StringComparer.OrdinalIgnoreCase);
        public List<CompiledRule> Rules { get; } = new();
        public CompiledRule[]? RulesArray { get; set; }
    }

    private readonly Node _root = new();

    public void Add(string suffix, CompiledRule rule)
    {
        ReadOnlySpan<char> span = suffix.AsSpan();
        var node = _root;

        while (!span.IsEmpty)
        {
            int dotIdx = span.LastIndexOf('.');
            ReadOnlySpan<char> label;
            if (dotIdx < 0)
            {
                label = span;
                span = default;
            }
            else
            {
                label = span[(dotIdx + 1)..];
                span = span[..dotIdx];
            }

            if (label.IsEmpty)
            {
                continue;
            }

            var lookup = node.Children.GetAlternateLookup<ReadOnlySpan<char>>();
            if (!lookup.TryGetValue(label, out var child))
            {
                child = new Node();
                node.Children[label.ToString()] = child;
            }
            node = child;
        }

        node.Rules.Add(rule);
        node.RulesArray = node.Rules.ToArray();
    }

    public MatchEnumerable MatchAll(string domain)
    {
        return new MatchEnumerable(this, domain);
    }

    public readonly struct MatchEnumerable : IEnumerable<CompiledRule>
    {
        private readonly SuffixTrie _trie;
        private readonly string _domain;

        public MatchEnumerable(SuffixTrie trie, string domain)
        {
            _trie = trie;
            _domain = domain;
        }

        public Enumerator GetEnumerator() => new(_trie, _domain);

        IEnumerator<CompiledRule> IEnumerable<CompiledRule>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<CompiledRule>
        {
            private readonly SuffixTrie _trie;
            private readonly string _domain;
            private int _length;
            private Node? _currentNode;
            private CompiledRule[]? _currentRules;
            private int _ruleIndex;

            internal Enumerator(SuffixTrie trie, string domain)
            {
                _trie = trie;
                _domain = domain;
                _length = domain.Length;
                _currentNode = trie._root;
                _currentRules = null;
                _ruleIndex = 0;
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

                while (_length > 0 && _currentNode != null)
                {
                    ReadOnlySpan<char> remaining = _domain.AsSpan(0, _length);
                    int dotIdx = remaining.LastIndexOf('.');
                    ReadOnlySpan<char> label;
                    if (dotIdx < 0)
                    {
                        label = remaining;
                        _length = 0;
                    }
                    else
                    {
                        label = remaining[(dotIdx + 1)..];
                        _length = dotIdx;
                    }

                    if (label.IsEmpty)
                    {
                        continue;
                    }

                    var lookup = _currentNode.Children.GetAlternateLookup<ReadOnlySpan<char>>();
                    if (!lookup.TryGetValue(label, out var child))
                    {
                        _currentNode = null;
                        return false;
                    }

                    _currentNode = child;
                    var rules = child.RulesArray;
                    if (rules != null && rules.Length > 0)
                    {
                        _currentRules = rules;
                        _ruleIndex = 1;
                        return true;
                    }
                }

                return false;
            }

            public void Reset() => throw new NotSupportedException();

            public void Dispose() { }
        }
    }
}
