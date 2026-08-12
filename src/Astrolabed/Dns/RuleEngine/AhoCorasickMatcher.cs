using System;
using System.Collections;
using System.Collections.Generic;

namespace Astrolabed.Dns.RuleEngine;

internal sealed class AhoCorasickMatcher<T>
{
    private sealed class Node
    {
        public Dictionary<char, Node> Next { get; } = new();
        public Node? Fail { get; set; }
        public List<T> Output { get; } = new();
        public int Id { get; set; }
    }

    private readonly Node _root = new();
    private int[] _asciiTransitions = Array.Empty<int>();
    private T[]?[] _outputs = Array.Empty<T[]?>();
    private Dictionary<(int State, char Char), int>? _nonAsciiTransitions;

    public void Add(string pattern, T rule)
    {
        var core = pattern.Trim('*');
        if (string.IsNullOrWhiteSpace(core))
        {
            return;
        }

        var node = _root;
        foreach (var c in core)
        {
            if (!node.Next.TryGetValue(c, out var child))
            {
                child = new Node();
                node.Next[c] = child;
            }
            node = child;
        }

        node.Output.Add(rule);
    }

    public void Build()
    {
        var nodes = new List<Node>();
        var queue = new Queue<Node>();

        _root.Id = 0;
        nodes.Add(_root);

        foreach (var kv in _root.Next)
        {
            kv.Value.Fail = _root;
            queue.Enqueue(kv.Value);
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            current.Id = nodes.Count;
            nodes.Add(current);

            foreach (var kv in current.Next)
            {
                var c = kv.Key;
                var child = kv.Value;

                var fail = current.Fail;
                while (fail != null && !fail.Next.ContainsKey(c))
                {
                    fail = fail.Fail;
                }

                child.Fail = fail?.Next.GetValueOrDefault(c) ?? _root;

                foreach (var r in child.Fail.Output)
                {
                    child.Output.Add(r);
                }

                queue.Enqueue(child);
            }
        }

        int totalStates = nodes.Count;
        _asciiTransitions = new int[totalStates * 128];
        _outputs = new T[totalStates][];

        Dictionary<(int State, char Char), int>? nonAscii = null;

        for (int i = 0; i < totalStates; i++)
        {
            var node = nodes[i];
            _outputs[i] = node.Output.Count > 0 ? node.Output.ToArray() : null;

            int failState = node.Fail?.Id ?? 0;

            for (int c = 0; c < 128; c++)
            {
                char ch = (char)c;
                if (node.Next.TryGetValue(ch, out var child))
                {
                    _asciiTransitions[i * 128 + c] = child.Id;
                }
                else if (i == 0)
                {
                    _asciiTransitions[i * 128 + c] = 0;
                }
                else
                {
                    _asciiTransitions[i * 128 + c] = _asciiTransitions[failState * 128 + c];
                }
            }

            foreach (var kv in node.Next)
            {
                if (kv.Key >= 128)
                {
                    nonAscii ??= new Dictionary<(int, char), int>();
                    nonAscii[(i, kv.Key)] = kv.Value.Id;
                }
            }
        }

        _nonAsciiTransitions = nonAscii;
    }

    public MatchEnumerable Match(string text)
    {
        return new MatchEnumerable(this, text);
    }

    internal int GetNonAsciiState(int state, char c)
    {
        if (_nonAsciiTransitions != null && _nonAsciiTransitions.TryGetValue((state, c), out int nextState))
        {
            return nextState;
        }
        return 0;
    }

    public readonly struct MatchEnumerable : IEnumerable<T>
    {
        private readonly AhoCorasickMatcher<T> _matcher;
        private readonly string _text;

        public MatchEnumerable(AhoCorasickMatcher<T> matcher, string text)
        {
            _matcher = matcher;
            _text = text;
        }

        public Enumerator GetEnumerator() => new(_matcher, _text);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<T>
        {
            private readonly AhoCorasickMatcher<T> _matcher;
            private readonly string _text;
            private int _textIndex;
            private int _state;
            private T[]? _currentOutputs;
            private int _outputIndex;

            internal Enumerator(AhoCorasickMatcher<T> matcher, string text)
            {
                _matcher = matcher;
                _text = text;
                _textIndex = 0;
                _state = 0;
                _currentOutputs = null;
                _outputIndex = 0;
            }

            public T Current => _currentOutputs![_outputIndex - 1];

            object? IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (_currentOutputs != null && _outputIndex < _currentOutputs.Length)
                {
                    _outputIndex++;
                    return true;
                }

                var asciiTransitions = _matcher._asciiTransitions;
                var outputs = _matcher._outputs;

                while (_textIndex < _text.Length)
                {
                    char c = _text[_textIndex++];
                    int state = _state;

                    if (c < 128)
                    {
                        int index = state * 128 + c;
                        state = (uint)index < (uint)asciiTransitions.Length ? asciiTransitions[index] : 0;
                    }
                    else
                    {
                        state = _matcher.GetNonAsciiState(state, c);
                    }

                    _state = state;

                    if ((uint)state < (uint)outputs.Length)
                    {
                        var stateOutputs = outputs[state];
                        if (stateOutputs != null && stateOutputs.Length > 0)
                        {
                            _currentOutputs = stateOutputs;
                            _outputIndex = 1;
                            return true;
                        }
                    }
                }

                return false;
            }

            public void Reset()
            {
                _textIndex = 0;
                _state = 0;
                _currentOutputs = null;
                _outputIndex = 0;
            }

            public void Dispose() { }
        }
    }
}

