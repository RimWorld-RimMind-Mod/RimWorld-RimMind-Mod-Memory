using RimMind.Memory.Data;
using RimMind.Memory.Decay;
using Xunit;

namespace RimMind.Memory.Tests
{
    public class ImportanceDecayManagerTests
    {
        private static MemoryEntry MakeEntry(float importance, bool isPinned = false)
        {
            return new MemoryEntry
            {
                id = $"mem-{System.Guid.NewGuid():N}".Substring(0, 12),
                content = "test content",
                type = MemoryType.Work,
                tick = 1000,
                importance = importance,
                isPinned = isPinned,
            };
        }

        [Fact]
        public void ApplyDecay_PawnMemoryStore_DecaysActiveEntries()
        {
            var store = new PawnMemoryStore();
            store.active.Add(MakeEntry(0.8f));
            store.active.Add(MakeEntry(0.5f));

            ImportanceDecayManager.ApplyDecay(store, decayRate: 0.1f, minThreshold: 0.05f);

            Assert.True(store.active[0].importance < 0.8f);
            Assert.True(store.active[1].importance < 0.5f);
        }

        [Fact]
        public void ApplyDecay_PawnMemoryStore_SkipsPinnedEntries()
        {
            var store = new PawnMemoryStore();
            var pinned = MakeEntry(0.8f, isPinned: true);
            store.active.Add(pinned);

            ImportanceDecayManager.ApplyDecay(store, decayRate: 0.5f, minThreshold: 0.05f);

            Assert.Equal(0.8f, store.active[0].importance);
        }

        [Fact]
        public void ApplyDecay_PawnMemoryStore_RemovesBelowThreshold()
        {
            var store = new PawnMemoryStore();
            store.active.Add(MakeEntry(0.8f));
            store.active.Add(MakeEntry(0.02f));

            ImportanceDecayManager.ApplyDecay(store, decayRate: 0.1f, minThreshold: 0.05f);

            Assert.Single(store.active);
            Assert.True(store.active[0].importance >= 0.05f);
        }

        [Fact]
        public void ApplyDecay_PawnMemoryStore_DoesNotRemovePinnedBelowThreshold()
        {
            var store = new PawnMemoryStore();
            var pinned = MakeEntry(0.02f, isPinned: true);
            store.active.Add(pinned);

            ImportanceDecayManager.ApplyDecay(store, decayRate: 0.5f, minThreshold: 0.05f);

            Assert.Single(store.active);
        }

        [Fact]
        public void ApplyDecay_PawnMemoryStore_DecaysArchiveEntries()
        {
            var store = new PawnMemoryStore();
            store.archive.Add(MakeEntry(0.6f));

            ImportanceDecayManager.ApplyDecay(store, decayRate: 0.1f, minThreshold: 0.05f);

            Assert.True(store.archive[0].importance < 0.6f);
        }

        [Fact]
        public void ApplyDecay_NarratorMemoryStore_DecaysActiveEntries()
        {
            var store = new NarratorMemoryStore();
            store.active.Add(MakeEntry(0.7f));

            ImportanceDecayManager.ApplyDecay(store, decayRate: 0.1f, minThreshold: 0.05f);

            Assert.True(store.active[0].importance < 0.7f);
        }

        [Fact]
        public void ApplyDecay_NullStore_DoesNotThrow()
        {
            PawnMemoryStore? nullStore = null;
            ImportanceDecayManager.ApplyDecay(nullStore!, decayRate: 0.1f, minThreshold: 0.05f);
        }

        [Fact]
        public void ApplyDecay_EmptyStore_DoesNotThrow()
        {
            var store = new PawnMemoryStore();
            ImportanceDecayManager.ApplyDecay(store, decayRate: 0.1f, minThreshold: 0.05f);
            Assert.True(store.IsEmpty);
        }
    }
}
