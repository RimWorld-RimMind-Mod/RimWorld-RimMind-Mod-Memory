using Verse;

namespace RimMind.Memory.Data
{
    public class NarratorMemoryStore : MemoryStoreBase
    {
        // NarratorMemoryStore overrides AddIfNotExists with isActive parameter
        // to route entries to active or archive. Dark entries always go to dark list.

        public void AddIfNotExists(MemoryEntry? entry, bool isActive)
        {
            if (entry == null || string.IsNullOrEmpty(entry.id)) return;
            if (ContainsId(entry.id)) return;
            if (entry.type == MemoryType.Dark)
                dark.Add(entry);
            else if (isActive)
                active.Insert(0, entry);
            else
                archive.Insert(0, entry);
        }
    }
}
