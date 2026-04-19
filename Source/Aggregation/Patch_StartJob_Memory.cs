using HarmonyLib;
using RimMind.Memory.Aggregation;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimMind.Memory.Aggregation
{
    [HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.StartJob))]
    public static class Patch_StartJob_Memory
    {
        static void Postfix(Pawn_JobTracker __instance, Job newJob)
        {
            if (!RimMindMemoryMod.Settings.enableMemory) return;
            if (!RimMindMemoryMod.Settings.triggerWorkSession) return;
            var pawn = (Pawn)AccessTools.Field(typeof(Pawn_JobTracker), "pawn").GetValue(__instance);
            if (pawn == null || !pawn.IsFreeNonSlaveColonist) return;

            var settings = RimMindMemoryMod.Settings;
            WorkSessionAggregator.Instance?.OnJobStarted(
                pawn, newJob,
                settings.maxActive, settings.maxArchive,
                settings.idleGapThresholdTicks,
                settings.minAggregationCount);
        }
    }
}
