using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimMind.Memory.Data
{
    public class NarratorMemoryStore : IExposable
    {
        public List<MemoryEntry> active = new List<MemoryEntry>();
        public List<MemoryEntry> archive = new List<MemoryEntry>();
        public List<MemoryEntry> dark = new List<MemoryEntry>();

        public void AddActive(MemoryEntry e, int maxActive, int maxArchive)
        {
            active.Insert(0, e);
            PawnMemoryStore.EnforceLimit(active, maxActive, archive, maxArchive);
        }

        public bool IsEmpty => active.Count == 0 && archive.Count == 0 && dark.Count == 0;

        private bool ContainsId(string id)
        {
            return active.Any(e => e.id == id)
                || archive.Any(e => e.id == id)
                || dark.Any(e => e.id == id);
        }

        public void AddIfNotExists(MemoryEntry entry, bool isActive)
        {
            if (entry == null || string.IsNullOrEmpty(entry.id)) return;
            if (ContainsId(entry.id)) return;
            if (isActive)
                active.Insert(0, entry);
            else
                archive.Insert(0, entry);
        }

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
