using RimMind.Memory.DarkMemory;
using Xunit;

namespace RimMind.Memory.Tests
{
    public class DarkMemoryResultParserTests
    {
        [Fact]
        public void Parse_ValidJson_ReturnsEntries()
        {
            string json = @"{""dark"": [""Pawn feels lonely"", ""Pawn remembers the raid""]}";
            var result = DarkMemoryResultParser.Parse(json, maxCount: 10);

            Assert.NotNull(result);
            Assert.Equal(2, result!.Count);
            Assert.Equal("Pawn feels lonely", result[0]);
            Assert.Equal("Pawn remembers the raid", result[1]);
        }

        [Fact]
        public void Parse_ExceedsMaxCount_Truncates()
        {
            string json = @"{""dark"": [""a"", ""b"", ""c"", ""d"", ""e""]}";
            var result = DarkMemoryResultParser.Parse(json, maxCount: 3);

            Assert.NotNull(result);
            Assert.Equal(3, result!.Count);
        }

        [Fact]
        public void Parse_EmptyArray_ReturnsEmptyList()
        {
            string json = @"{""dark"": []}";
            var result = DarkMemoryResultParser.Parse(json, maxCount: 10);

            Assert.NotNull(result);
            Assert.Empty(result!);
        }

        [Fact]
        public void Parse_SkipsEmptyEntries()
        {
            string json = @"{""dark"": [""valid"", """", null, ""also valid""]}";
            var result = DarkMemoryResultParser.Parse(json, maxCount: 10);

            Assert.NotNull(result);
            Assert.Equal(2, result!.Count);
        }

        [Fact]
        public void Parse_NullDark_ReturnsEmptyList()
        {
            string json = @"{""other"": ""field""}";
            var result = DarkMemoryResultParser.Parse(json, maxCount: 10);
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void Parse_InvalidJson_ReturnsNull()
        {
            var result = DarkMemoryResultParser.Parse("not json", maxCount: 10);
            Assert.Null(result);
        }

        [Fact]
        public void Parse_TruncatedJson_ReturnsNull()
        {
            string truncated = "{\"dark\": [\"memory1\"";
            var result = DarkMemoryResultParser.Parse(truncated, maxCount: 10);
            Assert.Null(result);
        }

        [Fact]
        public void Parse_MaxCountZero_ReturnsEmptyList()
        {
            string json = @"{""dark"": [""a""]}";
            var result = DarkMemoryResultParser.Parse(json, maxCount: 0);

            Assert.NotNull(result);
            Assert.Empty(result!);
        }
    }
}
