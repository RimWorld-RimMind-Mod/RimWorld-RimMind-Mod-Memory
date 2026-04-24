using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimMind.Core;
using RimMind.Core.Client;
using RimMind.Core.Context;
using RimMind.Core.Prompt;
using RimMind.Memory.Data;
using RimMind.Memory.Decay;
using Verse;

namespace RimMind.Memory.DarkMemory
{
    public class DarkMemoryUpdater : GameComponent
    {
        private static DarkMemoryUpdater? _instance;
        public static DarkMemoryUpdater Instance => _instance!;

        private int _narratorOffset;
        private const int DailyInterval = 60000;
        private const int JitterRange = 3000;

        private Dictionary<int, int> _pawnJitter = new Dictionary<int, int>();

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

        public override void GameComponentTick()
        {
            if (!RimMindMemoryMod.Settings.enableMemory) return;
            var wc = RimMindMemoryWorldComponent.Instance;
            if (wc == null) return;

            var settings = RimMindMemoryMod.Settings;

            foreach (var map in Find.Maps)
            {
                foreach (var pawn in map.mapPawns.FreeColonists)
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

            foreach (var pawn in Find.WorldPawns?.AllPawnsAlive ?? Enumerable.Empty<Pawn>())
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
            if (!RimMindAPI.IsConfigured()) return;

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

            var npcId = $"NPC-{pawn.ThingID}";
            var ctxRequest = new ContextRequest
            {
                NpcId = npcId,
                Scenario = ScenarioIds.Personality,
                Budget = 0.4f,
                CurrentQuery = PromptSanitizer.Sanitize(sb.ToString()),
                MaxTokens = 200,
                Temperature = 0.5f,
            };

            var schema = RimMind.Core.Context.SchemaRegistry.DarkMemoryOutput;

            RimMindAPI.RequestStructured(ctxRequest, schema, response =>
            {
                if (!response.Success) return;
                ApplyPawnDarkMemory(response.Content, store, settings.darkCount, now);
            });
        }

        public void TriggerNarratorDarkMemoryUpdate(RimMindMemoryWorldComponent wc, RimMindMemorySettings settings)
        {
            if (!RimMindAPI.IsConfigured()) return;

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
                Scenario = ScenarioIds.Storyteller,
                Budget = 0.4f,
                CurrentQuery = PromptSanitizer.Sanitize(sb.ToString()),
                MaxTokens = 300,
                Temperature = 0.5f,
            };

            var schema = RimMind.Core.Context.SchemaRegistry.DarkMemoryOutput;

            RimMindAPI.RequestStructured(ctxRequest, schema, response =>
            {
                if (!response.Success) return;
                ApplyNarratorDarkMemory(response.Content, store, settings.narratorDarkCount, now);
            });
        }

        private void ApplyPawnDarkMemory(string jsonContent, PawnMemoryStore store, int darkCount, int now)
        {
            try
            {
                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<DarkMemoryResultDto>(jsonContent);
                if (result?.dark == null) return;

                store.dark.Clear();
                int added = 0;
                foreach (var text in result.dark)
                {
                    if (string.IsNullOrEmpty(text)) continue;
                    if (added >= darkCount) break;
                    store.dark.Add(MemoryEntry.Create(text, MemoryType.Dark, now, 1.0f));
                    added++;
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[RimMind-Memory] Failed to parse pawn dark memory response: {ex.Message}");
            }
        }

        private void ApplyNarratorDarkMemory(string jsonContent, NarratorMemoryStore store, int darkCount, int now)
        {
            try
            {
                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<DarkMemoryResultDto>(jsonContent);
                if (result?.dark == null) return;

                store.dark.Clear();
                int added = 0;
                foreach (var text in result.dark)
                {
                    if (string.IsNullOrEmpty(text)) continue;
                    if (added >= darkCount) break;
                    store.dark.Add(MemoryEntry.Create(text, MemoryType.Dark, now, 1.0f));
                    added++;
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[RimMind-Memory] Failed to parse narrator dark memory response: {ex.Message}");
            }
        }

        private class DarkMemoryResultDto
        {
            public string[] dark = Array.Empty<string>();
        }
    }
}
