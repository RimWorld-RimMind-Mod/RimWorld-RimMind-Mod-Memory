using RimMind.Domain.ValueObjects;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimMind.Memory.DarkMemory;
using RimMind.Memory.Data;
using RimWorld;
using Verse;

namespace RimMind.Memory.Triggers
{
    [HarmonyPatch(typeof(Pawn), "Kill")]
    public static class Patch_PawnKill
    {
        static void Postfix(Pawn __instance, DamageInfo? dinfo)
        {
            if (!RimMindMemoryMod.Settings.enableMemory) return;
            if (!RimMindMemoryMod.Settings.triggerDeath) return;

            try
            {
                var wc = RimMindMemoryWorldComponent.Instance;
                if (wc == null) return;

                if (__instance == null) return;
                DarkMemoryUpdater.CleanupPawnJitter(__instance.thingIDNumber);

                var settings = RimMindMemoryMod.Settings;
                int now = Find.TickManager.TicksGame;
                string deadName = __instance.Name?.ToStringShort ?? __instance.def?.label ?? "RimMind.Memory.Trigger.Unknown".Translate();
                string deadTargetId = !string.IsNullOrEmpty(__instance.ThingID)
                    ? __instance.ThingID
                    : __instance.thingIDNumber.ToString();

                bool hadRelatedColonist = false;
                foreach (var colonist in GetRelatedColonists(__instance))
                {
                    hadRelatedColonist = true;
                    try
                    {
                        string relation = GetRelationLabel(colonist, __instance);
                        string content = relation != null
                            ? "RimMind.Memory.Trigger.RelationDeath".Translate(relation, deadName)
                            : "RimMind.Memory.Trigger.Death".Translate(deadName);

                        float importance = relation != null ? 1.0f : 0.85f;
                        string colonistPawnId = !string.IsNullOrEmpty(colonist.ThingID)
                            ? colonist.ThingID
                            : colonist.thingIDNumber.ToString();

                        wc.AddPawnMemory(
                            colonist,
                            MemoryEntry.Create(content, MemoryType.Event, now, importance, colonistPawnId, deadTargetId),
                            settings.maxActive,
                            settings.maxArchive);
                    }
                    catch { }
                }

                if (!ShouldRecordNarratorDeath(__instance, hadRelatedColonist))
                    return;

                string narratorContent = __instance.IsFreeNonSlaveColonist
                    ? "RimMind.Memory.Trigger.ColonistDeath".Translate(deadName)
                    : "RimMind.Memory.Trigger.Death".Translate(deadName);

                string attackerStr = "";
                string? killerPawnId = null;
                if (dinfo != null && dinfo.HasValue && dinfo.Value.Instigator != null && !dinfo.Value.Instigator.Destroyed)
                {
                    var inst = dinfo.Value.Instigator;
                    if (inst is Pawn killerPawn)
                    {
                        killerPawnId = !string.IsNullOrEmpty(killerPawn.ThingID)
                            ? killerPawn.ThingID
                            : killerPawn.thingIDNumber.ToString();
                    }
                    attackerStr = inst is Pawn p && p.Name != null
                        ? "RimMind.Memory.Trigger.KilledBy".Translate(p.Name.ToStringShort)
                        : (!inst.Label.NullOrEmpty() ? "RimMind.Memory.Trigger.KilledBy".Translate(inst.Label) : "");
                }

                wc.AddNarratorMemory(
                    MemoryEntry.Create(narratorContent + attackerStr, MemoryType.Event, now, 1.0f,
                        deadTargetId, killerPawnId),
                    settings.narratorMaxActive, settings.narratorMaxArchive);
            }
            catch (System.Exception ex)
            {
                RimMindErrors.Warn($"[RimMind-Memory] Patch_PawnKill error: {ex.Message}");
            }
        }

        public static bool ShouldRecordNarratorDeath(
            bool isColonyMember,
            bool isHumanlike,
            bool isNamed,
            bool hasColonistRelation,
            bool isMajorFactionLeader)
        {
            if (isColonyMember) return true;
            if (isHumanlike && isNamed && hasColonistRelation) return true;
            if (isMajorFactionLeader) return true;
            return false;
        }

        private static bool ShouldRecordNarratorDeath(Pawn dead, bool hasColonistRelation)
        {
            bool isColonyMember = dead.IsColonist || dead.IsPrisonerOfColony || dead.IsSlaveOfColony;
            bool isHumanlike = dead.RaceProps?.Humanlike == true;
            bool isNamed = dead.Name != null && !dead.Name.Numerical;
            bool isMajorFactionLeader = dead.Faction != null
                && !dead.Faction.IsPlayer
                && !dead.Faction.temporary
                && !dead.Faction.Hidden
                && dead.Faction.leader == dead;

            return ShouldRecordNarratorDeath(isColonyMember, isHumanlike, isNamed, hasColonistRelation, isMajorFactionLeader);
        }

        private static IEnumerable<Pawn> GetRelatedColonists(Pawn? dead)
        {
            var result = new List<Pawn>();
            if (dead == null) return result;

            if (Find.Maps != null)
            {
                for (int m = 0; m < Find.Maps.Count; m++)
                {
                    var colonists = RimMind.Presentation.Api.RimMindPawnLookup.GetFreeColonistsSafe(Find.Maps[m]);
                    for (int i = 0; i < colonists.Count; i++)
                    {
                        var pawn = colonists[i];
                        if (pawn == null || pawn == dead) continue;
                        if (pawn.relations?.DirectRelations?.Any(r => r.otherPawn == dead) == true)
                            result.Add(pawn);
                    }
                }
            }

            var caravans = Find.WorldObjects?.Caravans;
            if (caravans != null)
            {
                for (int c = 0; c < caravans.Count; c++)
                {
                    var caravan = caravans[c];
                    if (caravan == null || !caravan.IsPlayerControlled) continue;
                    var caravanPawns = caravan.PawnsListForReading;
                    if (caravanPawns == null) continue;
                    for (int i = 0; i < caravanPawns.Count; i++)
                    {
                        var pawn = caravanPawns[i];
                        if (pawn == null || !pawn.IsFreeNonSlaveColonist || pawn.Dead || pawn.Destroyed || pawn.IsKidnapped()) continue;
                        if (pawn == dead) continue;
                        if (pawn.relations?.DirectRelations?.Any(r => r.otherPawn == dead) == true)
                            result.Add(pawn);
                    }
                }
            }

            return result;
        }

        private static string GetRelationLabel(Pawn colonist, Pawn? dead)
        {
            if (colonist == null || dead == null) return null!;
            var rel = colonist.relations?.DirectRelations?.FirstOrDefault(r => r.otherPawn == dead);
            if (rel?.def == null) return null!;
            string raw = rel.def.LabelCap.RawText;
            return raw.NullOrEmpty() ? null! : raw;
        }
    }
}
