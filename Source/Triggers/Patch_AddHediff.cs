using System.Reflection;
using HarmonyLib;
using RimMind.Application.Common.Helpers;
using RimMind.Application.Features.GameEvents;
using RimMind.Domain.ValueObjects;
using RimMind.Memory.Data;
using RimMind.Presentation.Api;
using RimWorld;
using Verse;

namespace RimMind.Memory.Triggers
{
    [HarmonyPatch(typeof(Pawn_HealthTracker), "AddHediff",
        new System.Type[] { typeof(Hediff), typeof(BodyPartRecord), typeof(DamageInfo?), typeof(DamageWorker.DamageResult) })]
    public static class Patch_AddHediff
    {
        public const float MinHediffSeverity = 0.20f;
        public const int InjuryCoalesceWindowTicks = 2500;

        private static readonly FieldInfo? PawnField = AccessTools.Field(typeof(Pawn_HealthTracker), "pawn");
        private static readonly KeyedCooldownGate<int> LastInjuryTickByPawn = new KeyedCooldownGate<int>(256);

        internal static void ClearCooldowns() => LastInjuryTickByPawn.Clear();

        public static bool ShouldRecordHediff(
            bool isBad,
            bool isTendable,
            bool makesSickThought,
            float severity,
            bool isLifeThreatening,
            int nowTick,
            int? lastRecordedTick,
            int coalesceWindowTicks = InjuryCoalesceWindowTicks)
        {
            if (!GameEventSignificancePolicy.IsSignificantInjury(
                    isBad,
                    isTendable,
                    makesSickThought,
                    isLifeThreatening,
                    severity,
                    MinHediffSeverity))
            {
                return false;
            }
            if (!isLifeThreatening
                && lastRecordedTick.HasValue
                && nowTick >= lastRecordedTick.Value
                && (nowTick - lastRecordedTick.Value) < coalesceWindowTicks)
            {
                return false;
            }
            return true;
        }

        static void Postfix(Pawn_HealthTracker __instance, Hediff hediff, DamageInfo? dinfo)
        {
            if (!MemoryTriggerHelper.ShouldProcess(RimMindMemoryMod.Settings.triggerInjury)) return;
            if (hediff == null || hediff.def == null) return;

            bool isLifeThreatening = hediff.CurStage?.lifeThreatening == true
                || (hediff.def.lethalSeverity > 0f && hediff.Severity >= hediff.def.lethalSeverity * 0.5f);
            if (!ShouldRecordHediff(
                    hediff.def.isBad,
                    hediff.def.tendable,
                    hediff.def.makesSickThought,
                    hediff.Severity,
                    isLifeThreatening,
                    0,
                    null))
            {
                return;
            }

            var pawn = PawnField?.GetValue(__instance) as Pawn;
            if (pawn == null || !RimMindPawnLookup.IsActiveSpawnedColonist(pawn) || pawn.Name == null) return;

            try
            {
                int now = Find.TickManager?.TicksGame ?? 0;
                int cooldownTicks = isLifeThreatening ? 0 : InjuryCoalesceWindowTicks;
                if (!LastInjuryTickByPawn.TryEnter(pawn.thingIDNumber, now, cooldownTicks))
                    return;

                RimMindAPI.GameEvents.Publish(new HediffAddedGameEvent(
                    pawn.thingIDNumber,
                    hediff.def.defName ?? string.Empty,
                    hediff.Label ?? hediff.def.label ?? hediff.def.defName ?? string.Empty,
                    hediff.Severity,
                    hediff.def.isBad,
                    hediff.def.tendable,
                    hediff.def.makesSickThought,
                    isLifeThreatening,
                    hediff.Part?.Label));

                float importance = EstimateImportance(hediff);
                string? content = BuildContent(hediff, dinfo);
                if (string.IsNullOrEmpty(content)) return;

                string pawnId = !string.IsNullOrEmpty(pawn.ThingID) ? pawn.ThingID : pawn.thingIDNumber.ToString();
                string? targetPawnId = dinfo != null && dinfo.HasValue && dinfo.Value.Instigator is Pawn attackerPawn
                    ? (!string.IsNullOrEmpty(attackerPawn.ThingID) ? attackerPawn.ThingID : attackerPawn.thingIDNumber.ToString())
                    : null;

                MemoryTriggerHelper.WriteMemory(
                    pawn,
                    content!,
                    MemoryType.Event,
                    importance,
                    upgradeNarrator: true,
                    pawnId: pawnId,
                    targetPawnId: targetPawnId);
            }
            catch (System.Exception ex)
            {
                RimMindErrors.Warn($"[RimMind-Memory] Patch_AddHediff error: {ex.Message}");
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

        private static string? BuildContent(Hediff hediff, DamageInfo? dinfo)
        {
            try
            {
                string label = hediff.def.LabelCap.RawText.NullOrEmpty() ? hediff.def.defName : hediff.def.LabelCap.RawText;
                string part = hediff.Part?.Label ?? RimMindAPI.Text.TrOr("RimMind.Memory.Trigger.FullBody", "Whole Body");

                if (dinfo != null && dinfo.HasValue && dinfo.Value.Instigator != null && !dinfo.Value.Instigator.Destroyed)
                {
                    var instigator = dinfo.Value.Instigator;
                    string attacker = instigator is Pawn p && p.Name != null
                        ? p.Name.ToStringShort
                        : (!instigator.Label.NullOrEmpty()
                            ? instigator.Label
                            : RimMindAPI.Text.TrOr("RimMind.Memory.Trigger.UnknownSource", "Unknown Source"));
                    return RimMindAPI.Text.TrOr(
                        "RimMind.Memory.Trigger.AttackedBy",
                        "Attacked by {0}, causing {1} ({2})",
                        attacker,
                        label,
                        part);
                }
                return RimMindAPI.Text.TrOr(
                    "RimMind.Memory.Trigger.Contracted",
                    "Contracted {0} ({1})",
                    label,
                    part);
            }
            catch (System.Exception ex)
            {
                RimMindErrors.Warn($"[RimMind-Memory] Patch_AddHediff.BuildContent error: {ex.Message}");
                return null;
            }
        }
    }
}
