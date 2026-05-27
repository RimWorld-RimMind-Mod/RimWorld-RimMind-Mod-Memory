using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace RimMind.Memory.DarkMemory
{
    /// <summary>
    /// 纯逻辑版本的暗记忆结果解析器，不依赖 RimMindAPI 或 RimWorld 运行时。
    /// 供测试项目直接编译使用。
    /// </summary>
    internal static class DarkMemoryResultParserPure
    {
        internal class DarkMemoryResultDto
        {
            public string[] dark = Array.Empty<string>();
        }

        private static readonly Regex TrailingCommaRegex = new Regex(
            @",\s*([}\]])",
            RegexOptions.Compiled);

        public static List<string>? Parse(string json, int maxCount)
        {
            try
            {
                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<DarkMemoryResultDto>(json);
                if (result?.dark == null)
                {
                    // 尝试修复截断的JSON后重新反序列化
                    string? repaired = TryRepairTruncatedJson(json);
                    if (repaired != null)
                        result = Newtonsoft.Json.JsonConvert.DeserializeObject<DarkMemoryResultDto>(repaired);
                }
                if (result?.dark == null) return null;

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

        /// <summary>
        /// 尝试修复截断的JSON。若输入已是合法JSON则返回null（无需修复），
        /// 若输入为空白或不可修复则返回null。
        /// </summary>
        private static string? TryRepairTruncatedJson(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            try
            {
                JToken.Parse(input);
                return null; // 已是合法JSON，无需修复
            }
            catch
            {
                return Repair(input);
            }
        }

        private static string Repair(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "{}";

            var result = new StringBuilder(input);
            result = new StringBuilder(TrailingCommaRegex.Replace(result.ToString(), "$1"));
            var balanced = BalanceBrackets(result.ToString());
            return balanced;
        }

        private static string BalanceBrackets(string input)
        {
            int openCurly = 0, openSquare = 0;
            foreach (char c in input)
            {
                if (c == '{') openCurly++;
                else if (c == '}') openCurly--;
                else if (c == '[') openSquare++;
                else if (c == ']') openSquare--;
            }

            var sb = new StringBuilder(input);
            while (openSquare > 0) { sb.Append(']'); openSquare--; }
            while (openCurly > 0) { sb.Append('}'); openCurly--; }
            return sb.ToString();
        }
    }
}
