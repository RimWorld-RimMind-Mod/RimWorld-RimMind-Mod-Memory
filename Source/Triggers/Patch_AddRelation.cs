using System.Reflection;
using HarmonyLib;
using RimMind.Domain.ValueObjects;
using RimMind.Memory.Data;
using RimWorld;
using Verse;

namespace RimMind.Memory.Triggers
{
    [HarmonyPatch(typeof(Pawn_RelationsTracker), nameof(Pawn_RelationsTracker.AddDirectRelation))]
    public static class Patch_AddRelation
    {
        private static readonly FieldInfo? PawnField = AccessTools.Field(typeof(Pawn_RelationsTracker), "pawn");

        static void Postfix(Pawn_RelationsTracker __instance, PawnRelationDef def, Pawn otherPawn)
        {
            if (!MemoryTriggerHelper.ShouldProcess(RimMindMemoryMod.Settings.triggerRelation)) return;

            try
            {
                var pawn = PawnField?.GetValue(__instance) as Pawn;
                if (pawn == null || !RimMind.Presentation.Api.RimMindPawnLookup.IsActiveSpawnedColonist(pawn) || pawn.Name == null) return;
                if (otherPawn == null || otherPawn.Name == null) return;
                if (def == null) return;

                float importance = EstimateImportance(def);
                string relLabel = def.LabelCap.RawText.NullOrEmpty() ? def.defName : def.LabelCap.RawText;
                string content = "RimMind.Memory.Trigger.EstablishRelation".Translate(otherPawn.Name.ToStringShort, relLabel);
                WriteRelationMemory(pawn, otherPawn, content, importance, upgradeNarrator: true);

                if (RimMind.Presentation.Api.RimMindPawnLookup.IsActiveSpawnedColonist(otherPawn) && otherPawn.Name != null)
                {
                    try
                    {
                        string reverseContent = "RimMind.Memory.Trigger.EstablishRelation".Translate(pawn.Name.ToStringShort, relLabel);
                        WriteRelationMemory(otherPawn, pawn, reverseContent, importance, upgradeNarrator: false);
                    }
                    catch { }
                }
            }
            catch (System.Exception ex)
            {
                RimMindErrors.Warn($"[RimMind-Memory] Patch_AddRelation error: {ex.Message}");
            }
        }

        private static void WriteRelationMemory(Pawn pawn, Pawn targetPawn, string content, float importance, bool upgradeNarrator)
        {
            var wc = RimMindMemoryWorldComponent.Instance;
            if (wc == null) return;

            var settings = RimMindMemoryMod.Settings;
            int now = Find.TickManager.TicksGame;
            string pawnId = !string.IsNullOrEmpty(pawn.ThingID) ? pawn.ThingID : pawn.thingIDNumber.ToString();
            string targetPawnId = !string.IsNullOrEmpty(targetPawn.ThingID) ? targetPawn.ThingID : targetPawn.thingIDNumber.ToString();

            wc.AddPawnMemory(
                pawn,
                MemoryEntry.Create(content, MemoryType.Event, now, importance, pawnId, targetPawnId),
                settings.maxActive,
                settings.maxArchive);

            if (upgradeNarrator && importance >= settings.pawnToNarratorThreshold)
            {
                string pawnName = pawn.Name?.ToStringShort ?? pawn.LabelShort;
                wc.AddNarratorMemory(
                    MemoryEntry.Create($"[{pawnName}] {content}", MemoryType.Event, now, importance, pawnId, targetPawnId),
                    settings.narratorMaxActive,
                    settings.narratorMaxArchive);
            }
        }

        private static float EstimateImportance(PawnRelationDef def)
        {
            if (def == null) return 0.6f;
            if (def == PawnRelationDefOf.Spouse || def == PawnRelationDefOf.Lover) return 0.95f;
            if (def == PawnRelationDefOf.Fiance) return 0.9f;
            if (def == PawnRelationDefOf.Parent || def == PawnRelationDefOf.Child) return 0.9f;
            if (def == PawnRelationDefOf.Sibling) return 0.85f;
            if (def == PawnRelationDefOf.Bond) return 0.85f;
            return 0.6f;
        }
    }
}
