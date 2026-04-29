using RimMind.Memory.Data;
using Xunit;

namespace RimMind.Memory.Tests
{
    public class MemoryEntryTests
    {
        [Fact]
        public void Create_ShortContent_NotTruncated()
        {
            var entry = MemoryEntry.Create("hello", MemoryType.Work, 100, 0.5f);
            Assert.Equal("hello", entry.content);
            Assert.Equal(5, entry.content.Length);
        }

        [Fact]
        public void Create_ExactLimit_NotTruncated()
        {
            var content = new string('x', 2000);
            var entry = MemoryEntry.Create(content, MemoryType.Event, 200, 0.7f);
            Assert.Equal(content, entry.content);
            Assert.Equal(2000, entry.content.Length);
        }

        [Fact]
        public void Create_OverLimit_Truncated()
        {
            var content = new string('y', 3000);
            var entry = MemoryEntry.Create(content, MemoryType.Manual, 300, 0.9f);
            Assert.Equal(2003, entry.content.Length);
            Assert.EndsWith("...", entry.content);
            Assert.StartsWith(new string('y', 2000), entry.content);
        }

        [Fact]
        public void Create_OneOverLimit_Truncated()
        {
            var content = new string('z', 2001);
            var entry = MemoryEntry.Create(content, MemoryType.Dark, 400, 1.0f);
            Assert.Equal(2003, entry.content.Length);
            Assert.EndsWith("...", entry.content);
        }

        [Fact]
        public void Create_EmptyContent_NotTruncated()
        {
            var entry = MemoryEntry.Create("", MemoryType.Work, 500, 0.3f);
            Assert.Equal("", entry.content);
        }

        [Fact]
        public void Create_DarkType_IsPinned()
        {
            var entry = MemoryEntry.Create("test", MemoryType.Dark, 600, 0.5f);
            Assert.True(entry.isPinned);
        }

        [Fact]
        public void Create_NonDarkType_NotPinned()
        {
            var entry = MemoryEntry.Create("test", MemoryType.Work, 700, 0.5f);
            Assert.False(entry.isPinned);
        }

        [Fact]
        public void Create_SetsPawnId()
        {
            var entry = MemoryEntry.Create("test", MemoryType.Event, 800, 0.5f, "pawn-123");
            Assert.Equal("pawn-123", entry.pawnId);
        }
    }
}
