using System;
using System.Collections.Generic;
using RimMind.Application.Features.Json;
using RimMind.Memory.Data;

namespace RimMind.Memory.DarkMemory
{
    internal static class DarkMemoryResultParser
    {
        internal class DarkMemoryResultDto
        {
            public string[] dark = Array.Empty<string>();
        }

        public static List<string>? Parse(string json, int maxCount)
        {
            try
            {
                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<DarkMemoryResultDto>(json);
                if (result?.dark == null)
                {
                    string? repaired = JsonRepairHelper.TryRepairTruncatedJson(json);
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
    }
}
