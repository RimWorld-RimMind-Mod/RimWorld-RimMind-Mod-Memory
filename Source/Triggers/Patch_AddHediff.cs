using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
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
        private static readonly object CooldownLock = new object();
        private static readonly Dictionary<int, int> LastInjuryTickByPawn = new Dictionary<int, int>();

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
            if (!isBad && !isTendable && !makesSickThought) return false;
            if (!isLifeThreatening && severity < MinHediffSeverity) return false;
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
                lock (CooldownLock)
                {
                    int? lastTick = LastInjuryTickByPawn.TryGetValue(pawn.thingIDNumber, out int recorded) ? recorded : (int?)null;
                    if (!ShouldRecordHediff(
                            hediff.def.isBad,
                            hediff.def.tendable,
                            hediff.def.makesSickThought,
                            hediff.Severity,
                            isLifeThreatening,
                            now,
                            lastTick))
                    {
                        return;
                    }

                    if (LastInjuryTickByPawn.Count > 128)
                    {
                        var stale = new List<int>();
                        foreach (var kv in LastInjuryTickByPawn)
                        {
                            if (now < kv.Value || (now - kv.Value) >= InjuryCoalesceWindowTicks * 4)
                                stale.Add(kv.Key);
                        }
                        for (int i = 0; i < stale.Count; i++)
                            LastInjuryTickByPawn.Remove(stale[i]);
                    }

                    LastInjuryTickByPawn[pawn.thingIDNumber] = now;
                }

                float importance = EstimateImportance(hediff);
                string content = BuildContent(pawn, hediff, dinfo);
                if (content.NullOrEmpty()) return;

                var wc = RimMindMemoryWorldComponent.Instance;
                if (wc == null) return;
                var settings = RimMindMemoryMod.Settings;
                string? pawnId = !string.IsNullOrEmpty(pawn.ThingID) ? pawn.ThingID : pawn.thingIDNumber.ToString();
                string? targetPawnId = dinfo != null && dinfo.HasValue && dinfo.Value.Instigator is Pawn attackerPawn
                    ? (!string.IsNullOrEmpty(attackerPawn.ThingID) ? attackerPawn.ThingID : attackerPawn.thingIDNumber.ToString())
                    : null;

                wc.AddPawnMemory(
                    pawn,
                    MemoryEntry.Create(content, MemoryType.Event, now, importance, pawnId, targetPawnId),
                    settings.maxActive,
                    settings.maxArchive);

                if (importance >= settings.pawnToNarratorThreshold)
                {
                    string pawnName = pawn.Name?.ToStringShort ?? pawn.LabelShort;
                    wc.AddNarratorMemory(
                        MemoryEntry.Create($"[{pawnName}] {content}", MemoryType.Event, now, importance, pawnId, targetPawnId),
                        settings.narratorMaxActive,
                        settings.narratorMaxArchive);
                }
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
