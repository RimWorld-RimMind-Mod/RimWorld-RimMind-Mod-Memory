using System.Collections.Generic;
using RimMind.Memory.Data;
using RimMind.Testing;
using WM = RimMind.Memory.WorkingMemory.WorkingMemory;
using Xunit;

namespace RimMind.Memory.Tests.Contracts
{
    [Collection("Memory contracts")]
    public sealed class MemoryStoreContracts
    {
        [Fact]
        public void Memory_entries_preserve_identity_and_layer_routing()
        {
            ContractCaseRunner.Run(
                ("writes receive distinct monotonic identities", () =>
                {
                    var first = MemoryEntry.Create("first", MemoryType.Work, 100, 0.4f);
                    var second = MemoryEntry.Create("second", MemoryType.Work, 100, 0.4f);

                    Assert.NotEqual(first.id, second.id);
                    Assert.StartsWith("mem-100-", first.id);
                    Assert.StartsWith("mem-100-", second.id);
                }),
                ("dark entries are pinned and routed to the dark layer", () =>
                {
                    var store = new PawnMemoryStore();
                    var entry = MemoryEntry.Create("private fear", MemoryType.Dark, 200, 0.9f);

                    store.AddIfNotExists(entry);

                    Assert.True(entry.isPinned);
                    Assert.Empty(store.active);
                    Assert.Same(entry, Assert.Single(store.dark));
                }),
                ("duplicate identities are ignored across all layers", () =>
                {
                    var store = new PawnMemoryStore();
                    var entry = Entry("shared-id", 300, 0.5f);
                    store.archive.Add(entry);

                    store.AddIfNotExists(Entry("shared-id", 400, 0.7f));

                    Assert.Empty(store.active);
                    Assert.Same(entry, Assert.Single(store.archive));
                }),
                ("narrator snapshots retain active and archive routing", () =>
                {
                    var store = new NarratorMemoryStore();
                    store.AddIfNotExists(Entry("active", 1, 0.6f), isActive: true);
                    store.AddIfNotExists(Entry("archive", 2, 0.4f), isActive: false);

                    Assert.Equal("active", Assert.Single(store.active).id);
                    Assert.Equal("archive", Assert.Single(store.archive).id);
                }));
        }

        [Fact]
        public void Memory_store_preserves_capacity_and_importance_order()
        {
            ContractCaseRunner.Run(
                ("new writes demote the oldest active entry", () =>
                {
                    var store = new PawnMemoryStore();
                    store.AddActive(Entry("old", 1, 0.3f), maxActive: 1, maxArchive: 3);
                    store.AddActive(Entry("new", 2, 0.5f), maxActive: 1, maxArchive: 3);

                    Assert.Equal("new", Assert.Single(store.active).id);
                    Assert.Equal("old", Assert.Single(store.archive).id);
                }),
                ("archive eviction preserves the most important entries", () =>
                {
                    var source = new List<MemoryEntry>
                    {
                        Entry("medium", 1, 0.6f),
                        Entry("low", 2, 0.2f)
                    };
                    var archive = new List<MemoryEntry>
                    {
                        Entry("high", 3, 0.9f)
                    };

                    MemoryStoreBase.EnforceLimit(source, 0, archive, 2);

                    Assert.Collection(
                        archive,
                        entry => Assert.Equal("high", entry.id),
                        entry => Assert.Equal("medium", entry.id));
                }),
                ("pinned active entries send overflow directly to archive", () =>
                {
                    var store = new PawnMemoryStore();
                    var pinned = Entry("pinned", 1, 1.0f, pinned: true);
                    store.AddActive(pinned, maxActive: 1, maxArchive: 2);

                    store.AddActive(Entry("overflow", 2, 0.4f), maxActive: 1, maxArchive: 2);

                    Assert.Same(pinned, Assert.Single(store.active));
                    Assert.Equal("overflow", Assert.Single(store.archive).id);
                }));
        }

        [Fact]
        public void Legacy_working_memory_loads_and_resaves_without_trimming_existing_entries()
        {
            var entry = new RimMind.Memory.WorkingMemory.WorkingMemoryEntry();
            try
            {
                Verse.ScribeFixture.Loading = true;
                Verse.ScribeFixture.Values = new Dictionary<string, object>
                {
                    ["Content"] = "remember the visitor",
                    ["Timestamp"] = 420,
                    ["Source"] = "dialogue",
                    ["Relevance"] = 0.8f
                };
                entry.ExposeData();
                Assert.Equal("remember the visitor", entry.Content);
                Assert.Equal(420, entry.Timestamp);
                Assert.Equal("dialogue", entry.Source);
                Assert.Equal(0.8f, entry.Relevance);

                // Loading constructs the saved IExposable type without caller arguments.
                var memory = System.Activator.CreateInstance<WM>();
                Verse.ScribeFixture.Values = new Dictionary<string, object>
                {
                    ["capacity"] = 1,
                    ["entries"] = new List<RimMind.Memory.WorkingMemory.WorkingMemoryEntry> { entry, entry }
                };
                memory.ExposeData();
                Assert.Equal(1, memory.Capacity);
                Assert.Equal(2, memory.Entries.Count);
                Assert.False(memory.IsEmpty);

                Verse.ScribeFixture.Loading = false;
                Verse.ScribeFixture.Values = new Dictionary<string, object>();
                memory.ExposeData();
                var saved = Verse.ScribeFixture.Values;
                Verse.ScribeFixture.Loading = true;
                var reloaded = System.Activator.CreateInstance<WM>();
                reloaded.ExposeData();
                Assert.Equal(1, saved["capacity"]);
                Assert.Equal(2, reloaded.Entries.Count);
                Assert.Equal("remember the visitor", reloaded.Entries[0].Content);
            }
            finally
            {
                Verse.ScribeFixture.Values = null;
                Verse.ScribeFixture.Loading = false;
            }
        }

        [Fact]
        public void Legacy_working_memory_missing_from_save_is_empty()
        {
            try
            {
                Verse.ScribeFixture.Loading = true;
                Verse.ScribeFixture.Values = new Dictionary<string, object>();
                var memory = System.Activator.CreateInstance<WM>();
                memory.ExposeData();
                Assert.True(memory.IsEmpty);
                Assert.Empty(memory.Entries);
            }
            finally
            {
                Verse.ScribeFixture.Values = null;
                Verse.ScribeFixture.Loading = false;
            }
        }

        [Fact]
        public void Remote_snapshot_round_trip_preserves_layers_deduplication_and_limits()
        {
            ContractCaseRunner.Run(
                ("layered snapshot round-trips every store", () =>
                {
                    var source = new Dictionary<int, PawnMemoryStore>();
                    var pawn = new PawnMemoryStore();
                    pawn.active.Add(Entry("active", 1, 0.6f));
                    pawn.archive.Add(Entry("archive", 2, 0.5f));
                    pawn.dark.Add(Entry("dark", 3, 0.9f, pinned: true));
                    source[7] = pawn;
                    var narrator = new NarratorMemoryStore();
                    narrator.archive.Add(Entry("narrator", 4, 0.8f));

                    string json = MemorySnapshotService.Serialize(source, narrator);
                    var target = new Dictionary<int, PawnMemoryStore>();
                    var targetNarrator = new NarratorMemoryStore();
                    MemorySnapshotService.Merge(
                        json,
                        target,
                        targetNarrator,
                        new MemorySnapshotLimits(5, 5, 5, 5));

                    Assert.Equal("active", Assert.Single(target[7].active).id);
                    Assert.Equal("archive", Assert.Single(target[7].archive).id);
                    Assert.Equal("dark", Assert.Single(target[7].dark).id);
                    Assert.Equal("narrator", Assert.Single(targetNarrator.archive).id);
                }),
                ("existing identities are not duplicated or rerouted", () =>
                {
                    var source = new Dictionary<int, PawnMemoryStore>();
                    var incoming = new PawnMemoryStore();
                    incoming.active.Add(Entry("same", 2, 0.9f));
                    source[8] = incoming;
                    string json = MemorySnapshotService.Serialize(
                        source,
                        new NarratorMemoryStore());

                    var existing = new PawnMemoryStore();
                    existing.archive.Add(Entry("same", 1, 0.4f));
                    var target = new Dictionary<int, PawnMemoryStore> { [8] = existing };
                    MemorySnapshotService.Merge(
                        json,
                        target,
                        new NarratorMemoryStore(),
                        new MemorySnapshotLimits(5, 5, 5, 5));

                    Assert.Empty(existing.active);
                    Assert.Equal("same", Assert.Single(existing.archive).id);
                }),
                ("merge enforces active and archive limits", () =>
                {
                    var source = new Dictionary<int, PawnMemoryStore>();
                    var incoming = new PawnMemoryStore();
                    incoming.active.Add(Entry("new", 3, 0.9f));
                    incoming.active.Add(Entry("old", 1, 0.2f));
                    source[9] = incoming;

                    var target = new Dictionary<int, PawnMemoryStore>();
                    MemorySnapshotService.Merge(
                        MemorySnapshotService.Serialize(source, new NarratorMemoryStore()),
                        target,
                        new NarratorMemoryStore(),
                        new MemorySnapshotLimits(1, 1, 1, 1));

                    Assert.Single(target[9].active);
                    Assert.Single(target[9].archive);
                }));
        }

        private static MemoryEntry Entry(
            string id,
            int tick,
            float importance,
            bool pinned = false)
        {
            return new MemoryEntry
            {
                id = id,
                content = id,
                type = MemoryType.Work,
                tick = tick,
                importance = importance,
                isPinned = pinned
            };
        }
    }
}
