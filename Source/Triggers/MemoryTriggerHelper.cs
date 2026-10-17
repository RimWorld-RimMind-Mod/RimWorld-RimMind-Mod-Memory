using RimMind.Memory.Data;
using Verse;

namespace RimMind.Memory.Triggers
{
    internal static class MemoryTriggerHelper
    {
        internal static bool ShouldProcess(bool triggerEnabled)
        {
            return RimMindMemoryMod.Settings.enableMemory && triggerEnabled;
        }

        internal static void WriteMemory(Pawn pawn, string content, MemoryType type, float importance)
            => WriteMemory(pawn, content, type, importance, upgradeNarrator: true);

        internal static void WriteMemory(Pawn pawn, string content, MemoryType type, float importance, bool upgradeNarrator)
        {
            var wc = RimMindMemoryWorldComponent.Instance;
            if (wc == null) return;

            var settings = RimMindMemoryMod.Settings;
            int now = Find.TickManager.TicksGame;

            wc.AddPawnMemory(pawn,
                MemoryEntry.Create(content, type, now, importance),
                settings.maxActive, settings.maxArchive);

            if (upgradeNarrator && importance >= settings.pawnToNarratorThreshold)
            {
                string pawnName = pawn.Name?.ToStringShort ?? pawn.LabelShort;
                wc.AddNarratorMemory(
                    MemoryEntry.Create($"[{pawnName}] {content}", type, now, importance, pawn.ThingID),
                    settings.narratorMaxActive, settings.narratorMaxArchive);
            }
        }
    }
}
