using HarmonyLib;
using RimMind.Domain.ValueObjects;
using RimMind.Memory.Data;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimMind.Memory.Triggers
{
    [HarmonyPatch(typeof(MentalStateHandler), "TryStartMentalState")]
    public static class Patch_MentalBreak
    {
        static void Postfix(MentalStateHandler __instance, MentalStateDef stateDef, bool __result)
        {
            if (!__result) return;
            if (!RimMindMemoryMod.Settings.enableMemory) return;
            if (!RimMindMemoryMod.Settings.triggerMentalBreak) return;

            try
            {
                var pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
                if (pawn == null || !pawn.IsFreeNonSlaveColonist || pawn.Name == null) return;
                if (stateDef == null) return;

                var wc = RimMindMemoryWorldComponent.Instance;
                if (wc == null) return;

                var settings = RimMindMemoryMod.Settings;
                int now = Find.TickManager.TicksGame;

                float importance = EstimateImportance(stateDef);
                string label = stateDef.LabelCap.RawText.NullOrEmpty() ? stateDef.defName : stateDef.LabelCap.RawText;
                string content = "RimMind.Memory.Trigger.MentalBreak".Translate(pawn.Name.ToStringShort, label);

                wc.AddPawnMemory(pawn, MemoryEntry.Create(content, MemoryType.Event, now, importance),
                    settings.maxActive, settings.maxArchive);

                if (importance >= settings.pawnToNarratorThreshold)
                {
                    wc.AddNarratorMemory(
                        MemoryEntry.Create($"[{pawn.Name.ToStringShort}] {content}", MemoryType.Event, now, importance, pawn.ThingID),
                        settings.narratorMaxActive, settings.narratorMaxArchive);
                }
            }
            catch (System.Exception ex)
            {
                RimMindErrors.Warn($"[RimMind-Memory] Patch_MentalBreak error: {ex.Message}");
            }
        }

        private static float EstimateImportance(MentalStateDef def)
        {
            if (def == MentalStateDefOf.Berserk || def == MentalStateDefOf.BerserkPermanent) return 0.95f;
            if (def == MentalStateDefOf.ManhunterPermanent) return 0.9f;
            string name = def?.defName ?? "";
            if (name.Contains("Extreme") || name.Contains("Berserk") || name.Contains("FireSpreading")) return 0.9f;
            if (name.Contains("Serious") || name.Contains("Major")) return 0.8f;
            return 0.7f;
        }
    }
}
