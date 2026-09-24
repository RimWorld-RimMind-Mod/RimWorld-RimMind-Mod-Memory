using System;
using System.Linq;
using LudeonTK;
using RimMind.Memory.Core;
using RimMind.Memory.Data;
using RimMind.Presentation.Api;
using RimWorld;
using Verse;

namespace RimMind.Memory.Debug
{
    /// <summary>
    /// In-game behavioral autotest suite for RimMind-Memory.
    /// Discovered automatically by Core's BehaviorAutotestRunner and exposed to Dev menu.
    /// </summary>
    public sealed class MemoryBehaviorAutotestSuite : BehaviorAutotestSuiteBase
    {
        public override string ModId => "Memory";
        public override string SuiteId => "Behavior.MemoryThreeTier";

        [DebugAction("Autotests", "Run Memory In-Game Behavior Test", actionType = DebugActionType.Action)]
        public static void RunFromDevMenu() => RunSuiteFromDevMenu<MemoryBehaviorAutotestSuite>();

        public override void RunSuite(IInGameBehaviorSuiteContext context)
        {
            // 1. RimMindMemoryWorldComponent Instance
            var comp = RimMindMemoryWorldComponent.Instance;
            context.Assert(comp != null, "RimMindMemoryWorldComponent instance is available in World");
            if (comp == null) return;

            Pawn? pawn = context.ActiveColonist;

            // 2. PawnMemoryStore Operations & Zero-Pollution Cleanup
            if (pawn != null)
            {
                try
                {
                    var store = comp.GetOrCreatePawnStore(pawn);
                    context.Assert(store != null, $"PawnMemoryStore retrieved for {pawn.Name.ToStringShort}");

                    if (store != null)
                    {
                        var testEntry = MemoryEntry.Create("Completed a test in laboratory", MemoryType.Event, Find.TickManager.TicksGame, 0.75f, pawn.thingIDNumber.ToString());
                        testEntry.id = "autotest_mem_001";
                        int beforeCount = store.active.Count;

                        store.AddActive(testEntry, 50, 100);
                        context.Assert(store.active.Any(m => m.id == "autotest_mem_001"), "Active memory entry added successfully to PawnMemoryStore");

                        // Self-cleanup
                        store.active.RemoveAll(m => m.id == "autotest_mem_001");
                        context.Assert(store.active.Count == beforeCount, "Test memory entry cleaned up without state pollution");
                    }
                }
                catch (Exception ex)
                {
                    context.Assert(false, $"PawnMemoryStore operations threw exception: {ex.Message}");
                }
            }
            else
            {
                context.Warn("No colonist available on current map; skipped colonist memory store test.");
            }

            // 3. MemoryRetrievalScorer Ranking & Scoring
            int now = Find.TickManager.TicksGame;
            var pinnedOld = MemoryEntry.Create("A pinned core memory", MemoryType.Dark, 100, 0.1f);
            pinnedOld.id = "pinned_old";
            pinnedOld.isPinned = true;

            var unpinnedRecent = MemoryEntry.Create("A recent unpinned memory", MemoryType.Event, now, 0.9f);
            unpinnedRecent.id = "unpinned_recent";
            unpinnedRecent.isPinned = false;

            float scorePinned = MemoryRetrievalScorer.CalculateScore(pinnedOld, now);
            float scoreUnpinned = MemoryRetrievalScorer.CalculateScore(unpinnedRecent, now);

            context.Assert(scorePinned > scoreUnpinned, $"Pinned memory receives higher retrieval score than unpinned ({scorePinned:F2} > {scoreUnpinned:F2})");

            var ranked = MemoryRetrievalScorer.SelectTopScored(new[] { pinnedOld, unpinnedRecent }, 1, now);
            context.Assert(ranked.Count == 1 && ranked[0].id == "pinned_old", "SelectTopScored successfully filters top priority memory");

            // 4. ImportanceDecayCalculator
            float initialImportance = 0.8f;
            float decayed = ImportanceDecayCalculator.Decay(initialImportance, 0.1f);
            context.Assert(decayed < initialImportance && decayed > 0f, $"ImportanceDecayCalculator correctly decays importance ({decayed:F2} < {initialImportance:F2})");

            // 5. Target-Aware Memory Retrieval Boost
            var generalMemory = MemoryEntry.Create("Went hunting in the forest", MemoryType.Event, now - 1000, 0.8f);
            var partnerMemory = MemoryEntry.Create("Had a great conversation with Bob about engineering", MemoryType.Event, now - 1000, 0.8f, targetPawnId: "102");
            partnerMemory.id = "partner_bob";

            float generalScore = MemoryRetrievalScorer.CalculateScore(generalMemory, now, targetPawnId: "102", targetName: "Bob");
            float boostedScore = MemoryRetrievalScorer.CalculateScore(partnerMemory, now, targetPawnId: "102", targetName: "Bob");

            context.Assert(boostedScore > generalScore, $"Target-aware memory receives relevance boost ({boostedScore:F2} > {generalScore:F2})");
        }
    }
}
