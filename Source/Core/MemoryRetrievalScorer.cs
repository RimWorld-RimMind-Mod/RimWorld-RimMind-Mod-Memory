using System;
using System.Collections.Generic;
using System.Linq;
using RimMind.Memory.Data;

namespace RimMind.Memory.Core
{
    public static class MemoryRetrievalScorer
    {
        public static float CalculateScore(
            MemoryEntry entry,
            int currentTick,
            string? targetPawnId = null,
            string? targetName = null)
        {
            if (entry == null) return 0f;
            if (entry.isPinned) return 10000f + entry.importance;

            int ageTicks = Math.Max(0, currentTick - entry.tick);
            float daysAgo = ageTicks / 60000f;
            float recency = 1f / (1f + (daysAgo * 0.33f));

            float typeWeight = entry.type switch
            {
                MemoryType.Event => 1.3f,
                MemoryType.Manual => 1.5f,
                MemoryType.Dark => 2.0f,
                _ => 1.0f
            };

            float relevanceBoost = 1.0f;
            if (PawnIdsMatch(entry.targetPawnId, targetPawnId))
            {
                relevanceBoost = 2.0f;
            }
            else if (!string.IsNullOrEmpty(targetName) && !string.IsNullOrEmpty(entry.content) && entry.content.IndexOf(targetName, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                relevanceBoost = 1.75f;
            }

            return entry.importance * recency * typeWeight * relevanceBoost;
        }

        public static string? NormalizePawnId(string? rawId)
        {
            if (string.IsNullOrWhiteSpace(rawId)) return null;
            string trimmed = rawId!.Trim();
            int end = trimmed.Length;
            int start = end;
            while (start > 0 && char.IsDigit(trimmed[start - 1]))
            {
                start--;
            }

            if (start < end)
            {
                if (start == 1 && trimmed[0] == '-')
                    return trimmed;

                return trimmed.Substring(start);
            }

            return trimmed;
        }

        public static bool PawnIdsMatch(string? idA, string? idB)
        {
            string? normA = NormalizePawnId(idA);
            string? normB = NormalizePawnId(idB);
            if (string.IsNullOrEmpty(normA) || string.IsNullOrEmpty(normB))
                return false;
            return string.Equals(normA, normB, StringComparison.OrdinalIgnoreCase);
        }

        public static List<MemoryEntry> SelectTopScored(
            IEnumerable<MemoryEntry> entries,
            int count,
            int currentTick,
            string? targetPawnId = null,
            string? targetName = null)
        {
            if (entries == null || count <= 0) return new List<MemoryEntry>();
            return entries
                .OrderByDescending(e => CalculateScore(e, currentTick, targetPawnId, targetName))
                .Take(count)
                .OrderBy(e => e.tick)
                .ToList();
        }
    }
}
