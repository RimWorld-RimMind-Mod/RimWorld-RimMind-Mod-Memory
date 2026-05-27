using WM = RimMind.Memory.WorkingMemory.WorkingMemory;
using WME = RimMind.Memory.WorkingMemory.WorkingMemoryEntry;
using Xunit;

namespace RimMind.Memory.Tests
{
    // WorkingMemory 的补充测试：WorkingMemoryEntry 对象添加、容量边界
    public class WorkingMemoryExtendedTests
    {
        [Fact]
        public void Add_EntryObject_WithContent_Stores()
        {
            var wm = new WM(5);
            var entry = new WME("hello", "source", 0.7f);
            wm.Add(entry);
            Assert.Single(wm.Entries);
            Assert.Equal("hello", wm.Entries[0].Content);
        }

        [Fact]
        public void Add_EntryObject_EmptyContent_Skipped()
        {
            var wm = new WM(5);
            var entry = new WME("", "source", 0.5f);
            wm.Add(entry);
            Assert.True(wm.IsEmpty);
        }

        [Fact]
        public void Add_MultipleEntries_MaintainsOrder()
        {
            var wm = new WM(10);
            wm.Add("first");
            wm.Add("second");
            wm.Add("third");

            Assert.Equal(3, wm.Entries.Count);
            Assert.Equal("first", wm.Entries[0].Content);
            Assert.Equal("second", wm.Entries[1].Content);
            Assert.Equal("third", wm.Entries[2].Content);
        }

        [Fact]
        public void UpdateCapacity_IncreaseCapacity_NoDataLoss()
        {
            var wm = new WM(3);
            wm.Add("1");
            wm.Add("2");
            wm.Add("3");

            wm.UpdateCapacity(10);
            Assert.Equal(10, wm.Capacity);
            Assert.Equal(3, wm.Entries.Count);
        }

        [Fact]
        public void Constructor_ZeroCapacity_UsesDefault()
        {
            var wm = new WM(0);
            Assert.Equal(WM.DefaultCapacity, wm.Capacity);
        }

        [Fact]
        public void Constructor_NegativeCapacity_UsesDefault()
        {
            var wm = new WM(-10);
            Assert.Equal(WM.DefaultCapacity, wm.Capacity);
        }

        [Fact]
        public void Capacity_ReadOnlyFromOutside()
        {
            var wm = new WM(5);
            Assert.Equal(5, wm.Capacity);
            // Capacity 只能通过 UpdateCapacity 修改
        }

        [Fact]
        public void Entries_IsReadOnlyInterface()
        {
            var wm = new WM(5);
            wm.Add("test");
            // Entries 属性返回 IReadOnlyList 接口
            Assert.IsAssignableFrom<System.Collections.Generic.IReadOnlyList<WME>>(wm.Entries);
        }

        [Fact]
        public void Add_WithSourceAndRelevance()
        {
            var wm = new WM(5);
            wm.Add("content", "my-source", 0.9f);
            Assert.Single(wm.Entries);
            Assert.Equal("my-source", wm.Entries[0].Source);
            Assert.Equal(0.9f, wm.Entries[0].Relevance);
        }

        [Fact]
        public void Clear_AfterMultipleAdds_Empty()
        {
            var wm = new WM(5);
            for (int i = 0; i < 5; i++)
                wm.Add($"item-{i}");
            Assert.Equal(5, wm.Entries.Count);

            wm.Clear();
            Assert.True(wm.IsEmpty);
            Assert.Equal(5, wm.Capacity); // 容量不变
        }

        [Fact]
        public void Add_ExactlyAtCapacity_NoEviction()
        {
            var wm = new WM(3);
            wm.Add("1");
            wm.Add("2");
            wm.Add("3");
            Assert.Equal(3, wm.Entries.Count);
            Assert.Equal("1", wm.Entries[0].Content);
        }
    }
}
