using System.Collections.Generic;
using RimMind.Memory.Data;
using Xunit;

namespace RimMind.Memory.Tests
{
    public class MemoryStoreBaseTests
    {
        private static MemoryEntry MakeEntry(int tick, float importance, bool isPinned = false, MemoryType type = MemoryType.Work)
        {
            return new MemoryEntry
            {
                id = $"mem-base-{tick}-{importance}",
                content = $"base-entry-{tick}",
                type = type,
                tick = tick,
                importance = importance,
                isPinned = isPinned || type == MemoryType.Dark,
            };
        }

        [Fact]
        public void AddActive_InsertsAtHead()
        {
            var store = new MemoryStoreBase();
            store.AddActive(MakeEntry(100, 0.5f), maxActive: 10, maxArchive: 10);
            store.AddActive(MakeEntry(200, 0.6f), maxActive: 10, maxArchive: 10);
            Assert.Equal(2, store.active.Count);
            Assert.Equal(200, store.active[0].tick);
        }

        [Fact]
        public void AddActive_OverCapacity_DemotesToArchive()
        {
            var store = new MemoryStoreBase();
            store.AddActive(MakeEntry(100, 0.3f), maxActive: 1, maxArchive: 10);
            store.AddActive(MakeEntry(200, 0.5f), maxActive: 1, maxArchive: 10);
            Assert.Single(store.active);
            Assert.Single(store.archive);
        }

        [Fact]
        public void AddActive_AllPinnedOverflow_GoesToArchive()
        {
            var store = new MemoryStoreBase();
            store.AddActive(MakeEntry(1, 0.5f, isPinned: true), maxActive: 1, maxArchive: 10);
            store.AddActive(MakeEntry(2, 0.6f), maxActive: 1, maxArchive: 10);
            Assert.Single(store.active);
            Assert.Single(store.archive);
        }

        [Fact]
        public void EnforceLimit_AllPinned_StopsEviction()
        {
            var store = new MemoryStoreBase();
            var src = new List<MemoryEntry>
            {
                MakeEntry(1, 0.5f, isPinned: true),
                MakeEntry(2, 0.6f, isPinned: true),
                MakeEntry(3, 0.7f, isPinned: true),
            };
            var dst = new List<MemoryEntry>();
            MemoryStoreBase.EnforceLimit(src, srcMax: 2, dst, dstMax: 10);
            Assert.Equal(3, src.Count);
        }

        [Fact]
        public void IsEmpty_WhenNoEntries_ReturnsTrue()
        {
            var store = new MemoryStoreBase();
            Assert.True(store.IsEmpty);
        }

        [Fact]
        public void IsEmpty_WhenHasEntries_ReturnsFalse()
        {
            var store = new MemoryStoreBase();
            store.AddActive(MakeEntry(100, 0.5f), maxActive: 10, maxArchive: 10);
            Assert.False(store.IsEmpty);
        }

        [Fact]
        public void ContainsId_ExistsInActive_ReturnsTrue()
        {
            var store = new MemoryStoreBase();
            var entry = MakeEntry(100, 0.5f);
            store.AddActive(entry, maxActive: 10, maxArchive: 10);
            Assert.True(store.ContainsId(entry.id));
        }

        [Fact]
        public void ContainsId_NotExists_ReturnsFalse()
        {
            var store = new MemoryStoreBase();
            Assert.False(store.ContainsId("nonexistent"));
        }

        [Fact]
        public void AddIfNotExists_AddsWhenNew()
        {
            var store = new MemoryStoreBase();
            var entry = MakeEntry(100, 0.5f);
            store.AddIfNotExists(entry);
            Assert.Single(store.active);
            Assert.Equal(entry.id, store.active[0].id);
        }

        [Fact]
        public void AddIfNotExists_SkipsDuplicate()
        {
            var store = new MemoryStoreBase();
            var entry = MakeEntry(100, 0.5f);
            store.AddIfNotExists(entry);
            store.AddIfNotExists(entry);
            Assert.Single(store.active);
        }

        [Fact]
        public void AddIfNotExists_NullOrEmptyId_NoOp()
        {
            var store = new MemoryStoreBase();
            store.AddIfNotExists(null);
            Assert.Empty(store.active);
            store.AddIfNotExists(new MemoryEntry { id = "", content = "empty", type = MemoryType.Work, tick = 1, importance = 0.5f });
            Assert.Empty(store.active);
        }

        [Fact]
        public void ContainsId_ExistsInArchive_ReturnsTrue()
        {
            var store = new MemoryStoreBase();
            var entry = MakeEntry(100, 0.3f);
            store.archive.Add(entry);
            Assert.True(store.ContainsId(entry.id));
        }

        [Fact]
        public void ContainsId_ExistsInDark_ReturnsTrue()
        {
            var store = new MemoryStoreBase();
            var entry = MakeEntry(100, 0.9f, type: MemoryType.Dark);
            store.dark.Add(entry);
            Assert.True(store.ContainsId(entry.id));
        }

        [Fact]
        public void EnforceLimit_DstOverflow_EvictsLeastImportantNonPinned()
        {
            var src = new List<MemoryEntry>();
            var dst = new List<MemoryEntry>
            {
                MakeEntry(10, 0.9f),
                MakeEntry(11, 0.5f),
            };
            MemoryStoreBase.EnforceLimit(src, srcMax: 0, dst, dstMax: 1);
            Assert.Single(dst);
            Assert.Equal(0.9f, dst[0].importance);
        }

        [Fact]
        public void EnforceLimit_InsertsByImportanceOrder()
        {
            var src = new List<MemoryEntry>
            {
                MakeEntry(1, 0.7f),
            };
            var dst = new List<MemoryEntry>
            {
                MakeEntry(10, 0.9f),
                MakeEntry(11, 0.3f),
            };
            MemoryStoreBase.EnforceLimit(src, srcMax: 0, dst, dstMax: 10);
            Assert.Equal(3, dst.Count);
            Assert.Equal(0.9f, dst[0].importance);
            Assert.Equal(0.7f, dst[1].importance);
            Assert.Equal(0.3f, dst[2].importance);
        }
    }
}
