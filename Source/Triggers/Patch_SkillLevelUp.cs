using HarmonyLib;
using RimMind.Application.Features.GameEvents;
using RimMind.Domain.ValueObjects;
using RimMind.Memory.Data;
using RimMind.Presentation.Api;
using RimWorld;
using Verse;

namespace RimMind.Memory.Triggers
{
    [HarmonyPatch(typeof(SkillRecord), nameof(SkillRecord.Learn))]
    public static class Patch_SkillLevelUp
    {
        static void Prefix(SkillRecord __instance, float xp, out int __state)
            => __state = __instance != null && xp > 0f && !__instance.TotallyDisabled ? __instance.Level : -1;

        static void Postfix(SkillRecord __instance, int __state)
        {
            int prevLevel = __state;
            if (prevLevel < 0 || __instance == null || __instance.Level <= prevLevel) return;
            if (!MemoryTriggerHelper.ShouldProcess(RimMindMemoryMod.Settings.triggerSkillLevelUp)) return;

            try
            {
                var pawn = __instance.Pawn;
                if (pawn == null || !RimMindPawnLookup.IsActiveSpawnedColonist(pawn) || pawn.Name == null) return;

                RimMindAPI.GameEvents.Publish(new SkillLevelUpGameEvent(
                    pawn.thingIDNumber,
                    __instance.def?.defName ?? string.Empty,
                    prevLevel,
                    __instance.Level));

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
