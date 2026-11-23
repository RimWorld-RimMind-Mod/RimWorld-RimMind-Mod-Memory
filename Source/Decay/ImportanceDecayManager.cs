using System.Collections.Generic;
using RimMind.Memory.Core;
using RimMind.Memory.Data;

namespace RimMind.Memory.Decay
{
    public static class ImportanceDecayManager
    {
        public static int ApplyDecay(MemoryStoreBase store, float decayRate, float minThreshold)
        {
            if (store == null) return 0;
            lock (store.SyncRoot)
            {
                DecayList(store.active, decayRate);
                DecayList(store.archive, decayRate);
                int removed = store.active.RemoveAll(e => !e.isPinned && ImportanceDecayCalculator.ShouldRemove(e.importance, minThreshold));
                removed += store.archive.RemoveAll(e => !e.isPinned && ImportanceDecayCalculator.ShouldRemove(e.importance, minThreshold));
                return removed;
            }
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
