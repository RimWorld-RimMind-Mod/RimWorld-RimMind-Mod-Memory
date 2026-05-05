using RimMind.Memory.WorkingMemory;
using Xunit;

namespace RimMind.Memory.Tests
{
    public class WorkingMemoryEntryTests
    {
        [Fact]
        public void DefaultConstructor_InitializesDefaults()
        {
            var entry = new WorkingMemoryEntry();
            Assert.Equal("", entry.Content);
            Assert.Equal("", entry.Source);
            Assert.Equal(0, entry.Timestamp);
            Assert.Equal(0f, entry.Relevance);
        }

        [Fact]
        public void ParameterizedConstructor_SetsProperties()
        {
            var entry = new WorkingMemoryEntry("hello world", "test", 0.8f);
            Assert.Equal("hello world", entry.Content);
            Assert.Equal("test", entry.Source);
            Assert.Equal(0.8f, entry.Relevance);
            Assert.Equal(Verse.Find.TickManager.TicksGame, entry.Timestamp);
        }

        [Fact]
        public void ParameterizedConstructor_NullContent_BecomesEmpty()
        {
            var entry = new WorkingMemoryEntry(null!, "src", 0.5f);
            Assert.Equal("", entry.Content);
        }

        [Fact]
        public void ParameterizedConstructor_NullSource_BecomesEmpty()
        {
            var entry = new WorkingMemoryEntry("content", null!, 0.5f);
            Assert.Equal("", entry.Source);
        }

        [Fact]
        public void ParameterizedConstructor_DefaultSource()
        {
            var entry = new WorkingMemoryEntry("content");
            Assert.Equal("", entry.Source);
        }

        [Fact]
        public void ParameterizedConstructor_DefaultRelevance()
        {
            var entry = new WorkingMemoryEntry("content");
            Assert.Equal(0.5f, entry.Relevance);
        }

        [Fact]
        public void Content_SetNull_BecomesEmpty()
        {
            var entry = new WorkingMemoryEntry();
            entry.Content = null!;
            Assert.Equal("", entry.Content);
        }

        [Fact]
        public void Source_SetNull_BecomesEmpty()
        {
            var entry = new WorkingMemoryEntry();
            entry.Source = null!;
            Assert.Equal("", entry.Source);
        }

        [Fact]
        public void Relevance_SetAndGet()
        {
            var entry = new WorkingMemoryEntry();
            entry.Relevance = 0.9f;
            Assert.Equal(0.9f, entry.Relevance);
        }

        [Fact]
        public void Timestamp_SetAndGet()
        {
            var entry = new WorkingMemoryEntry();
            entry.Timestamp = 50000;
            Assert.Equal(50000, entry.Timestamp);
        }
    }
}
