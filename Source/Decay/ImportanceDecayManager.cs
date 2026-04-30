using System.Collections.Generic;
using RimMind.Memory.Core;
using RimMind.Memory.Data;

namespace RimMind.Memory.Decay
{
    public static class ImportanceDecayManager
    {
        public static void ApplyDecay(PawnMemoryStore store, float decayRate, float minThreshold)
        {
            if (store == null) return;
            DecayList(store.active, decayRate);
            DecayList(store.archive, decayRate);
            store.active.RemoveAll(e => !e.isPinned && ImportanceDecayCalculator.ShouldRemove(e.importance, minThreshold));
            store.archive.RemoveAll(e => !e.isPinned && ImportanceDecayCalculator.ShouldRemove(e.importance, minThreshold));
        }

        public static void ApplyDecay(NarratorMemoryStore store, float decayRate, float minThreshold)
        {
            if (store == null) return;
            DecayList(store.active, decayRate);
            DecayList(store.archive, decayRate);
            store.active.RemoveAll(e => !e.isPinned && ImportanceDecayCalculator.ShouldRemove(e.importance, minThreshold));
            store.archive.RemoveAll(e => !e.isPinned && ImportanceDecayCalculator.ShouldRemove(e.importance, minThreshold));
        }

        private static void DecayList(List<MemoryEntry> entries, float decayRate)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (!entries[i].isPinned)
                    entries[i].importance = ImportanceDecayCalculator.Decay(entries[i].importance, decayRate);
            }
        }
    }
}
