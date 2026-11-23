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
            => WriteMemory(pawn, content, type, importance, upgradeNarrator: true, pawnId: null, targetPawnId: null);

        internal static void WriteMemory(
            Pawn pawn,
            string content,
            MemoryType type,
            float importance,
            bool upgradeNarrator,
            string? pawnId = null,
            string? targetPawnId = null)
        {
            var wc = RimMindMemoryWorldComponent.Instance;
            if (wc == null) return;

            var settings = RimMindMemoryMod.Settings;
            int now = Find.TickManager.TicksGame;
            string resolvedPawnId = !string.IsNullOrEmpty(pawnId)
                ? pawnId!
                : (!string.IsNullOrEmpty(pawn.ThingID) ? pawn.ThingID : pawn.thingIDNumber.ToString());

            wc.AddPawnMemory(pawn,
                MemoryEntry.Create(content, type, now, importance, pawnId, targetPawnId),
                settings.maxActive, settings.maxArchive);

            if (upgradeNarrator && importance >= settings.pawnToNarratorThreshold)
            {
                string pawnName = pawn.Name?.ToStringShort ?? pawn.LabelShort;
                wc.AddNarratorMemory(
                    MemoryEntry.Create($"[{pawnName}] {content}", type, now, importance, resolvedPawnId, targetPawnId),
                    settings.narratorMaxActive, settings.narratorMaxArchive);
            }
        }
    }
}
