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
            AllowListSources = ["http://example.com/allow.txt"],
            BlockListSources = ["http://example.com/block.txt"]
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
        Assert.Equal(1, ruleStore.UpdateRulesCallCount);
        Assert.Contains("allowed.org", ruleStore.LastAllows);
        Assert.Contains("blocked.org", ruleStore.LastBlocks);
    }

    private sealed class FakeListLoader : IListLoader
    {
        public Dictionary<string, (IReadOnlyList<string> Allows, IReadOnlyList<string> Blocks)> Responses { get; } = new();

        public Task<(IReadOnlyList<string> AllowRules, IReadOnlyList<string> BlockRules)> LoadRulesAsync(string sourcePath, CancellationToken cancellationToken = default)
        {
            if (Responses.TryGetValue(sourcePath, out var result))
            {
                return Task.FromResult(result);
            }

            return Task.FromResult<(IReadOnlyList<string>, IReadOnlyList<string>)>((Array.Empty<string>(), Array.Empty<string>()));
        }

        public Task LoadAndApplyListAsync(string sourcePath, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class SpyDomainFilterRuleStore : IDomainFilterRuleStore
    {
        public int UpdateRulesCallCount { get; private set; }
        public List<string> LastAllows { get; private set; } = [];
        public List<string> LastBlocks { get; private set; } = [];

        public IReadOnlySet<string> ExactAllows => FrozenSet<string>.Empty;
        public IReadOnlyList<string> RegexAllows => Array.Empty<string>();
        public IReadOnlySet<string> ExactBlocks => FrozenSet<string>.Empty;
        public IReadOnlyList<string> RegexBlocks => Array.Empty<string>();

        public RuleStoreSnapshot GetCompiledSnapshot() => throw new NotImplementedException();

        public void UpdateRules(IEnumerable<string> allowRules, IEnumerable<string> blockRules)
        {
            UpdateRulesCallCount++;
            LastAllows = allowRules.ToList();
            LastBlocks = blockRules.ToList();
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
