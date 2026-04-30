using System.Linq;
using RimMind.Memory.Data;
using Xunit;

namespace RimMind.Memory.Tests
{
    public class PawnMemoryStoreTests
    {
        private static MemoryEntry MakeEntry(int tick, float importance, bool isPinned = false, MemoryType type = MemoryType.Work)
        {
            return new MemoryEntry
            {
                id = $"mem-{tick}",
                content = $"entry-{tick}",
                type = type,
                tick = tick,
                importance = importance,
                isPinned = isPinned || type == MemoryType.Dark,
            };
        }

        [Fact]
        public void AddActive_InsertsAtHead()
        {
            var store = new PawnMemoryStore();
            store.AddActive(MakeEntry(100, 0.5f), maxActive: 10, maxArchive: 10);
            store.AddActive(MakeEntry(200, 0.5f), maxActive: 10, maxArchive: 10);
            Assert.Equal(2, store.active.Count);
            Assert.Equal(200, store.active[0].tick);
            Assert.Equal(100, store.active[1].tick);
        }

        [Fact]
        public void AddActive_OverCapacity_DemotesToArchive()
        {
            var store = new PawnMemoryStore();
            store.AddActive(MakeEntry(100, 0.5f), maxActive: 2, maxArchive: 10);
            store.AddActive(MakeEntry(200, 0.6f), maxActive: 2, maxArchive: 10);
            store.AddActive(MakeEntry(300, 0.7f), maxActive: 2, maxArchive: 10);

            Assert.Equal(2, store.active.Count);
            Assert.Equal(1, store.archive.Count);
            Assert.Equal(100, store.archive[0].tick);
        }

        [Fact]
        public void AddActive_PinnedNotDemoted()
        {
            var store = new PawnMemoryStore();
            store.AddActive(MakeEntry(100, 0.5f), maxActive: 2, maxArchive: 10);
            store.AddActive(MakeEntry(200, 0.6f), maxActive: 2, maxArchive: 10);
            store.AddActive(MakeEntry(300, 0.7f, isPinned: true), maxActive: 2, maxArchive: 10);

            Assert.Equal(2, store.active.Count);
            Assert.True(store.active.Any(e => e.isPinned));
            Assert.Equal(1, store.archive.Count);
        }

        [Fact]
        public void AddActive_ArchiveOverCapacity_RemovesLowestImportance()
        {
            var store = new PawnMemoryStore();
            store.AddActive(MakeEntry(100, 0.3f), maxActive: 1, maxArchive: 2);
            store.AddActive(MakeEntry(200, 0.5f), maxActive: 1, maxArchive: 2);
            store.AddActive(MakeEntry(300, 0.7f), maxActive: 1, maxArchive: 2);
            store.AddActive(MakeEntry(400, 0.9f), maxActive: 1, maxArchive: 2);

            Assert.Equal(1, store.active.Count);
            Assert.Equal(2, store.archive.Count);
            Assert.All(store.archive, e => Assert.True(e.importance >= 0.5f));
        }

        [Fact]
        public void AddActive_ArchiveSortedByImportance()
        {
            var store = new PawnMemoryStore();
            store.AddActive(MakeEntry(100, 0.3f), maxActive: 1, maxArchive: 10);
            store.AddActive(MakeEntry(200, 0.7f), maxActive: 1, maxArchive: 10);
            store.AddActive(MakeEntry(300, 0.5f), maxActive: 1, maxArchive: 10);

            Assert.Equal(2, store.archive.Count);
            Assert.Equal(0.7f, store.archive[0].importance);
            Assert.Equal(0.3f, store.archive[1].importance);
        }

        [Fact]
        public void AddActive_DarkTypeAutoPinned()
        {
            var store = new PawnMemoryStore();
            var darkEntry = MakeEntry(100, 1.0f, type: MemoryType.Dark);
            store.AddActive(darkEntry, maxActive: 1, maxArchive: 10);

            Assert.True(darkEntry.isPinned);
        }

        [Fact]
        public void IsEmpty_WhenNoEntries_ReturnsTrue()
        {
            var store = new PawnMemoryStore();
            Assert.True(store.IsEmpty);
        }

        [Fact]
        public void IsEmpty_WhenHasEntries_ReturnsFalse()
        {
            var store = new PawnMemoryStore();
            store.AddActive(MakeEntry(100, 0.5f), maxActive: 10, maxArchive: 10);
            Assert.False(store.IsEmpty);
        }

        [Fact]
        public void ContainsId_Exists_ReturnsTrue()
        {
            var store = new PawnMemoryStore();
            var entry = MakeEntry(100, 0.5f);
            store.AddActive(entry, maxActive: 10, maxArchive: 10);
            Assert.True(store.ContainsId(entry.id));
        }

        [Fact]
        public void ContainsId_NotExists_ReturnsFalse()
        {
            var store = new PawnMemoryStore();
            Assert.False(store.ContainsId("nonexistent"));
        }

        [Fact]
        public void ContainsId_ExistsInArchive_ReturnsTrue()
        {
            var store = new PawnMemoryStore();
            var entry = MakeEntry(100, 0.3f);
            store.AddActive(MakeEntry(200, 0.5f), maxActive: 1, maxArchive: 10);
            store.AddActive(entry, maxActive: 1, maxArchive: 10);
            Assert.True(store.ContainsId(entry.id));
        }

        [Fact]
        public void AddIfNotExists_AddsWhenNew()
        {
            var store = new PawnMemoryStore();
            var entry = MakeEntry(100, 0.5f);
            store.AddIfNotExists(entry);
            Assert.Equal(1, store.active.Count);
            Assert.True(store.ContainsId(entry.id));
        }

        [Fact]
        public void AddIfNotExists_SkipsDuplicate()
        {
            var store = new PawnMemoryStore();
            var entry = MakeEntry(100, 0.5f);
            store.AddActive(entry, maxActive: 10, maxArchive: 10);
            store.AddIfNotExists(entry);
            Assert.Equal(1, store.active.Count);
        }

        [Fact]
        public void AddIfNotExists_NullEntry_NoOp()
        {
            var store = new PawnMemoryStore();
            store.AddIfNotExists(null!);
            Assert.True(store.IsEmpty);
        }

        [Fact]
        public void AddIfNotExists_EmptyId_NoOp()
        {
            var store = new PawnMemoryStore();
            var entryWithEmptyId = MakeEntry(100, 0.5f);
            entryWithEmptyId.id = "";
            store.AddIfNotExists(entryWithEmptyId);
            Assert.True(store.IsEmpty);
        }

        [Fact]
        public void ContainsId_ExistsInDark_ReturnsTrue()
        {
            var store = new PawnMemoryStore();
            var entry = MakeEntry(100, 0.5f);
            store.dark.Add(entry);
            Assert.True(store.ContainsId(entry.id));
        }
    }
}
