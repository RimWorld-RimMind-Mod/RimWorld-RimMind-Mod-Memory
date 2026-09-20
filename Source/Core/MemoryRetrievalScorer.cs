using System;
using System.Collections.Generic;
using System.Linq;
using RimMind.Memory.Data;

namespace RimMind.Memory.Core
{
    public static class MemoryRetrievalScorer
    {
        public static float CalculateScore(MemoryEntry entry, int currentTick)
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

            return entry.importance * recency * typeWeight;
        }

        public static List<MemoryEntry> SelectTopScored(IEnumerable<MemoryEntry> entries, int count, int currentTick)
        {
            if (entries == null || count <= 0) return new List<MemoryEntry>();
            return entries
                .OrderByDescending(e => CalculateScore(e, currentTick))
                .Take(count)
                .OrderBy(e => e.tick)
                .ToList();
        }
    }
}
