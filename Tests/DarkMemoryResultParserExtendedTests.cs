using RimMind.Memory.DarkMemory;
using Xunit;

namespace RimMind.Memory.Tests
{
    // DarkMemoryResultParserPure 的 JSON 修复和边界场景补充测试
    public class DarkMemoryResultParserExtendedTests
    {
        [Fact]
        public void Parse_TrailingCommaInArray_Repaired()
        {
            // 数组末尾多余逗号
            string json = @"{""dark"": [""memory1"", ]}";
            var result = DarkMemoryResultParserPure.Parse(json, maxCount: 10);
            Assert.NotNull(result);
            Assert.Single(result!);
            Assert.Equal("memory1", result[0]);
        }

        [Fact]
        public void Parse_TrailingCommaInObject_Repaired()
        {
            // 对象末尾多余逗号
            string json = @"{""dark"": [""a""], }";
            var result = DarkMemoryResultParserPure.Parse(json, maxCount: 10);
            Assert.NotNull(result);
            Assert.Single(result!);
        }

        [Fact]
        public void Parse_UnclosedArrayBracket_ReturnsNull()
        {
            // 缺少 ] 闭合，反序列化失败直接返回 null
            string json = @"{""dark"": [""memory1"", ""memory2""}";
            var result = DarkMemoryResultParserPure.Parse(json, maxCount: 10);
            Assert.Null(result);
        }

        [Fact]
        public void Parse_UnclosedCurlyBracket_ReturnsNull()
        {
            // 缺少 } 闭合，反序列化失败直接返回 null
            string json = @"{""dark"": [""memory1""]";
            var result = DarkMemoryResultParserPure.Parse(json, maxCount: 10);
            Assert.Null(result);
        }

        [Fact]
        public void Parse_UnclosedBothBrackets_ReturnsNull()
        {
            // 缺少 ] }，反序列化失败直接返回 null
            string json = @"{""dark"": [""memory1"", ""memory2""";
            var result = DarkMemoryResultParserPure.Parse(json, maxCount: 10);
            Assert.Null(result);
        }

        [Fact]
        public void Parse_EmptyString_ReturnsNull()
        {
            var result = DarkMemoryResultParserPure.Parse("", maxCount: 10);
            Assert.Null(result);
        }

        [Fact]
        public void Parse_WhitespaceOnly_ReturnsNull()
        {
            var result = DarkMemoryResultParserPure.Parse("   \n\t  ", maxCount: 10);
            Assert.Null(result);
        }

        [Fact]
        public void Parse_SingleEntry_MaxCountOne()
        {
            string json = @"{""dark"": [""only one""]}";
            var result = DarkMemoryResultParserPure.Parse(json, maxCount: 1);
            Assert.NotNull(result);
            Assert.Single(result!);
            Assert.Equal("only one", result[0]);
        }

        [Fact]
        public void Parse_NullEntries_Skipped()
        {
            string json = @"{""dark"": [null, ""valid"", null]}";
            var result = DarkMemoryResultParserPure.Parse(json, maxCount: 10);
            Assert.NotNull(result);
            Assert.Single(result!);
            Assert.Equal("valid", result[0]);
        }

        [Fact]
        public void Parse_DarkFieldIsNotArray_ReturnsNull()
        {
            // dark 字段不是数组
            string json = @"{""dark"": ""not an array""}";
            var result = DarkMemoryResultParserPure.Parse(json, maxCount: 10);
            Assert.Null(result);
        }

        [Fact]
        public void Parse_DarkFieldIsNumber_ReturnsNull()
        {
            string json = @"{""dark"": 42}";
            var result = DarkMemoryResultParserPure.Parse(json, maxCount: 10);
            Assert.Null(result);
        }

        [Fact]
        public void Parse_LargeMaxCount_NoTruncation()
        {
            string json = @"{""dark"": [""a"", ""b"", ""c""]}";
            var result = DarkMemoryResultParserPure.Parse(json, maxCount: 1000);
            Assert.NotNull(result);
            Assert.Equal(3, result!.Count);
        }

        [Fact]
        public void Parse_UnicodeContent_Parsed()
        {
            string json = @"{""dark"": [""\u4e2d\u6587\u5185\u5bb9""]}";
            var result = DarkMemoryResultParserPure.Parse(json, maxCount: 10);
            Assert.NotNull(result);
            Assert.Single(result!);
        }
    }
}
