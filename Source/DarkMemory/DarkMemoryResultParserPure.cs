using System;
using System.Collections.Generic;
using RimMind.Presentation.Api;

namespace RimMind.Memory.DarkMemory
{
    /// <summary>
    /// 纯逻辑版本的暗记忆结果解析器，委托 Core <see cref="RimMindAPI.Json"/> 进行清洗、提取与修复。
    /// 供生产与测试项目共用。
    /// </summary>
    public static class DarkMemoryResultParserPure
    {
        public const string SchemaKey = "DarkMemoryOutput";
        public const string DarkMemoryOutputSchema =
            "{\"type\":\"object\",\"properties\":{\"dark\":{\"type\":\"array\",\"items\":{\"type\":\"string\"}}},\"required\":[\"dark\"],\"additionalProperties\":false}";

        public class DarkMemoryResultDto
        {
            public string[] dark = Array.Empty<string>();
        }

        public static List<string>? Parse(string json, int maxCount)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                string? payload = RimMindAPI.Json.ExtractJsonPayload(json);
                if (string.IsNullOrWhiteSpace(payload) || HasUnclosedString(payload!))
                    return null;

                if (!RimMindAPI.Json.TryDeserializeWithRepair<DarkMemoryResultDto>(payload, out var result)
                    || result?.dark == null)
                {
                    return null;
                }

                var entries = new List<string>();
                foreach (var text in result.dark)
                {
                    if (string.IsNullOrEmpty(text)) continue;
                    if (entries.Count >= maxCount) break;
                    entries.Add(text);
                }
                return entries;
            }
            catch
            {
                return null;
            }
        }

        private static bool HasUnclosedString(string input)
        {
            bool inString = false;
            bool escaped = false;
            foreach (char c in input)
            {
                if (inString)
                {
                    if (escaped) escaped = false;
                    else if (c == '\\') escaped = true;
                    else if (c == '"') inString = false;
                }
                else if (c == '"')
                {
                    inString = true;
                }
            }
            return inString;
        }
    }
}
