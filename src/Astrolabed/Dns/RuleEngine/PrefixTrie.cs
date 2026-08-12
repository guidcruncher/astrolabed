using System;
using System.Collections;
using System.Collections.Generic;

namespace Astrolabed.Dns.RuleEngine;

internal sealed class PrefixTrie
{
    private sealed class Node
    {
        public Dictionary<string, Node> Children { get; } = new(StringComparer.OrdinalIgnoreCase);
        public List<CompiledRule> Rules { get; } = new();
        public CompiledRule[]? RulesArray { get; set; }
    }

    private readonly Node _root = new();

    public void Add(string prefix, CompiledRule rule)
    {
        ReadOnlySpan<char> span = prefix.AsSpan();
        var node = _root;

        while (!span.IsEmpty)
        {
            int dotIdx = span.IndexOf('.');
            ReadOnlySpan<char> label;
            if (dotIdx < 0)
            {
                label = span;
                span = default;
            }
            else
            {
                label = span[..dotIdx];
                span = span[(dotIdx + 1)..];
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
        private readonly PrefixTrie _trie;
        private readonly string _domain;

        public MatchEnumerable(PrefixTrie trie, string domain)
        {
            _trie = trie;
            _domain = domain;
        }

        public Enumerator GetEnumerator() => new(_trie, _domain);

        IEnumerator<CompiledRule> IEnumerable<CompiledRule>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<CompiledRule>
        {
            private readonly PrefixTrie _trie;
            private readonly string _domain;
            private int _offset;
            private Node? _currentNode;
            private CompiledRule[]? _currentRules;
            private int _ruleIndex;

            internal Enumerator(PrefixTrie trie, string domain)
            {
                _trie = trie;
                _domain = domain;
                _offset = 0;
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

                while (_offset < _domain.Length && _currentNode != null)
                {
                    ReadOnlySpan<char> remaining = _domain.AsSpan(_offset);
                    int dotIdx = remaining.IndexOf('.');
                    ReadOnlySpan<char> label;
                    if (dotIdx < 0)
                    {
                        label = remaining;
                        _offset = _domain.Length;
                    }
                    else
                    {
                        label = remaining[..dotIdx];
                        _offset += dotIdx + 1;
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
