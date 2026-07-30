using RimMind.Memory.Core;
using RimMind.Memory.DarkMemory;
using RimMind.Memory.Data;
using RimMind.Memory.Decay;
using RimMind.Testing;
using Xunit;

namespace RimMind.Memory.Tests.Contracts
{
    [Collection("Memory contracts")]
    public sealed class MemoryParsingContextContracts
    {
        [Fact]
        public void Dark_memory_parsing_preserves_safe_fallbacks_and_limits()
        {
            ContractCaseRunner.Run(
                ("invalid JSON returns no replacement value", () =>
                    Assert.Null(DarkMemoryResultParserPure.Parse("not-json", 3))),
                ("missing dark field preserves an empty result", () =>
                {
                    var result = DarkMemoryResultParserPure.Parse("{\"other\":true}", 3);
                    Assert.NotNull(result);
                    Assert.Empty(result!);
                }),
                ("partial entries skip empty values", () =>
                {
                    var result = DarkMemoryResultParserPure.Parse(
                        "{\"dark\":[\"kept\",\"\",null,\"also kept\"]}",
                        3);

                    Assert.Equal(new[] { "kept", "also kept" }, result);
                }),
                ("configured maximum bounds accepted entries", () =>
                {
                    var result = DarkMemoryResultParserPure.Parse(
                        "{\"dark\":[\"one\",\"two\",\"three\"]}",
                        2);

                    Assert.Equal(new[] { "one", "two" }, result);
                }),
                ("unrepairable truncation returns no replacement value", () =>
                    Assert.Null(DarkMemoryResultParserPure.Parse(
                        "{\"dark\":[\"unfinished",
                        3))));
        }

        [Fact]
        public void Importance_decay_preserves_threshold_and_pinned_boundaries()
        {
            ContractCaseRunner.Run(
                ("zero and full rates retain calculator bounds", () =>
                {
                    Assert.Equal(0.8f, ImportanceDecayCalculator.Decay(0.8f, 0f));
                    Assert.Equal(0f, ImportanceDecayCalculator.Decay(0.8f, 1f));
                }),
                ("threshold equality is retained while lower values are removed", () =>
                {
                    Assert.False(ImportanceDecayCalculator.ShouldRemove(0.05f, 0.05f));
                    Assert.True(ImportanceDecayCalculator.ShouldRemove(0.049f, 0.05f));
                }),
                ("manager decays active and archive entries", () =>
                {
                    var store = new PawnMemoryStore();
                    store.active.Add(Entry("active", 0.8f));
                    store.archive.Add(Entry("archive", 0.6f));

                    ImportanceDecayManager.ApplyDecay(store, 0.25f, 0.05f);

                    Assert.Equal(0.6f, store.active[0].importance, 3);
                    Assert.Equal(0.45f, store.archive[0].importance, 3);
                }),
                ("pinned and dark state survives decay", () =>
                {
                    var store = new PawnMemoryStore();
                    store.active.Add(Entry("pinned", 0.01f, pinned: true));
                    store.dark.Add(Entry("dark", 0.01f, pinned: true));

                    ImportanceDecayManager.ApplyDecay(store, 0.5f, 0.05f);

                    Assert.Equal(0.01f, Assert.Single(store.active).importance);
                    Assert.Equal(0.01f, Assert.Single(store.dark).importance);
                }));
        }

        [Fact]
        public void Time_context_preserves_elapsed_and_game_date_boundaries()
        {
            ContractCaseRunner.Run(
                ("future events clamp to just now", () =>
                    Assert.Equal("Just now", TimeFormatter.FormatTimeAgo(1000, 0))),
                ("elapsed hours round through the short window", () =>
                {
                    Assert.Equal("About 1h ago", TimeFormatter.FormatTimeAgo(0, 2500));
                    Assert.Equal("About 5h ago", TimeFormatter.FormatTimeAgo(0, 12500));
                }),
                ("same-day history remains today", () =>
                    Assert.Equal("Today", TimeFormatter.FormatTimeAgo(0, 59999))),
                ("three days remains relative", () =>
                    Assert.Equal("3 days ago", TimeFormatter.FormatTimeAgo(0, 180000))),
                ("older history uses a stable game date", () =>
                    Assert.Equal("Day 5 00:00", TimeFormatter.FormatTimeAgo(0, 240000))),
                ("game date preserves day and hour", () =>
                    Assert.Equal("Day 2 06:00", TimeFormatter.FormatGameDate(75000))));
        }

        private static MemoryEntry Entry(string id, float importance, bool pinned = false)
        {
            return new MemoryEntry
            {
                id = id,
                content = id,
                type = MemoryType.Work,
                tick = 1,
                importance = importance,
                isPinned = pinned
            };
        }
    }
}
