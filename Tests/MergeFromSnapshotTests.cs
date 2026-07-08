using RimMind.Memory.Data;
using Xunit;

namespace RimMind.Memory.Tests
{
    public class MergeFromSnapshotTests
    {
        [Fact]
        public void AddIfNotExists_DarkEntry_RoutesToDarkList()
        {
            var store = new PawnMemoryStore();
            var darkEntry = new MemoryEntry
            {
                id = "mem-dark-1",
                content = "dark memory",
                type = MemoryType.Dark,
                tick = 100,
                importance = 1.0f,
                isPinned = true,
            };

            store.AddIfNotExists(darkEntry);

            Assert.Empty(store.active);
            Assert.Single(store.dark);
            Assert.Equal("mem-dark-1", store.dark[0].id);
        }

        [Fact]
        public void AddIfNotExists_NonDarkEntry_RoutesToActiveList()
        {
            var store = new PawnMemoryStore();
            var entry = new MemoryEntry
            {
                id = "mem-work-1",
                content = "work memory",
                type = MemoryType.Work,
                tick = 100,
                importance = 0.5f,
                isPinned = false,
            };

            store.AddIfNotExists(entry);

            Assert.Single(store.active);
            Assert.Empty(store.dark);
        }

        [Fact]
        public void AddIfNotExists_DarkEntry_NarratorStore_RoutesToDarkList()
        {
            var store = new NarratorMemoryStore();
            var darkEntry = new MemoryEntry
            {
                id = "mem-narrator-dark-1",
                content = "narrator dark",
                type = MemoryType.Dark,
                tick = 100,
                importance = 1.0f,
                isPinned = true,
            };

            store.AddIfNotExists(darkEntry, isActive: false);

            Assert.Empty(store.active);
            Assert.Empty(store.archive);
            Assert.Single(store.dark);
        }
    }
}
