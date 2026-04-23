using HarmonyLib;
using RimMind.Memory.Data;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace RimMind.Memory.Triggers
{
    [HarmonyPatch(typeof(SkillRecord), nameof(SkillRecord.Learn))]
    public static class Patch_SkillLevelUp
    {
        private static readonly Dictionary<SkillRecord, int> _previousLevels = new Dictionary<SkillRecord, int>();

        static void Prefix(SkillRecord __instance)
        {
            _previousLevels[__instance] = __instance.Level;
        }

        static void Postfix(SkillRecord __instance)
        {
            if (!_previousLevels.TryGetValue(__instance, out int prevLevel)) return;
            _previousLevels.Remove(__instance);

            if (__instance.Level <= prevLevel) return;
            if (!RimMindMemoryMod.Settings.enableMemory) return;
            if (!RimMindMemoryMod.Settings.triggerSkillLevelUp) return;

            try
            {
                var pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
                if (pawn == null || !pawn.IsFreeNonSlaveColonist || pawn.Name == null) return;

                var wc = RimMindMemoryWorldComponent.Instance;
                if (wc == null) return;

                var settings = RimMindMemoryMod.Settings;
                int now = Find.TickManager.TicksGame;

                float importance = __instance.Level >= 15 ? 0.7f : 0.5f;
                string skillLabel = (__instance.def?.LabelCap.RawText.NullOrEmpty() ?? true)
                    ? (__instance.def?.defName ?? "RimMind.Memory.Trigger.Skill".Translate())
                    : __instance.def.LabelCap.RawText;
                string content = "RimMind.Memory.Trigger.SkillUp".Translate(
                    skillLabel, __instance.Level.ToString(), prevLevel.ToString(), __instance.Level.ToString());

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
                Log.Warning($"[RimMind-Memory] Patch_SkillLevelUp error: {ex.Message}");
            }
        }
    }
}
