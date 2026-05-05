using WM = RimMind.Memory.WorkingMemory.WorkingMemory;
using WME = RimMind.Memory.WorkingMemory.WorkingMemoryEntry;
using Xunit;

namespace RimMind.Memory.Tests
{
    public class WorkingMemoryTests
    {
        [Fact]
        public void Add_EntryStored()
        {
            var wm = new WM(5);
            wm.Add("test content", "source", 0.5f);
            Assert.Single(wm.Entries);
        }

        [Fact]
        public void Add_EmptyContent_Skipped()
        {
            var wm = new WM(5);
            wm.Add("", "source", 0.5f);
            Assert.True(wm.IsEmpty);
        }

        [Fact]
        public void Add_NullContent_Skipped()
        {
            var wm = new WM(5);
            wm.Add((string)null!, "source", 0.5f);
            Assert.True(wm.IsEmpty);
        }

        [Fact]
        public void Add_ExceedsCapacity_OldestEvicted()
        {
            var wm = new WM(3);
            wm.Add("1");
            wm.Add("2");
            wm.Add("3");
            wm.Add("4");

            Assert.Equal(3, wm.Entries.Count);
            Assert.Equal("2", wm.Entries[0].Content);
            Assert.Equal("4", wm.Entries[2].Content);
        }

        [Fact]
        public void Add_EntryObject_NullEntry_Skipped()
        {
            var wm = new WM(5);
            wm.Add((WME)null!);
            Assert.True(wm.IsEmpty);
        }

        [Fact]
        public void UpdateCapacity_ReducesCapacity_TrimsEntries()
        {
            var wm = new WM(5);
            wm.Add("1");
            wm.Add("2");
            wm.Add("3");
            wm.Add("4");

            wm.UpdateCapacity(2);
            Assert.Equal(2, wm.Capacity);
            Assert.Equal(2, wm.Entries.Count);
            Assert.Equal("3", wm.Entries[0].Content);
        }

        [Fact]
        public void UpdateCapacity_Zero_ResetsToDefault()
        {
            var wm = new WM(5);
            wm.UpdateCapacity(0);
            Assert.Equal(WM.DefaultCapacity, wm.Capacity);
        }

        [Fact]
        public void UpdateCapacity_Negative_ResetsToDefault()
        {
            var wm = new WM(5);
            wm.UpdateCapacity(-5);
            Assert.Equal(WM.DefaultCapacity, wm.Capacity);
        }

        [Fact]
        public void Clear_RemovesAllEntries()
        {
            var wm = new WM(5);
            wm.Add("1");
            wm.Add("2");
            wm.Clear();
            Assert.True(wm.IsEmpty);
        }

        [Fact]
        public void IsEmpty_InitiallyTrue()
        {
            var wm = new WM(5);
            Assert.True(wm.IsEmpty);
        }

        [Fact]
        public void DefaultCapacity_Is10()
        {
            var wm = new WM();
            Assert.Equal(10, wm.Capacity);
        }
    }
}
