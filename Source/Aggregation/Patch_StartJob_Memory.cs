using System.Reflection;
using HarmonyLib;
using RimMind.Memory.Aggregation;
using RimMind.Presentation.Api;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimMind.Memory.Aggregation
{
    [HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.StartJob))]
    public static class Patch_StartJob_Memory
    {
        private static readonly FieldInfo? PawnField = AccessTools.Field(typeof(Pawn_JobTracker), "pawn");

        static void Postfix(Pawn_JobTracker __instance, Job newJob)
        {
            if (!RimMindMemoryMod.Settings.enableMemory) return;
            if (!RimMindMemoryMod.Settings.triggerWorkSession) return;
            var pawn = PawnField?.GetValue(__instance) as Pawn;
            if (pawn == null || !RimMindPawnLookup.IsActiveSpawnedColonist(pawn)) return;

            var settings = RimMindMemoryMod.Settings;
            WorkSessionAggregator.Instance?.OnJobStarted(
                pawn, newJob,
                settings.maxActive, settings.maxArchive,
                settings.idleGapThresholdTicks,
                settings.minAggregationCount);
        }
    }
}
