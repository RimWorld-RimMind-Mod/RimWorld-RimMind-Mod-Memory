using RimMind.Memory.Data;
using RimMind.Memory.Core;
using RimMind.Memory.Decay;
using Xunit;

namespace RimMind.Memory.Tests
{
    // ImportanceDecayCalculator 和 ImportanceDecayManager 的补充测试
    public class ImportanceDecayExtendedTests
    {
        [Fact]
        public void Decay_HighRate_LargeReduction()
        {
            float result = ImportanceDecayCalculator.Decay(1.0f, 0.9f);
            Assert.Equal(0.1f, result, 3);
        }

        [Fact]
        public void Decay_SmallRate_MinimalReduction()
        {
            float result = ImportanceDecayCalculator.Decay(1.0f, 0.01f);
            Assert.Equal(0.99f, result, 3);
        }

        [Fact]
        public void Decay_ZeroImportance_NoChange()
        {
            float result = ImportanceDecayCalculator.Decay(0f, 0.5f);
            Assert.Equal(0f, result);
        }

        [Fact]
        public void ShouldRemove_ZeroImportance_BelowAnyPositiveThreshold()
        {
            Assert.True(ImportanceDecayCalculator.ShouldRemove(0f, 0.01f));
        }

        [Fact]
        public void ShouldRemove_ZeroThreshold_NeverRemoves()
        {
            Assert.False(ImportanceDecayCalculator.ShouldRemove(0.001f, 0f));
        }

        [Fact]
        public void ApplyDecay_NarratorStore_NullStore_DoesNotThrow()
        {
            NarratorMemoryStore? nullStore = null;
            ImportanceDecayManager.ApplyDecay(nullStore!, decayRate: 0.1f, minThreshold: 0.05f);
        }

        [Fact]
        public void ApplyDecay_NarratorStore_RemovesBelowThreshold()
        {
            var store = new NarratorMemoryStore();
            store.active.Add(new MemoryEntry
            {
                id = "mem-low",
                content = "low",
                type = MemoryType.Work,
                tick = 100,
                importance = 0.02f,
                isPinned = false,
            });
            store.active.Add(new MemoryEntry
            {
                id = "mem-high",
                content = "high",
                type = MemoryType.Work,
                tick = 200,
                importance = 0.8f,
                isPinned = false,
            });

            ImportanceDecayManager.ApplyDecay(store, decayRate: 0.1f, minThreshold: 0.05f);

            Assert.Single(store.active);
            Assert.True(store.active[0].importance >= 0.05f);
        }

        [Fact]
        public void ApplyDecay_NarratorStore_SkipsPinned()
        {
            var store = new NarratorMemoryStore();
            var pinned = new MemoryEntry
            {
                id = "mem-pinned",
                content = "pinned",
                type = MemoryType.Dark,
                tick = 100,
                importance = 0.02f,
                isPinned = true,
            };
            store.active.Add(pinned);

            ImportanceDecayManager.ApplyDecay(store, decayRate: 0.5f, minThreshold: 0.05f);

            Assert.Single(store.active);
            Assert.Equal(0.02f, store.active[0].importance);
        }

        [Fact]
        public void ApplyDecay_NarratorStore_DecaysArchive()
        {
            var store = new NarratorMemoryStore();
            store.archive.Add(new MemoryEntry
            {
                id = "mem-arch",
                content = "archive",
                type = MemoryType.Work,
                tick = 100,
                importance = 0.6f,
                isPinned = false,
            });

            ImportanceDecayManager.ApplyDecay(store, decayRate: 0.1f, minThreshold: 0.05f);

            Assert.True(store.archive[0].importance < 0.6f);
        }

        [Fact]
        public void ApplyDecay_PawnStore_MultipleEntries_AllDecayed()
        {
            var store = new PawnMemoryStore();
            store.active.Add(new MemoryEntry
            {
                id = "mem-a1",
                content = "a1",
                type = MemoryType.Work,
                tick = 100,
                importance = 0.9f,
                isPinned = false,
            });
            store.active.Add(new MemoryEntry
            {
                id = "mem-a2",
                content = "a2",
                type = MemoryType.Work,
                tick = 200,
                importance = 0.5f,
                isPinned = false,
            });
            store.archive.Add(new MemoryEntry
            {
                id = "mem-ar1",
                content = "ar1",
                type = MemoryType.Work,
                tick = 300,
                importance = 0.3f,
                isPinned = false,
            });

            ImportanceDecayManager.ApplyDecay(store, decayRate: 0.2f, minThreshold: 0.05f);

            // 所有非pinned条目都应被衰减
            Assert.True(store.active[0].importance < 0.9f);
            Assert.True(store.active[1].importance < 0.5f);
            Assert.True(store.archive[0].importance < 0.3f);
        }
    }
}
