// File: tests/Astrolabed.Dns.Tests/DomainFilterRuleReloaderTests.cs
using System.Collections.Frozen;

using Astrolabed.Dns.Filtering;
using Astrolabed.Dns.Options;
using Astrolabed.Dns.Services;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Xunit;

namespace Astrolabed.Dns.Tests;

public class DomainFilterRuleReloaderTests
{
    [Fact]
    public async Task StartAsync_LoadsConfiguredSources_UpdatesRuleStore()
    {
        // Arrange
        var options = new DomainFilterRuleOptions
        {
            AllowListSources = [new ListSource { Id = 1, Name = "", Path = "http://example.com/allow.txt" }],
            BlockListSources = [new ListSource { Id = 2, Name = "", Path = "http://example.com/block.txt" }]
        };

        var optionsMonitor = new TestOptionsMonitor<DomainFilterRuleOptions>(options);

        var listLoader = new FakeListLoader();
        listLoader.Responses["http://example.com/allow.txt"] = (new[] { "allowed.org" }, Array.Empty<string>());
        listLoader.Responses["http://example.com/block.txt"] = (Array.Empty<string>(), new[] { "blocked.org" });

        var ruleStore = new SpyDomainFilterRuleStore();

        using var reloader = new DomainFilterRuleReloader(optionsMonitor, listLoader, ruleStore, NullLogger<DomainFilterRuleReloader>.Instance);

        // Act
        await reloader.StartAsync(CancellationToken.None);

        // Assert
        Assert.Equal(2, ruleStore.UpdateRulesCallCount);
        Assert.Contains(ruleStore.Updates, u => u.RuleListId == 1 && u.Allows.Contains("allowed.org"));
        Assert.Contains(ruleStore.Updates, u => u.RuleListId == 2 && u.Blocks.Contains("blocked.org"));
    }

    private sealed class FakeListLoader : IListLoader
    {
        public Dictionary<string, (IReadOnlyList<string> Allows, IReadOnlyList<string> Blocks)> Responses { get; } = new();

        public Task<(IReadOnlyList<string> AllowRules, IReadOnlyList<string> BlockRules)> LoadRulesAsync(ListSource source, CancellationToken cancellationToken = default)
        {
            if (Responses.TryGetValue(source.Path, out var result))
            {
                return Task.FromResult(result);
            }

            return Task.FromResult<(IReadOnlyList<string>, IReadOnlyList<string>)>((Array.Empty<string>(), Array.Empty<string>()));
        }

        public Task LoadAndApplyListAsync(ListSource source, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class SpyDomainFilterRuleStore : IDomainFilterRuleStore
    {
        public int UpdateRulesCallCount { get; private set; }
        public List<(int RuleListId, List<string> Allows, List<string> Blocks)> Updates { get; } = [];

        public IReadOnlyDictionary<string, int> ExactAllows => FrozenDictionary<string, int>.Empty;
        public IReadOnlyList<RegexRule> RegexAllows => Array.Empty<RegexRule>();
        public IReadOnlyDictionary<string, int> ExactBlocks => FrozenDictionary<string, int>.Empty;
        public IReadOnlyList<RegexRule> RegexBlocks => Array.Empty<RegexRule>();

        public RuleStoreSnapshot GetCompiledSnapshot() => throw new NotImplementedException();

        public void UpdateRules(int ruleListId, IEnumerable<string> allowRules, IEnumerable<string> blockRules)
        {
            UpdateRulesCallCount++;
            Updates.Add((ruleListId, allowRules.ToList(), blockRules.ToList()));
        }
    }

    private sealed class TestOptionsMonitor<T>(T currentValue) : IOptionsMonitor<T>
    {
        public T CurrentValue => currentValue;

        public T Get(string? name) => currentValue;

        public IDisposable OnChange(Action<T, string?> listener) => DummyDisposable.Instance;

        private sealed class DummyDisposable : IDisposable
        {
            public static readonly DummyDisposable Instance = new();
            public void Dispose() { }
        }
    }
}
