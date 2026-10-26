using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Features.Llm;
using RimMind.Domain.Llm;
using RimMind.Domain.ValueObjects;
using RimMind.Presentation.Api;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Memory.Data;
using RimMind.Memory.Decay;
using RimWorld;
using Verse;

namespace RimMind.Memory.DarkMemory
{
    public class DarkMemoryUpdater : GameComponent
    {
        private static DarkMemoryUpdater? _instance;
        public static DarkMemoryUpdater? Instance => Current.Game != null ? _instance : null;

        private int _narratorOffset;
        private const int DailyInterval = 60000;
        private const int JitterRange = 3000;

        public DarkMemoryUpdater(Game game)
        {
            _instance = this;
            _narratorOffset = new System.Random().Next(0, DailyInterval);
        }

        public static int GetPawnJitter(int thingID)
        {
            uint hash = unchecked((uint)(thingID ^ 0x5A5A5A5A));
            hash ^= hash >> 16;
            hash *= 0x7feb352du;
            hash ^= hash >> 15;
            hash *= 0x846ca68bu;
            hash ^= hash >> 16;
            int span = JitterRange * 2 + 1;
            return (int)(hash % (uint)span) - JitterRange;
        }

        public static void CleanupPawnJitter(int thingID)
        {
            // Jitter is computed deterministically from thingIDNumber; no per-pawn state to prune.
        }

        public override void GameComponentTick()
        {
            if (!RimMindMemoryMod.Settings.enableMemory) return;
            var wc = RimMindMemoryWorldComponent.Instance;
            if (wc == null) return;

            var settings = RimMindMemoryMod.Settings;

            foreach (var map in Find.Maps)
            {
                var colonists = RimMind.Presentation.Api.RimMindPawnLookup.GetSpawnedColonistsSafe(map);
                if (colonists.Count == 0) continue;
                for (int i = 0; i < colonists.Count; i++)
                {
                    var pawn = colonists[i];
                    if (pawn == null || pawn.Dead || pawn.Destroyed || pawn.IsKidnapped()) continue;
                    int jitteredInterval = DailyInterval + GetPawnJitter(pawn.thingIDNumber);
                    if (!pawn.IsHashIntervalTick(jitteredInterval)) continue;
                    TriggerPawnDarkMemoryUpdate(pawn, wc, settings);

                    if (settings.enableDecay)
                    {
                        var store = wc.GetOrCreatePawnStore(pawn);
                        ImportanceDecayManager.ApplyDecay(store, settings.decayRate, settings.minImportanceThreshold);
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
                    for (int i = 0; i < caravanPawns.Count; i++)
                    {
                        var pawn = caravanPawns[i];
                        if (pawn == null || !pawn.IsFreeNonSlaveColonist || pawn.Faction != Faction.OfPlayer || pawn.Dead || pawn.Destroyed || pawn.IsKidnapped()) continue;
                        int jitteredInterval = DailyInterval + GetPawnJitter(pawn.thingIDNumber);
                        if (!pawn.IsHashIntervalTick(jitteredInterval)) continue;
                        TriggerPawnDarkMemoryUpdate(pawn, wc, settings);

                        if (settings.enableDecay)
                        {
                            var store = wc.GetOrCreatePawnStore(pawn);
                            ImportanceDecayManager.ApplyDecay(store, settings.decayRate, settings.minImportanceThreshold);
                        }
                    }
                }
            }

            if ((Find.TickManager.TicksGame + _narratorOffset) % DailyInterval == 0)
            {
                TriggerNarratorDarkMemoryUpdate(wc, settings);

                if (settings.enableDecay)
                    ImportanceDecayManager.ApplyDecay(wc.NarratorStore, settings.decayRate, settings.minImportanceThreshold);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref _narratorOffset, "narratorOffset", 0);
        }

        public void TriggerPawnDarkMemoryUpdate(Pawn pawn, RimMindMemoryWorldComponent wc, RimMindMemorySettings settings)
        {
            if (pawn == null || pawn.Dead || pawn.Destroyed || pawn.IsKidnapped() || pawn.Faction != Faction.OfPlayer) return;
            if (!pawn.Spawned && !RimWorld.Planet.CaravanUtility.IsCaravanMember(pawn)) return;
            if (!RimMind.Presentation.Api.RimMindAPI.IsConfigured()) return;

            var store = wc.GetOrCreatePawnStore(pawn);
            int now = Find.TickManager.TicksGame;
            int todayStart = now - (now % DailyInterval);
            var (activeList, _, darkList) = store.GetSnapshots();
            var todayEntries = activeList.Where(e => e.tick >= todayStart && e.type != MemoryType.Dark).ToList();

            if (todayEntries.Count == 0) return;

            var sb = new StringBuilder();
            sb.AppendLine("RimMind.Memory.Prompt.PawnTodayMemory".Translate(pawn.Name?.ToStringShort ?? pawn.LabelShort));
            foreach (var e in todayEntries)
                sb.AppendLine($"- {e.content}");
            sb.AppendLine();
            sb.AppendLine("RimMind.Memory.Prompt.ExistingImpression".Translate());
            foreach (var d in darkList)
                sb.AppendLine($"- {d.content}");
            sb.AppendLine();
            sb.AppendLine("RimMind.Memory.Prompt.MergeInstruction".Translate(settings.darkCount));
            sb.AppendLine("RimMind.Memory.Prompt.JsonTemplate".Translate());

            var npcId = $"NPC-{pawn.thingIDNumber}";
            var currentQuery = RimMindAPI.Prompt.Sanitize(sb.ToString());

            var schema = RimMindAPI.Context.SchemaDarkMemoryOutput;

            var envelope = LlmRequestEnvelopeBuilder
                .ForScenario(RimMindAPI.Context.ScenarioMemory)
                .WithModId("RimMind.Memory")
                .WithSchema(schema)
                .WithMaxTokens(400)
                .WithTemperature(0.5f)
                .WithNpcId(npcId)
                .WithGameStateInfo(currentQuery)
                .Build();
            RimMindAPI.Request.Send(envelope, result =>
            {
                if (result.IsErr) return;
                ApplyPawnDarkMemory(result.Value.Content, store, settings.darkCount, now);
            });
        }

        public void TriggerNarratorDarkMemoryUpdate(RimMindMemoryWorldComponent wc, RimMindMemorySettings settings)
        {
            if (!RimMind.Presentation.Api.RimMindAPI.IsConfigured()) return;

            var store = wc.NarratorStore;
            int now = Find.TickManager.TicksGame;
            int todayStart = now - (now % DailyInterval);
            var (activeList, _, darkList) = store.GetSnapshots();
            var todayEntries = activeList.Where(e => e.tick >= todayStart && e.type != MemoryType.Dark).ToList();

            if (todayEntries.Count == 0) return;

            var sb = new StringBuilder();
            sb.AppendLine("RimMind.Memory.Prompt.ColonyTodayEvents".Translate());
            foreach (var e in todayEntries)
                sb.AppendLine($"- {e.content}");
            sb.AppendLine();
            sb.AppendLine("RimMind.Memory.Prompt.ExistingNarrative".Translate());
            foreach (var d in darkList)
                sb.AppendLine($"- {d.content}");
            sb.AppendLine();
            sb.AppendLine("RimMind.Memory.Prompt.MergeNarrativeInstruction".Translate(settings.narratorDarkCount));
            sb.AppendLine("RimMind.Memory.Prompt.JsonTemplate".Translate());

            var npcId = "NPC-storyteller";
            var currentQuery = RimMindAPI.Prompt.Sanitize(sb.ToString());

            var schema = RimMindAPI.Context.SchemaDarkMemoryOutput;

            var envelope = LlmRequestEnvelopeBuilder
                .ForScenario(RimMindAPI.Context.ScenarioMemory)
                .WithModId("RimMind.Memory")
                .WithSchema(schema)
                .WithMaxTokens(400)
                .WithTemperature(0.5f)
                .WithNpcId(npcId)
                .WithGameStateInfo(currentQuery)
                .Build();
            RimMindAPI.Request.Send(envelope, result =>
            {
                if (result.IsErr) return;
                ApplyNarratorDarkMemory(result.Value.Content, store, settings.narratorDarkCount, now);
            });
        }

        private void ApplyPawnDarkMemory(string jsonContent, PawnMemoryStore store, int darkCount, int now)
        {
            ApplyDarkMemory(jsonContent, darkCount, now, store);
        }

        private void ApplyNarratorDarkMemory(string jsonContent, NarratorMemoryStore store, int darkCount, int now)
        {
            ApplyDarkMemory(jsonContent, darkCount, now, store);
        }

        private static void ApplyDarkMemory(string json, int darkCount, int now, MemoryStoreBase store)
        {
            try
            {
                var entries = DarkMemoryResultParserPure.Parse(json, darkCount);
                if (entries == null || entries.Count == 0) return;

                lock (store.SyncRoot)
                {
                    store.dark.Clear();
                    foreach (var text in entries)
                    {
                        store.dark.Add(MemoryEntry.Create(text, MemoryType.Dark, now, 1.0f));
                    }
                }
            }
            catch (Exception ex)
            {
                RimMindErrors.Warn($"[RimMind-Memory] Failed to parse dark memory response: {ex.Message}");
            }
        }
    }
}
