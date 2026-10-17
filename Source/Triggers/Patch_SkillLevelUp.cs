using HarmonyLib;
using RimMind.Domain.ValueObjects;
using RimMind.Memory.Data;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace RimMind.Memory.Triggers
{
    [HarmonyPatch(typeof(SkillRecord), nameof(SkillRecord.Learn))]
    public static class Patch_SkillLevelUp
    {
        static void Prefix(SkillRecord __instance, out int __state) => __state = __instance?.Level ?? 0;

        static void Postfix(SkillRecord __instance, int __state)
        {
            int prevLevel = __state;
            if (__instance == null || __instance.Level <= prevLevel) return;
            if (!MemoryTriggerHelper.ShouldProcess(RimMindMemoryMod.Settings.triggerSkillLevelUp)) return;

            try
            {
                var pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
                if (pawn == null || !pawn.IsFreeNonSlaveColonist || pawn.Name == null) return;

                float importance = __instance.Level >= 15 ? 0.7f : 0.5f;
                string skillLabel = (__instance.def?.LabelCap.RawText.NullOrEmpty() ?? true)
                    ? (__instance.def?.defName ?? "RimMind.Memory.Trigger.Skill".Translate())
                    : __instance.def.LabelCap.RawText;
                string content = "RimMind.Memory.Trigger.SkillUp".Translate(
                    skillLabel, __instance.Level.ToString(), prevLevel.ToString(), __instance.Level.ToString());
                MemoryTriggerHelper.WriteMemory(pawn, content, MemoryType.Event, importance);
            }
            catch (System.Exception ex)
            {
                RimMindErrors.Warn($"[RimMind-Memory] Patch_SkillLevelUp error: {ex.Message}");
            }
        }
    }
}
