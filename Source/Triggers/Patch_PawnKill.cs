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

                if (__instance != null)
                    DarkMemoryUpdater.CleanupPawnJitter(__instance.thingIDNumber);

                var settings = RimMindMemoryMod.Settings;
                int now = Find.TickManager.TicksGame;
                string deadName = __instance?.Name?.ToStringShort ?? __instance?.def?.label ?? "RimMind.Memory.Trigger.Unknown".Translate();

                foreach (var colonist in GetRelatedColonists(__instance))
                {
                    try
                    {
                        string relation = GetRelationLabel(colonist, __instance);
                        string content = relation != null
                            ? "RimMind.Memory.Trigger.RelationDeath".Translate(relation, deadName)
                            : "RimMind.Memory.Trigger.Death".Translate(deadName);

                        float importance = relation != null ? 1.0f : 0.85f;

                        wc.AddPawnMemory(colonist, MemoryEntry.Create(content, MemoryType.Event, now, importance),
                            settings.maxActive, settings.maxArchive);
                    }
                    catch { }
                }

                string narratorContent = __instance != null && __instance.IsFreeNonSlaveColonist
                    ? "RimMind.Memory.Trigger.ColonistDeath".Translate(deadName)
                    : "RimMind.Memory.Trigger.Death".Translate(deadName);

                string attackerStr = "";
                if (dinfo != null && dinfo.HasValue && dinfo.Value.Instigator != null && !dinfo.Value.Instigator.Destroyed)
                {
                    var inst = dinfo.Value.Instigator;
                    attackerStr = inst is Pawn p && p.Name != null
                        ? "RimMind.Memory.Trigger.KilledBy".Translate(p.Name.ToStringShort)
                        : (!inst.Label.NullOrEmpty() ? "RimMind.Memory.Trigger.KilledBy".Translate(inst.Label) : "");
                }

                wc.AddNarratorMemory(
                    MemoryEntry.Create(narratorContent + attackerStr, MemoryType.Event, now, 1.0f,
                        __instance != null ? __instance.ThingID.ToString() : null),
                    settings.narratorMaxActive, settings.narratorMaxArchive);
            }
            catch (System.Exception ex)
            {
                Log.Warning($"[RimMind-Memory] Patch_PawnKill error: {ex.Message}");
            }
        }

        private static IEnumerable<Pawn> GetRelatedColonists(Pawn? dead)
        {
            if (dead == null) yield break;

            foreach (var pawn in Find.Maps.SelectMany(m => m.mapPawns.FreeColonists))
            {
                if (pawn == dead) continue;
                if (pawn.relations?.DirectRelations?.Any(r => r.otherPawn == dead) == true)
                    yield return pawn;
            }

            foreach (var pawn in Find.WorldPawns?.AllPawnsAlive ?? Enumerable.Empty<Pawn>())
            {
                if (!pawn.IsFreeNonSlaveColonist) continue;
                if (pawn == dead) continue;
                if (pawn.relations?.DirectRelations?.Any(r => r.otherPawn == dead) == true)
                    yield return pawn;
            }
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
