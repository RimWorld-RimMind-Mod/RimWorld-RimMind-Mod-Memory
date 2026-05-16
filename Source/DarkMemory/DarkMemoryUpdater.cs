using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Models.Context;
using RimMind.Domain.ValueObjects;
using RimMind.Presentation;
using RimMind.Presentation.Context;
using RimMind.Infrastructure.Services.Clients;
using RimMind.Application.Features.Json;
using RimMind.Application.Features.Context;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Features.Prompt;
using RimMind.Memory.Data;
using RimMind.Memory.Decay;
using Verse;

namespace RimMind.Memory.DarkMemory
{
    public class DarkMemoryUpdater : GameComponent
    {
        private static DarkMemoryUpdater? _instance;
        public static DarkMemoryUpdater Instance => _instance ?? throw new InvalidOperationException("DarkMemoryUpdater not initialized");

        private int _narratorOffset;
        private const int DailyInterval = 60000;
        private const int JitterRange = 3000;

        private Dictionary<int, int> _pawnJitter = new Dictionary<int, int>();
        private int _lastJitterCleanupTick;
        private const int JitterCleanupInterval = 300000;

        public DarkMemoryUpdater(Game game)
        {
            _instance = this;
            _narratorOffset = new System.Random().Next(0, DailyInterval);
        }

        private int GetPawnJitter(int thingID)
        {
            if (!_pawnJitter.TryGetValue(thingID, out int jitter))
            {
                jitter = new System.Random(thingID ^ 0x5A5A5A5A).Next(-JitterRange, JitterRange + 1);
                _pawnJitter[thingID] = jitter;
            }
            return jitter;
        }

        public static void CleanupPawnJitter(int thingID)
        {
            _instance?._pawnJitter.Remove(thingID);
        }

        private void PruneDestroyedPawnJitter()
        {
            int now = Find.TickManager.TicksGame;
            if (now - _lastJitterCleanupTick < JitterCleanupInterval) return;
            _lastJitterCleanupTick = now;

            var aliveIds = new HashSet<int>();
            foreach (var map in Find.Maps)
                foreach (var pawn in map.mapPawns.FreeColonists)
                    aliveIds.Add(pawn.thingIDNumber);
            foreach (var pawn in Find.WorldPawns?.AllPawnsAlive ?? Enumerable.Empty<Pawn>())
                if (pawn.IsFreeNonSlaveColonist)
                    aliveIds.Add(pawn.thingIDNumber);

            var keysToRemove = _pawnJitter.Keys.Where(kv => !aliveIds.Contains(kv)).ToList();
            foreach (var key in keysToRemove) _pawnJitter.Remove(key);
        }

        public override void GameComponentTick()
        {
            if (!RimMindMemoryMod.Settings.enableMemory) return;
            PruneDestroyedPawnJitter();
            var wc = RimMindMemoryWorldComponent.Instance;
            if (wc == null) return;

            var settings = RimMindMemoryMod.Settings;

            foreach (var map in Find.Maps)
            {
                foreach (var pawn in map.mapPawns.FreeColonists.ToList())
                {
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

            foreach (var pawn in (Find.WorldPawns?.AllPawnsAlive ?? Enumerable.Empty<Pawn>()).ToList())
            {
                if (!pawn.IsFreeNonSlaveColonist) continue;
                int jitteredInterval = DailyInterval + GetPawnJitter(pawn.thingIDNumber);
                if (!pawn.IsHashIntervalTick(jitteredInterval)) continue;
                TriggerPawnDarkMemoryUpdate(pawn, wc, settings);

                if (settings.enableDecay)
                {
                    var store = wc.GetOrCreatePawnStore(pawn);
                    ImportanceDecayManager.ApplyDecay(store, settings.decayRate, settings.minImportanceThreshold);
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
            Scribe_Collections.Look(ref _pawnJitter, "pawnJitter", LookMode.Value, LookMode.Value);
            _pawnJitter ??= new Dictionary<int, int>();
        }

        public void TriggerPawnDarkMemoryUpdate(Pawn pawn, RimMindMemoryWorldComponent wc, RimMindMemorySettings settings)
        {
            if (!RimMind.Presentation.RimMindAPI.IsConfigured()) return;

            var store = wc.GetOrCreatePawnStore(pawn);
            int now = Find.TickManager.TicksGame;
            int todayStart = now - (now % DailyInterval);
            var todayEntries = store.active.Where(e => e.tick >= todayStart && e.type != MemoryType.Dark).ToList();

            if (todayEntries.Count == 0 && store.dark.Count == 0) return;

            var sb = new StringBuilder();
            sb.AppendLine("RimMind.Memory.Prompt.PawnTodayMemory".Translate(pawn.Name.ToStringShort));
            foreach (var e in todayEntries)
                sb.AppendLine($"- {e.content}");
            sb.AppendLine();
            sb.AppendLine("RimMind.Memory.Prompt.ExistingImpression".Translate());
            foreach (var d in store.dark)
                sb.AppendLine($"- {d.content}");
            sb.AppendLine();
            sb.AppendLine("RimMind.Memory.Prompt.MergeInstruction".Translate(settings.darkCount));
            sb.AppendLine("RimMind.Memory.Prompt.JsonTemplate".Translate());

            var npcId = $"NPC-{pawn.thingIDNumber}";
            var ctxRequest = new ContextRequest
            {
                NpcId = npcId,
                Scenario = ScenarioIds.Memory,
                Budget = 0.4f,
                CurrentQuery = PromptSanitizer.Sanitize(sb.ToString()),
                MaxTokens = 400,
                Temperature = 0.5f,
            };

            var schema = RimMind.Application.Features.Context.SchemaRegistry.DarkMemoryOutput;

            RimMind.Presentation.RimMindAPI.RequestStructured(ctxRequest, schema, result =>
            {
                if (result.IsErr) return;
                ApplyPawnDarkMemory(result.Value.Content, store, settings.darkCount, now);
            });
        }

        public void TriggerNarratorDarkMemoryUpdate(RimMindMemoryWorldComponent wc, RimMindMemorySettings settings)
        {
            if (!RimMind.Presentation.RimMindAPI.IsConfigured()) return;

            var store = wc.NarratorStore;
            int now = Find.TickManager.TicksGame;
            int todayStart = now - (now % DailyInterval);
            var todayEntries = store.active.Where(e => e.tick >= todayStart && e.type != MemoryType.Dark).ToList();

            if (todayEntries.Count == 0 && store.dark.Count == 0) return;

            var sb = new StringBuilder();
            sb.AppendLine("RimMind.Memory.Prompt.ColonyTodayEvents".Translate());
            foreach (var e in todayEntries)
                sb.AppendLine($"- {e.content}");
            sb.AppendLine();
            sb.AppendLine("RimMind.Memory.Prompt.ExistingNarrative".Translate());
            foreach (var d in store.dark)
                sb.AppendLine($"- {d.content}");
            sb.AppendLine();
            sb.AppendLine("RimMind.Memory.Prompt.MergeNarrativeInstruction".Translate(settings.narratorDarkCount));
            sb.AppendLine("RimMind.Memory.Prompt.JsonTemplate".Translate());

            var ctxRequest = new ContextRequest
            {
                NpcId = "NPC-storyteller",
                Scenario = ScenarioIds.Memory,
                Budget = 0.4f,
                CurrentQuery = PromptSanitizer.Sanitize(sb.ToString()),
                MaxTokens = 400,
                Temperature = 0.5f,
                Map = Find.Maps.FirstOrDefault(),
            };

            var schema = RimMind.Application.Features.Context.SchemaRegistry.DarkMemoryOutput;

            RimMind.Presentation.RimMindAPI.RequestStructured(ctxRequest, schema, result =>
            {
                if (result.IsErr) return;
                ApplyNarratorDarkMemory(result.Value.Content, store, settings.narratorDarkCount, now);
            });
        }

        private void ApplyPawnDarkMemory(string jsonContent, PawnMemoryStore store, int darkCount, int now)
        {
            ApplyDarkMemory(jsonContent, darkCount, now, store.dark);
        }

        private void ApplyNarratorDarkMemory(string jsonContent, NarratorMemoryStore store, int darkCount, int now)
        {
            ApplyDarkMemory(jsonContent, darkCount, now, store.dark);
        }

        private static void ApplyDarkMemory(string json, int darkCount, int now, List<MemoryEntry> darkStore)
        {
            try
            {
                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<DarkMemoryResultDto>(json);
                if (result?.dark == null)
                {
                    string? repaired = JsonRepairHelper.TryRepairTruncatedJson(json);
                    if (repaired != null)
                        result = Newtonsoft.Json.JsonConvert.DeserializeObject<DarkMemoryResultDto>(repaired);
                }
                if (result?.dark == null) return;

                darkStore.Clear();
                int added = 0;
                foreach (var text in result.dark)
                {
                    if (string.IsNullOrEmpty(text)) continue;
                    if (added >= darkCount) break;
                    darkStore.Add(MemoryEntry.Create(text, MemoryType.Dark, now, 1.0f));
                    added++;
                }
            }
            catch (Exception ex)
            {
                RimMindErrors.Warn($"[RimMind-Memory] Failed to parse dark memory response: {ex.Message}");
            }
        }

        private class DarkMemoryResultDto
        {
            public string[] dark = Array.Empty<string>();
        }
    }
}
