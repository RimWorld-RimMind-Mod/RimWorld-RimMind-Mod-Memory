using HarmonyLib;
using RimMind.Memory.Data;
using RimWorld;
using Verse;

namespace RimMind.Memory.Triggers
{
    [HarmonyPatch(typeof(Pawn_RelationsTracker), nameof(Pawn_RelationsTracker.AddDirectRelation))]
    public static class Patch_AddRelation
    {
        static void Postfix(Pawn_RelationsTracker __instance, PawnRelationDef def, Pawn otherPawn)
        {
            if (!RimMindMemoryMod.Settings.enableMemory) return;
            if (!RimMindMemoryMod.Settings.triggerRelation) return;

            try
            {
                var pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
                if (pawn == null || !pawn.IsFreeNonSlaveColonist || pawn.Name == null) return;
                if (otherPawn == null || otherPawn.Name == null) return;
                if (def == null) return;

                var wc = RimMindMemoryWorldComponent.Instance;
                if (wc == null) return;

                var settings = RimMindMemoryMod.Settings;
                int now = Find.TickManager.TicksGame;

                float importance = EstimateImportance(def);
                string relLabel = def.LabelCap.RawText.NullOrEmpty() ? def.defName : def.LabelCap.RawText;
                string content = "RimMind.Memory.Trigger.EstablishRelation".Translate(otherPawn.Name.ToStringShort, relLabel);

                wc.AddPawnMemory(pawn, MemoryEntry.Create(content, MemoryType.Event, now, importance),
                    settings.maxActive, settings.maxArchive);

                if (otherPawn.IsFreeNonSlaveColonist && otherPawn.Name != null)
                {
                    try
                    {
                        wc.AddPawnMemory(otherPawn,
                            MemoryEntry.Create(
                                "RimMind.Memory.Trigger.EstablishRelation".Translate(pawn.Name.ToStringShort, relLabel),
                                MemoryType.Event, now, importance),
                            settings.maxActive, settings.maxArchive);
                    }
                    catch { }
                }

                if (importance >= settings.pawnToNarratorThreshold)
                {
                    wc.AddNarratorMemory(
                        MemoryEntry.Create($"[{pawn.Name.ToStringShort}] {content}", MemoryType.Event, now, importance, pawn.ThingID),
                        settings.narratorMaxActive, settings.narratorMaxArchive);
                }
            }
            catch (System.Exception ex)
            {
                Log.Warning($"[RimMind-Memory] Patch_AddRelation error: {ex.Message}");
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
