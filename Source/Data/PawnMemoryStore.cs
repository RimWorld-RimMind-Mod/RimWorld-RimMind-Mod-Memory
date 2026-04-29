using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimMind.Memory.Data
{
    public class PawnMemoryStore : IExposable
    {
        public List<MemoryEntry> active = new List<MemoryEntry>();
        public List<MemoryEntry> archive = new List<MemoryEntry>();
        public List<MemoryEntry> dark = new List<MemoryEntry>();

        public void AddActive(MemoryEntry e, int maxActive, int maxArchive)
        {
            if (active.Count >= maxActive && active.All(x => x.isPinned))
            {
                archive.Insert(0, e);
                return;
            }
            active.Insert(0, e);
            EnforceLimit(active, maxActive, archive, maxArchive);
        }

        public static void EnforceLimit(List<MemoryEntry> src, int srcMax,
                                         List<MemoryEntry> dst, int dstMax)
        {
            while (src.Count > srcMax)
            {
                var evict = src.LastOrDefault(x => !x.isPinned);
                if (evict == null)
                {
                    Log.Warning($"[RimMind-Memory] Cannot enforce limit: all {src.Count} entries in src are pinned (max={srcMax})");
                    break;
                }
                src.Remove(evict);
                int insertIdx = dst.FindIndex(x => x.importance < evict.importance);
                if (insertIdx < 0) dst.Add(evict);
                else dst.Insert(insertIdx, evict);
            }
            while (dst.Count > dstMax)
            {
                var least = dst.LastOrDefault(x => !x.isPinned);
                if (least != null) dst.Remove(least);
                else
                {
                    Log.Warning($"[RimMind-Memory] Cannot enforce limit: all {dst.Count} entries in dst are pinned (max={dstMax})");
                    break;
                }
            }
        }

        public bool IsEmpty => active.Count == 0 && archive.Count == 0 && dark.Count == 0;

        public void ExposeData()
        {
            Scribe_Collections.Look(ref active, "active", LookMode.Deep);
            Scribe_Collections.Look(ref archive, "archive", LookMode.Deep);
            Scribe_Collections.Look(ref dark, "dark", LookMode.Deep);
            active ??= new List<MemoryEntry>();
            archive ??= new List<MemoryEntry>();
            dark ??= new List<MemoryEntry>();
        }
    }
}
