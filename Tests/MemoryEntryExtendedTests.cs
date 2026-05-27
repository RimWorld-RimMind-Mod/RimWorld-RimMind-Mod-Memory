using RimMind.Memory.Data;
using Xunit;

namespace RimMind.Memory.Tests
{
    // MemoryEntry 的补充测试：id 格式、序列号递增、边界值
    public class MemoryEntryExtendedTests
    {
        [Fact]
        public void Create_IdContainsTick()
        {
            var entry = MemoryEntry.Create("test", MemoryType.Work, 12345, 0.5f);
            Assert.Contains("12345", entry.id);
            Assert.StartsWith("mem-", entry.id);
        }

        [Fact]
        public void Create_SequentialIds_AreUnique()
        {
            var e1 = MemoryEntry.Create("a", MemoryType.Work, 100, 0.5f);
            var e2 = MemoryEntry.Create("b", MemoryType.Work, 100, 0.5f);
            Assert.NotEqual(e1.id, e2.id);
        }

        [Fact]
        public void Create_DifferentTicks_AreUnique()
        {
            var e1 = MemoryEntry.Create("a", MemoryType.Work, 100, 0.5f);
            var e2 = MemoryEntry.Create("b", MemoryType.Work, 200, 0.5f);
            Assert.NotEqual(e1.id, e2.id);
        }

        [Fact]
        public void Create_DefaultPawnId_IsNull()
        {
            var entry = MemoryEntry.Create("test", MemoryType.Work, 100, 0.5f);
            Assert.Null(entry.pawnId);
        }

        [Fact]
        public void Create_AllMemoryTypes()
        {
            var work = MemoryEntry.Create("w", MemoryType.Work, 100, 0.5f);
            var evt = MemoryEntry.Create("e", MemoryType.Event, 200, 0.5f);
            var manual = MemoryEntry.Create("m", MemoryType.Manual, 300, 0.5f);
            var dark = MemoryEntry.Create("d", MemoryType.Dark, 400, 0.5f);

            Assert.Equal(MemoryType.Work, work.type);
            Assert.Equal(MemoryType.Event, evt.type);
            Assert.Equal(MemoryType.Manual, manual.type);
            Assert.Equal(MemoryType.Dark, dark.type);
        }

        [Fact]
        public void Create_ImportancePreserved()
        {
            var entry = MemoryEntry.Create("test", MemoryType.Work, 100, 0.75f);
            Assert.Equal(0.75f, entry.importance);
        }

        [Fact]
        public void Create_TickPreserved()
        {
            var entry = MemoryEntry.Create("test", MemoryType.Work, 99999, 0.5f);
            Assert.Equal(99999, entry.tick);
        }

        [Fact]
        public void Create_ZeroImportance()
        {
            var entry = MemoryEntry.Create("test", MemoryType.Work, 100, 0f);
            Assert.Equal(0f, entry.importance);
        }

        [Fact]
        public void Create_MaxImportance()
        {
            var entry = MemoryEntry.Create("test", MemoryType.Work, 100, 1f);
            Assert.Equal(1f, entry.importance);
        }

        [Fact]
        public void DefaultConstructor_DefaultValues()
        {
            var entry = new MemoryEntry();
            Assert.Equal(string.Empty, entry.id);
            Assert.Equal(string.Empty, entry.content);
            Assert.Equal(MemoryType.Work, entry.type);
            Assert.Equal(0, entry.tick);
            Assert.Equal(0f, entry.importance);
            Assert.False(entry.isPinned);
            Assert.Null(entry.pawnId);
        }
    }
}
