using RimMind.Memory.Data;
using Xunit;

namespace RimMind.Memory.Tests
{
    // NarratorMemoryStore 的 AddActive 和边界场景补充测试
    public class NarratorMemoryStoreExtendedTests
    {
        private static MemoryEntry MakeEntry(int tick, float importance, bool isPinned = false, MemoryType type = MemoryType.Work)
        {
            return new MemoryEntry
            {
                id = $"mem-nr-{tick}-{importance}",
                content = $"narrator-{tick}",
                type = type,
                tick = tick,
                importance = importance,
                isPinned = isPinned || type == MemoryType.Dark,
            };
        }

        [Fact]
        public void AddActive_InsertsAtHead()
        {
            var store = new NarratorMemoryStore();
            store.AddActive(MakeEntry(100, 0.5f), maxActive: 10, maxArchive: 10);
            store.AddActive(MakeEntry(200, 0.6f), maxActive: 10, maxArchive: 10);

            Assert.Equal(2, store.active.Count);
            Assert.Equal(200, store.active[0].tick);
        }

        [Fact]
        public void AddActive_OverCapacity_DemotesToArchive()
        {
            var store = new NarratorMemoryStore();
            store.AddActive(MakeEntry(100, 0.5f), maxActive: 2, maxArchive: 10);
            store.AddActive(MakeEntry(200, 0.6f), maxActive: 2, maxArchive: 10);
            store.AddActive(MakeEntry(300, 0.7f), maxActive: 2, maxArchive: 10);

            Assert.Equal(2, store.active.Count);
            Assert.Single(store.archive);
        }

        [Fact]
        public void AddActive_PinnedNotDemoted()
        {
            var store = new NarratorMemoryStore();
            store.AddActive(MakeEntry(100, 0.5f), maxActive: 2, maxArchive: 10);
            store.AddActive(MakeEntry(200, 0.6f, isPinned: true), maxActive: 2, maxArchive: 10);
            store.AddActive(MakeEntry(300, 0.7f), maxActive: 2, maxArchive: 10);

            Assert.Equal(2, store.active.Count);
            Assert.Contains(store.active, e => e.isPinned);
        }

        [Fact]
        public void AddActive_AllPinnedOverflow_GoesToArchive()
        {
            var store = new NarratorMemoryStore();
            store.AddActive(MakeEntry(1, 0.5f, isPinned: true), maxActive: 1, maxArchive: 10);
            store.AddActive(MakeEntry(2, 0.6f), maxActive: 1, maxArchive: 10);

            Assert.Single(store.active);
            Assert.Single(store.archive);
        }

        [Fact]
        public void IsEmpty_WhenHasArchiveOnly_ReturnsFalse()
        {
            var store = new NarratorMemoryStore();
            store.archive.Add(MakeEntry(100, 0.5f));
            Assert.False(store.IsEmpty);
        }

        [Fact]
        public void IsEmpty_WhenHasDarkOnly_ReturnsFalse()
        {
            var store = new NarratorMemoryStore();
            store.dark.Add(MakeEntry(100, 0.5f));
            Assert.False(store.IsEmpty);
        }

        [Fact]
        public void AddIfNotExists_DuplicateInDark_Skips()
        {
            var store = new NarratorMemoryStore();
            var entry = MakeEntry(100, 0.5f);
            store.dark.Add(entry);
            store.AddIfNotExists(entry, isActive: true);

            // dark 中已存在，不应重复添加到 active
            Assert.Empty(store.active);
        }

        [Fact]
        public void AddIfNotExists_ArchiveInsertsAtHead()
        {
            var store = new NarratorMemoryStore();
            store.AddIfNotExists(MakeEntry(100, 0.3f), isActive: false);
            store.AddIfNotExists(MakeEntry(200, 0.7f), isActive: false);

            Assert.Equal(2, store.archive.Count);
            Assert.Equal(200, store.archive[0].tick);
        }
    }
}
