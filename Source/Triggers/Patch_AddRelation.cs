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
        static void Postfix(Pawn_RelationsTracker __instance, PawnRelationDef def, Pawn otherPawn)
        {
            if (!MemoryTriggerHelper.ShouldProcess(RimMindMemoryMod.Settings.triggerRelation)) return;

            try
            {
                var pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
                if (pawn == null || !pawn.IsFreeNonSlaveColonist || pawn.Name == null) return;
                if (otherPawn == null || otherPawn.Name == null) return;
                if (def == null) return;

                float importance = EstimateImportance(def);
                string relLabel = def.LabelCap.RawText.NullOrEmpty() ? def.defName : def.LabelCap.RawText;
                string content = "RimMind.Memory.Trigger.EstablishRelation".Translate(otherPawn.Name.ToStringShort, relLabel);
                MemoryTriggerHelper.WriteMemory(pawn, content, MemoryType.Event, importance);

                if (otherPawn.IsFreeNonSlaveColonist && otherPawn.Name != null)
                {
                    try
                    {
                        string reverseContent = "RimMind.Memory.Trigger.EstablishRelation".Translate(pawn.Name.ToStringShort, relLabel);
                        MemoryTriggerHelper.WriteMemory(otherPawn, reverseContent, MemoryType.Event, importance, upgradeNarrator: false);
                    }
                    catch { }
                }
            }
            catch (System.Exception ex)
            {
                RimMindErrors.Warn($"[RimMind-Memory] Patch_AddRelation error: {ex.Message}");
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
