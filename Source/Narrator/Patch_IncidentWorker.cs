using System;
using System.Collections.Generic;
using HarmonyLib;
using RimMind.Domain.ValueObjects;
using RimMind.Memory.Data;
using RimMind.Presentation.Api;
using RimWorld;
using Verse;

namespace RimMind.Memory.Narrator
{
    [HarmonyPatch(typeof(IncidentWorker), nameof(IncidentWorker.TryExecute))]
    public static class Patch_IncidentWorker
    {
        private static readonly Dictionary<string, float> CategoryImportances =
            new Dictionary<string, float>(StringComparer.Ordinal)
            {
                ["ThreatBig"] = 0.9f,
                ["DiseaseHuman"] = 0.75f,
                ["ThreatSmall"] = 0.7f,
                ["GiveQuest"] = 0.65f,
                ["OrbitalVisitor"] = 0.6f,
                ["FactionArrival"] = 0.6f,
            };
        private static readonly (string Token, float Importance)[] NameTokenImportances =
        {
            ("Death", 1.0f),
            ("Execution", 1.0f),
            ("Funeral", 0.9f),
            ("Burial", 0.9f),
            ("Raid", 0.9f),
            ("Siege", 0.9f),
            ("MechCluster", 0.9f),
            ("Infestation", 0.85f),
            ("Hive", 0.85f),
            ("Fire", 0.85f),
            ("Explosion", 0.85f),
            ("Tornado", 0.85f),
            ("Meteor", 0.85f),
            ("Wedding", 0.85f),
            ("Joiner", 0.85f),
            ("Wanderer", 0.85f),
            ("Research", 0.8f),
            ("Disease", 0.75f),
            ("Plague", 0.75f),
            ("Flu", 0.75f),
            ("Birthday", 0.7f),
            ("Anniversary", 0.7f),
            ("Quest", 0.65f),
            ("Trader", 0.6f),
            ("Visitor", 0.6f),
            ("Caravan", 0.6f),
        };

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
                string mapTag = RimMindAPI.Text.TrOr("RimMind.Memory.Context.Map", "[Map]");
                string content = $"{mapTag} {BuildNarratorText(__instance.def, parms)}";

                wc.AddNarratorMemory(
                    MemoryEntry.Create(content, MemoryType.Event, now, importance),
                    settings.narratorMaxActive, settings.narratorMaxArchive);
            }
            catch (System.Exception ex)
            {
                RimMindErrors.Warn($"[RimMind-Memory] Patch_IncidentWorker error: {ex.Message}");
            }
        }

        private static float GetIncidentImportance(IncidentDef def)
        {
            if (def == null) return 0.3f;

            // Prefer structured RimWorld IncidentCategoryDef metadata first.
            float categoryScore = 0.3f;
            string? catName = def.category?.defName;
            if (!string.IsNullOrEmpty(catName) && CategoryImportances.TryGetValue(catName!, out float mapped))
            {
                categoryScore = mapped;
            }
            else if (def.category == IncidentCategoryDefOf.ThreatBig)
            {
                categoryScore = 0.9f;
            }
            else if (def.category == IncidentCategoryDefOf.ThreatSmall)
            {
                categoryScore = 0.7f;
            }

            string name = def.defName ?? string.Empty;
            for (int i = 0; i < NameTokenImportances.Length; i++)
            {
                if (name.Contains(NameTokenImportances[i].Token) && NameTokenImportances[i].Importance > categoryScore)
                    return NameTokenImportances[i].Importance;
            }
            return categoryScore;
        }

        private static string BuildNarratorText(IncidentDef def, IncidentParms parms)
        {
            string label = def.LabelCap.RawText.NullOrEmpty() ? def.defName : def.LabelCap.RawText;
            if (parms.faction != null)
                return RimMindAPI.Text.TrOr("RimMind.Memory.Context.FromFaction", "({0} from {1})", label, parms.faction.Name);
            return label;
        }
    }
}
