using System;
using RimMind.Memory.Core;
using RimMind.Memory.Data;
using Verse;

namespace RimMind.Memory
{
    public static class RimMindMemoryAPI
    {
        public static bool AddMemory(string content, string memoryType, int tick, float importance, string? pawnId = null)
        {
            var type = Enum.TryParse<MemoryType>(memoryType, out var t) ? t : MemoryType.Event;

            var pawn = PawnLookup.FindPawnById(pawnId ?? "");
            if (pawn == null) return false;

            var wc = RimMindMemoryWorldComponent.Instance;
            if (wc == null) return false;

            var settings = RimMindMemoryMod.Settings;
            wc.AddPawnMemory(pawn,
                MemoryEntry.Create(content, type, tick, importance),
                settings?.maxActive ?? 30,
                settings?.maxArchive ?? 50);
            return true;
        }
    }
}
