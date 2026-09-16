using Verse;

namespace RimMind.Memory.Data
{
    public class PawnMemoryStore : MemoryStoreBase
    {
        // All shared logic (AddActive, EnforceLimit, IsEmpty, ContainsId, AddIfNotExists, ExposeData)
        // is inherited from MemoryStoreBase.
        // PawnMemoryStore uses the default AddIfNotExists (always inserts to active).
    }
}
