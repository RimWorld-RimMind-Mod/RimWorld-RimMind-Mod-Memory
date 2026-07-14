using System.Collections.Generic;
using System.Linq;
using RimMind.Application.Common.Interfaces.Memory;
using RimMind.Application.Common.Models.Memory;
using RimMind.Memory.Data;

namespace RimMind.Memory.Core
{
    /// <summary>RimMind-Memory's typed implementation of Core's optional memory capability.</summary>
    internal sealed class RimMindMemoryBridge : IMemoryBridge
    {
        public bool AddPawnMemory(string content, MemoryKind kind, int tick, float importance, string? pawnId)
        {
            if (!RimMindMemoryMod.Settings.enableMemory) return false;

            var pawn = PawnLookup.FindPawnById(pawnId ?? string.Empty);
            var world = RimMindMemoryWorldComponent.Instance;
            if (pawn == null || world == null) return false;

            world.AddPawnMemory(pawn, MemoryEntry.Create(content, ToMemoryType(kind), tick, importance),
                RimMindMemoryMod.Settings.maxActive, RimMindMemoryMod.Settings.maxArchive);
            return true;
        }

        public bool AddNarratorMemory(string content, int tick, float importance)
        {
            if (!RimMindMemoryMod.Settings.enableMemory) return false;

            var world = RimMindMemoryWorldComponent.Instance;
            if (world == null) return false;

            world.AddNarratorMemory(MemoryEntry.Create(content, MemoryType.Event, tick, importance),
                RimMindMemoryMod.Settings.narratorMaxActive, RimMindMemoryMod.Settings.narratorMaxArchive);
            return true;
        }

        public IReadOnlyList<NarratorMemoryEntry> GetRecentNarrations(int maxEntries)
        {
            if (maxEntries <= 0 || !RimMindMemoryMod.Settings.enableMemory)
                return System.Array.Empty<NarratorMemoryEntry>();

            var world = RimMindMemoryWorldComponent.Instance;
            if (world == null) return System.Array.Empty<NarratorMemoryEntry>();

            return world.GetNarratorMemories()
                .Take(maxEntries)
                .Select(entry => new NarratorMemoryEntry(entry.content, entry.tick))
                .ToList();
        }

        private static MemoryType ToMemoryType(MemoryKind kind)
        {
            return kind switch
            {
                MemoryKind.Work => MemoryType.Work,
                MemoryKind.Manual => MemoryType.Manual,
                MemoryKind.Dark => MemoryType.Dark,
                _ => MemoryType.Event,
            };
        }
    }
}
