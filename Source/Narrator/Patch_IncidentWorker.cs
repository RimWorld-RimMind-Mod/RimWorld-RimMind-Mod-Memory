using System;
using HarmonyLib;
using RimMind.Memory.Data;
using RimWorld;
using Verse;

namespace RimMind.Memory.Narrator
{
    [HarmonyPatch(typeof(IncidentWorker), nameof(IncidentWorker.TryExecute))]
    public static class Patch_IncidentWorker
    {
        static void Postfix(IncidentWorker __instance, IncidentParms parms, bool __result)
        {
            if (!__result) return;
            if (!RimMindMemoryMod.Settings.enableMemory) return;

            try
            {
                float importance = GetIncidentImportance(__instance.def);
                var settings = RimMindMemoryMod.Settings;
                if (importance < settings.narratorEventThreshold) return;

                var wc = RimMindMemoryWorldComponent.Instance;
                if (wc == null) return;

                int now = Find.TickManager.TicksGame;
                string content = $"[{"RimMind.Memory.Context.Map".Translate()}] {BuildNarratorText(__instance.def, parms)}";

                wc.AddNarratorMemory(
                    MemoryEntry.Create(content, MemoryType.Event, now, importance),
                    settings.narratorMaxActive, settings.narratorMaxArchive);
            }
            catch (System.Exception ex)
            {
                Log.Warning($"[RimMind-Memory] Patch_IncidentWorker error: {ex.Message}");
            }
        }

        private static float GetIncidentImportance(IncidentDef def)
        {
            string name = def.defName;
            if (name.Contains("Death") || name.Contains("Execution")) return 1.0f;
            if (name.Contains("Funeral") || name.Contains("Burial")) return 0.9f;
            if (name.Contains("Raid") || name.Contains("Siege") || name.Contains("MechCluster")) return 0.9f;
            if (name.Contains("Infestation") || name.Contains("Hive") || name.Contains("Fire") ||
                name.Contains("Explosion") || name.Contains("Tornado") || name.Contains("Meteor")) return 0.85f;
            if (name.Contains("Wedding") || name.Contains("Joiner") || name.Contains("Wanderer")) return 0.85f;
            if (name.Contains("Disease") || name.Contains("Plague") || name.Contains("Flu")) return 0.75f;
            if (name.Contains("Research")) return 0.8f;
            if (name.Contains("Birthday") || name.Contains("Anniversary")) return 0.7f;
            if (name.Contains("Trader") || name.Contains("Visitor") || name.Contains("Caravan")) return 0.6f;
            if (name.Contains("Quest")) return 0.65f;
            if (def.category == IncidentCategoryDefOf.ThreatBig) return 0.9f;
            if (def.category == IncidentCategoryDefOf.ThreatSmall) return 0.7f;
            return 0.3f;
        }

        private static string BuildNarratorText(IncidentDef def, IncidentParms parms)
        {
            string label = def.LabelCap.RawText.NullOrEmpty() ? def.defName : def.LabelCap.RawText;
            if (parms.faction != null)
                return "RimMind.Memory.Context.FromFaction".Translate(label, parms.faction.Name);
            return label;
        }
    }
}
