using HarmonyLib;
using RimMind.Memory.Data;
using RimWorld;
using Verse;

namespace RimMind.Memory.Triggers
{
    [HarmonyPatch(typeof(Pawn_HealthTracker), "AddHediff",
        new System.Type[] { typeof(Hediff), typeof(BodyPartRecord), typeof(DamageInfo?), typeof(DamageWorker.DamageResult) })]
    public static class Patch_AddHediff
    {
        static void Postfix(Pawn_HealthTracker __instance, Hediff hediff, DamageInfo? dinfo)
        {
            if (!RimMindMemoryMod.Settings.enableMemory) return;
            if (!RimMindMemoryMod.Settings.triggerInjury) return;

            var pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            if (pawn == null || !pawn.IsFreeNonSlaveColonist) return;

            if (hediff == null || hediff.def == null) return;
            if (!hediff.def.isBad && !hediff.def.tendable && !hediff.def.makesSickThought) return;

            try
            {
                var wc = RimMindMemoryWorldComponent.Instance;
                if (wc == null) return;

                var settings = RimMindMemoryMod.Settings;
                int now = Find.TickManager.TicksGame;
                float importance = EstimateImportance(hediff);
                string content = BuildContent(pawn, hediff, dinfo);
                if (content.NullOrEmpty()) return;

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
                Log.Warning($"[RimMind-Memory] Patch_AddHediff error: {ex.Message}");
            }
        }

        private static float EstimateImportance(Hediff hediff)
        {
            if (hediff.def.lethalSeverity > 0f) return 0.9f;
            if (hediff.def.chronic) return 0.8f;
            if (hediff.def.tendable) return 0.7f;
            if (hediff.def.makesSickThought) return 0.6f;
            return 0.5f;
        }

        private static string BuildContent(Pawn pawn, Hediff hediff, DamageInfo? dinfo)
        {
            try
            {
                string label = hediff.def.LabelCap.RawText.NullOrEmpty() ? hediff.def.defName : hediff.def.LabelCap.RawText;
                string part = hediff.Part?.Label ?? "RimMind.Memory.Trigger.FullBody".Translate();

                if (dinfo != null && dinfo.HasValue && dinfo.Value.Instigator != null && !dinfo.Value.Instigator.Destroyed)
                {
                    var instigator = dinfo.Value.Instigator;
                    string attacker = instigator is Pawn p && p.Name != null
                        ? p.Name.ToStringShort
                        : (!instigator.Label.NullOrEmpty() ? instigator.Label : "RimMind.Memory.Trigger.UnknownSource".Translate());
                    return "RimMind.Memory.Trigger.AttackedBy".Translate(attacker, label, part);
                }
                return "RimMind.Memory.Trigger.Contracted".Translate(label, part);
            }
            catch
            {
                return null!;
            }
        }
    }
}
