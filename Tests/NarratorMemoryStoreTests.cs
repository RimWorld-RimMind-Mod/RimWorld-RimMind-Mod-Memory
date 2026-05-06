using RimMind.Memory.Data;
using Xunit;

namespace RimMind.Memory.Tests
{
    public class NarratorMemoryStoreTests
    {
        private static MemoryEntry MakeEntry(int tick, float importance, MemoryType type = MemoryType.Work)
        {
            return new MemoryEntry
            {
                id = $"mem-{tick}",
                content = $"entry-{tick}",
                type = type,
                tick = tick,
                importance = importance,
            };
        }

        [Fact]
        public void AddIfNotExists_Active_AddsWhenNew()
        {
            var store = new NarratorMemoryStore();
            var entry = MakeEntry(100, 0.5f);
            store.AddIfNotExists(entry, isActive: true);
            Assert.Single(store.active);
            Assert.Equal(entry.id, store.active[0].id);
        }

        [Fact]
        public void AddIfNotExists_Archive_AddsWhenNew()
        {
            var store = new NarratorMemoryStore();
            var entry = MakeEntry(100, 0.5f);
            store.AddIfNotExists(entry, isActive: false);
            Assert.Empty(store.active);
            Assert.Single(store.archive);
            Assert.Equal(entry.id, store.archive[0].id);
        }

        [Fact]
        public void AddIfNotExists_SkipsDuplicate()
        {
            var store = new NarratorMemoryStore();
            var entry = MakeEntry(100, 0.5f);
            store.active.Add(entry);
            store.AddIfNotExists(entry, isActive: true);
            Assert.Single(store.active);
        }

        [Fact]
        public void AddIfNotExists_SkipsDuplicateAcrossLists()
        {
            var store = new NarratorMemoryStore();
            var entry = MakeEntry(100, 0.5f);
            store.archive.Add(entry);
            store.AddIfNotExists(entry, isActive: true);
            Assert.Empty(store.active);
        }

        [Fact]
        public void AddIfNotExists_NullEntry_NoOp()
        {
            var store = new NarratorMemoryStore();
            store.AddIfNotExists(null!, isActive: true);
            Assert.True(store.IsEmpty);
        }

        [Fact]
        public void AddIfNotExists_EmptyId_NoOp()
        {
            var store = new NarratorMemoryStore();
            var entryWithEmptyId = MakeEntry(100, 0.5f);
            entryWithEmptyId.id = "";
            store.AddIfNotExists(entryWithEmptyId, isActive: true);
            Assert.True(store.IsEmpty);
        }

        [Fact]
        public void AddIfNotExists_InsertsAtHead()
        {
            var store = new NarratorMemoryStore();
            store.AddIfNotExists(MakeEntry(100, 0.3f), isActive: true);
            store.AddIfNotExists(MakeEntry(200, 0.7f), isActive: true);
            Assert.Equal(2, store.active.Count);
            Assert.Equal(200, store.active[0].tick);
            Assert.Equal(100, store.active[1].tick);
        }
    }
}
