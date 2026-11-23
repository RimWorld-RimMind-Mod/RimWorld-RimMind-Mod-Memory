using System;
using System.Linq;
using System.Text;
using RimMind.Application.Common.Helpers;
using RimMind.Application.Features.Llm;
using RimMind.Domain.ValueObjects;
using RimMind.Presentation.Api;
using RimMind.Memory.Data;
using RimMind.Memory.Decay;
using RimMind.Memory.Injection;
using RimWorld;
using Verse;

namespace RimMind.Memory.DarkMemory
{
    public class DarkMemoryUpdater : GameComponent
    {
        public const string RequestModId = "RimMind.Memory";
        public const int DailyInterval = 60000;
        public const int DarkMemoryMaxQueueWaitTicks = DailyInterval;
        private const int StaggerStepTicks = 250;
        private const int DailyStaggerSlots = DailyInterval / StaggerStepTicks;

        private static DarkMemoryUpdater? _instance;
        public static DarkMemoryUpdater? Instance => Current.Game != null ? _instance : null;

        private int _narratorOffset;

        public DarkMemoryUpdater(Game game)
        {
            _instance = this;
            _narratorOffset = new System.Random().Next(0, DailyStaggerSlots);
        }

        public static string BuildCoalesceKey(string npcId)
            => $"{RequestModId}:dark_memory:{npcId}";

        public override void GameComponentTick()
        {
            if (!RimMindMemoryMod.Settings.enableMemory) return;
            var wc = RimMindMemoryWorldComponent.Instance;
            if (wc == null) return;

            int now = Find.TickManager.TicksGame;
            if (now <= 0 || (now % StaggerStepTicks) != 0) return;

            int slot = now / StaggerStepTicks;
            var settings = RimMindMemoryMod.Settings;

            if (Find.Maps != null)
            {
                for (int m = 0; m < Find.Maps.Count; m++)
                {
                    var colonists = RimMindPawnLookup.GetSpawnedColonistsSafe(Find.Maps[m]);
                    for (int i = 0; i < colonists.Count; i++)
                    {
                        var pawn = colonists[i];
                        if (pawn == null || pawn.Dead || pawn.Destroyed || pawn.IsKidnapped()) continue;
                        if (!TickStagger.IsDue(slot, pawn.thingIDNumber, DailyStaggerSlots)) continue;
                        TriggerPawnDarkMemoryUpdate(pawn, wc, settings);

                        if (settings.enableDecay)
                        {
                            var store = wc.GetPawnStore(pawn);
                            if (store != null && ImportanceDecayManager.ApplyDecay(store, settings.decayRate, settings.minImportanceThreshold) > 0)
                                MemoryChangedEvent.Publish(pawn.thingIDNumber);
                        }
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
                        if (!TickStagger.IsDue(slot, pawn.thingIDNumber, DailyStaggerSlots)) continue;
                        TriggerPawnDarkMemoryUpdate(pawn, wc, settings);

                        if (settings.enableDecay)
                        {
                            var store = wc.GetPawnStore(pawn);
                            if (store != null && ImportanceDecayManager.ApplyDecay(store, settings.decayRate, settings.minImportanceThreshold) > 0)
                                MemoryChangedEvent.Publish(pawn.thingIDNumber);
                        }
                    }
                }
            }

            if (TickStagger.IsDue(slot, _narratorOffset, DailyStaggerSlots))
            {
                TriggerNarratorDarkMemoryUpdate(wc, settings);

                if (settings.enableDecay
                    && ImportanceDecayManager.ApplyDecay(wc.NarratorStore, settings.decayRate, settings.minImportanceThreshold) > 0)
                {
                    MemoryChangedEvent.Publish(0);
                }
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
            bool isCaravanMember = !pawn.Spawned && RimWorld.Planet.CaravanUtility.IsCaravanMember(pawn);
            if (!pawn.Spawned && !isCaravanMember) return;
            if (!RimMindAPI.Settings.IsConfigured()) return;

            var store = wc.GetPawnStore(pawn);
            if (store == null) return;
            int now = Find.TickManager.TicksGame;
            int windowStart = Math.Max(0, now - DailyInterval);
            var (activeList, _, darkList) = store.GetSnapshots();
            var todayEntries = activeList.Where(e => e.tick >= windowStart && e.type != MemoryType.Dark).ToList();

            if (todayEntries.Count == 0) return;

            var sb = new StringBuilder();
            sb.AppendLine(RimMindAPI.Text.TrOr(
                "RimMind.Memory.Prompt.PawnTodayMemory",
                "{0}'s memories today:",
                pawn.Name?.ToStringShort ?? pawn.LabelShort));
            foreach (var e in todayEntries)
                sb.AppendLine($"- {e.content}");
            sb.AppendLine();
            sb.AppendLine(RimMindAPI.Text.TrOr(
                "RimMind.Memory.Prompt.ExistingImpression",
                "Existing long-term impressions (each <=50 chars, evolvable):"));
            foreach (var d in darkList)
                sb.AppendLine($"- {d.content}");
            sb.AppendLine();
            sb.AppendLine(RimMindAPI.Text.TrOr(
                "RimMind.Memory.Prompt.MergeInstruction",
                "Merge today's memories into existing impressions, output JSON (keep {0} entries, each <=50 chars):",
                settings.darkCount));
            sb.AppendLine(RimMindAPI.Text.TrOr(
                "RimMind.Memory.Prompt.JsonTemplate",
                "{{\"dark\":[\"...\",\"...\"]}}"));

            var npcId = $"NPC-{pawn.thingIDNumber}";
            var currentQuery = RimMindAPI.Prompt.Sanitize(sb.ToString());
            var schema = RimMindAPI.Context.FindSchema(DarkMemoryResultParserPure.SchemaKey)
                ?? DarkMemoryResultParserPure.DarkMemoryOutputSchema;

            var envelope = LlmRequestEnvelopeBuilder
                .ForScenario(RimMindAPI.Context.ScenarioMemory)
                .WithModId(RequestModId)
                .WithSchema(schema)
                .WithMaxTokens(400)
                .WithTemperature(0.5f)
                .WithNpcId(npcId)
                .WithGameStateInfo(currentQuery)
                .WithCoalesceKey(BuildCoalesceKey(npcId))
                .WithMaxQueueWaitTicks(DarkMemoryMaxQueueWaitTicks)
                .WithRequireAliveColonist(!isCaravanMember)
                .Build();
            RimMindAPI.Request.Send(envelope, result =>
            {
                if (result.IsErr) return;
                if (!ReferenceEquals(wc, RimMindMemoryWorldComponent.Instance)) return;
                if (pawn.Dead || pawn.Destroyed || pawn.IsKidnapped() || pawn.Faction != Faction.OfPlayer) return;
                if (!pawn.Spawned && !RimWorld.Planet.CaravanUtility.IsCaravanMember(pawn)) return;
                if (ApplyDarkMemory(result.Value.Content, settings.darkCount, now, store))
                {
                    MemoryChangedEvent.Publish(pawn.thingIDNumber);
                }
            });
        }

        public void TriggerNarratorDarkMemoryUpdate(RimMindMemoryWorldComponent wc, RimMindMemorySettings settings)
        {
            if (!RimMindAPI.Settings.IsConfigured()) return;

            var store = wc.NarratorStore;
            int now = Find.TickManager.TicksGame;
            int windowStart = Math.Max(0, now - DailyInterval);
            var (activeList, _, darkList) = store.GetSnapshots();
            var todayEntries = activeList.Where(e => e.tick >= windowStart && e.type != MemoryType.Dark).ToList();

            if (todayEntries.Count == 0) return;

            var sb = new StringBuilder();
            sb.AppendLine(RimMindAPI.Text.TrOr(
                "RimMind.Memory.Prompt.ColonyTodayEvents",
                "Colony events today:"));
            foreach (var e in todayEntries)
                sb.AppendLine($"- {e.content}");
            sb.AppendLine();
            sb.AppendLine(RimMindAPI.Text.TrOr(
                "RimMind.Memory.Prompt.ExistingNarrative",
                "Existing long-term narratives (each <=50 chars, evolvable):"));
            foreach (var d in darkList)
                sb.AppendLine($"- {d.content}");
            sb.AppendLine();
            sb.AppendLine(RimMindAPI.Text.TrOr(
                "RimMind.Memory.Prompt.MergeNarrativeInstruction",
                "Merge today's events into existing narratives, output JSON (keep {0} entries, each <=50 chars):",
                settings.narratorDarkCount));
            sb.AppendLine(RimMindAPI.Text.TrOr(
                "RimMind.Memory.Prompt.JsonTemplate",
                "{{\"dark\":[\"...\",\"...\"]}}"));

            var npcId = "NPC-storyteller";
            var currentQuery = RimMindAPI.Prompt.Sanitize(sb.ToString());
            var schema = RimMindAPI.Context.FindSchema(DarkMemoryResultParserPure.SchemaKey)
                ?? DarkMemoryResultParserPure.DarkMemoryOutputSchema;

            var envelope = LlmRequestEnvelopeBuilder
                .ForScenario(RimMindAPI.Context.ScenarioMemory)
                .WithModId(RequestModId)
                .WithSchema(schema)
                .WithMaxTokens(400)
                .WithTemperature(0.5f)
                .WithNpcId(npcId)
                .WithGameStateInfo(currentQuery)
                .WithCoalesceKey(BuildCoalesceKey(npcId))
                .WithMaxQueueWaitTicks(DarkMemoryMaxQueueWaitTicks)
                .WithRequireAliveColonist(false)
                .Build();
            RimMindAPI.Request.Send(envelope, result =>
            {
                if (result.IsErr) return;
                if (!ReferenceEquals(wc, RimMindMemoryWorldComponent.Instance)) return;
                if (ApplyDarkMemory(result.Value.Content, settings.narratorDarkCount, now, store))
                {
                    MemoryChangedEvent.Publish(0);
                }
            });
        }

        private static bool ApplyDarkMemory(string json, int darkCount, int now, MemoryStoreBase store)
        {
            try
            {
                var entries = DarkMemoryResultParserPure.Parse(json, darkCount);
                if (entries == null || entries.Count == 0) return false;

                lock (store.SyncRoot)
                {
                    store.dark.Clear();
                    foreach (var text in entries)
                    {
                        store.dark.Add(MemoryEntry.Create(text, MemoryType.Dark, now, 1.0f));
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                RimMindErrors.Warn($"[RimMind-Memory] Failed to parse dark memory response: {ex.Message}");
                return false;
            }
        }
    }
}
