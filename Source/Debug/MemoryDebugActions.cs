using System.Text;
using LudeonTK;
using RimMind.Core;
using RimMind.Memory.Data;
using RimMind.Memory.DarkMemory;
using RimMind.Memory.Decay;
using RimWorld;
using Verse;

namespace RimMind.Memory.Debug
{
    [StaticConstructorOnStartup]
    public static class MemoryDebugActions
    {
        [DebugAction("RimMind Memory", "Force Add Memory (selected)", actionType = DebugActionType.Action)]
        public static void ForceAddMemory()
        {
            var pawn = Find.Selector.SingleSelectedThing as Pawn;
            if (pawn == null) { Log.Warning("[RimMind-Memory] Select a pawn first."); return; }

            var wc = RimMindMemoryWorldComponent.Instance;
            if (wc == null) { Log.Warning("[RimMind-Memory] WorldComponent not initialized."); return; }

            var settings = RimMindMemoryMod.Settings;
            int now = Find.TickManager.TicksGame;
            wc.AddPawnMemory(pawn,
                MemoryEntry.Create("[Debug] Test memory entry", MemoryType.Manual, now, 0.6f),
                settings.maxActive, settings.maxArchive);

            Log.Message($"[RimMind-Memory] Added test memory for {pawn.Name.ToStringShort}.");
        }

        [DebugAction("RimMind Memory", "Show Memory State (selected)", actionType = DebugActionType.Action)]
        public static void ShowMemoryState()
        {
            var pawn = Find.Selector.SingleSelectedThing as Pawn;
            if (pawn == null) { Log.Warning("[RimMind-Memory] Select a pawn first."); return; }

            var wc = RimMindMemoryWorldComponent.Instance;
            if (wc == null) { Log.Warning("[RimMind-Memory] WorldComponent not initialized."); return; }

            var store = wc.GetOrCreatePawnStore(pawn);
            var sb = new StringBuilder();
            sb.AppendLine($"=== {pawn.Name.ToStringShort} Memory State ===");
            sb.AppendLine($"[Diag] Memory system enabled: {RimMindMemoryMod.Settings.enableMemory}");
            sb.AppendLine($"[Diag] API configured: {RimMindAPI.IsConfigured()}");

            sb.AppendLine($"\n[Active Memories] {store.active.Count} entries");
            foreach (var e in store.active)
                sb.AppendLine($"  {e.content} (imp={e.importance:F2}, pin={e.isPinned})");

            sb.AppendLine($"\n[Archived Memories] {store.archive.Count} entries");
            foreach (var e in store.archive)
                sb.AppendLine($"  {e.content} (imp={e.importance:F2})");

            sb.AppendLine($"\n[Dark Memories] {store.dark.Count} entries");
            foreach (var d in store.dark)
                sb.AppendLine($"  {d.content}");

            Log.Message(sb.ToString());
        }

        [DebugAction("RimMind Memory", "Clear Pawn Memory (selected)", actionType = DebugActionType.Action)]
        public static void ClearPawnMemory()
        {
            var pawn = Find.Selector.SingleSelectedThing as Pawn;
            if (pawn == null) { Log.Warning("[RimMind-Memory] Select a pawn first."); return; }

            var wc = RimMindMemoryWorldComponent.Instance;
            if (wc == null) return;

            wc.ClearPawnStore(pawn);
            Log.Message($"[RimMind-Memory] Cleared all memories for {pawn.Name.ToStringShort}.");
        }

        [DebugAction("RimMind Memory", "Force Add Narrator Memory", actionType = DebugActionType.Action)]
        public static void ForceAddNarratorMemory()
        {
            var wc = RimMindMemoryWorldComponent.Instance;
            if (wc == null) { Log.Warning("[RimMind-Memory] WorldComponent not initialized."); return; }

            var settings = RimMindMemoryMod.Settings;
            int now = Find.TickManager.TicksGame;
            wc.AddNarratorMemory(
                MemoryEntry.Create("[Debug] Test narrator entry", MemoryType.Event, now, 0.7f),
                settings.narratorMaxActive, settings.narratorMaxArchive);

            Log.Message($"[RimMind-Memory] Added test narrator memory. Active count: {wc.NarratorStore.active.Count}");
        }

        [DebugAction("RimMind Memory", "Show Narrator Memory", actionType = DebugActionType.Action)]
        public static void ShowNarratorMemory()
        {
            var wc = RimMindMemoryWorldComponent.Instance;
            if (wc == null) { Log.Warning("[RimMind-Memory] WorldComponent not initialized."); return; }

            var store = wc.NarratorStore;
            var sb = new StringBuilder("=== Narrator Memory ===\n");

            sb.AppendLine($"[Active Narratives] {store.active.Count} entries");
            foreach (var e in store.active)
                sb.AppendLine($"  {e.content} (imp={e.importance:F2})");

            sb.AppendLine($"\n[Archived Narratives] {store.archive.Count} entries");
            foreach (var e in store.archive)
                sb.AppendLine($"  {e.content} (imp={e.importance:F2})");

            sb.AppendLine($"\n[Dark Narratives] {store.dark.Count} entries");
            foreach (var d in store.dark)
                sb.AppendLine($"  {d.content}");

            Log.Message(sb.ToString());
        }

        [DebugAction("RimMind Memory", "Force Pawn Dark Memory (selected)", actionType = DebugActionType.Action)]
        public static void ForcePawnDarkMemory()
        {
            var pawn = Find.Selector.SingleSelectedThing as Pawn;
            if (pawn == null) { Log.Warning("[RimMind-Memory] Select a pawn first."); return; }

            if (!RimMindAPI.IsConfigured())
            {
                Log.Warning("[RimMind-Memory] API not configured, cannot trigger dark memory.");
                return;
            }

            var wc = RimMindMemoryWorldComponent.Instance;
            if (wc == null) return;

            var settings = RimMindMemoryMod.Settings;
            DarkMemoryUpdater.Instance?.TriggerPawnDarkMemoryUpdate(pawn, wc, settings);
            Log.Message($"[RimMind-Memory] Triggered dark memory update for {pawn.Name.ToStringShort}.");
        }

        [DebugAction("RimMind Memory", "Force Narrator Dark Memory", actionType = DebugActionType.Action)]
        public static void ForceNarratorDarkMemory()
        {
            if (!RimMindAPI.IsConfigured())
            {
                Log.Warning("[RimMind-Memory] API not configured, cannot trigger dark narrative.");
                return;
            }

            var wc = RimMindMemoryWorldComponent.Instance;
            if (wc == null) { Log.Warning("[RimMind-Memory] WorldComponent not initialized."); return; }

            var settings = RimMindMemoryMod.Settings;
            DarkMemoryUpdater.Instance?.TriggerNarratorDarkMemoryUpdate(wc, settings);
            Log.Message("[RimMind-Memory] Triggered narrator dark narrative update.");
        }

        [DebugAction("RimMind Memory", "Trigger Importance Decay", actionType = DebugActionType.Action)]
        public static void TriggerImportanceDecay()
        {
            var wc = RimMindMemoryWorldComponent.Instance;
            if (wc == null) { Log.Warning("[RimMind-Memory] WorldComponent not initialized."); return; }

            var settings = RimMindMemoryMod.Settings;
            int decayed = 0;
            int removed = 0;

            foreach (var store in wc.AllPawnStores)
            {
                int before = store.archive.Count;
                ImportanceDecayManager.ApplyDecay(store, settings.decayRate, settings.minImportanceThreshold);
                removed += before - store.archive.Count;
                decayed += store.active.Count + store.archive.Count;
            }

            int narratorBefore = wc.NarratorStore.archive.Count;
            ImportanceDecayManager.ApplyDecay(wc.NarratorStore, settings.decayRate, settings.minImportanceThreshold);
            removed += narratorBefore - wc.NarratorStore.archive.Count;

            Log.Message($"[RimMind-Memory] Decay complete: {decayed} memories decayed, {removed} below threshold removed.");
        }

        [DebugAction("RimMind Memory", "Show Working Memory (selected)", actionType = DebugActionType.Action)]
        public static void ShowWorkingMemory()
        {
            var pawn = Find.Selector.SingleSelectedThing as Pawn;
            if (pawn == null) { Log.Warning("[RimMind-Memory] Select a pawn first."); return; }

            var wc = RimMindMemoryWorldComponent.Instance;
            if (wc == null) { Log.Warning("[RimMind-Memory] WorldComponent not initialized."); return; }

            var wm = wc.GetOrCreateWorkingMemory(pawn);
            var sb = new StringBuilder();
            sb.AppendLine($"=== {pawn.Name.ToStringShort} Working Memory ({wm.Entries.Count}/{wm.Capacity}) ===");
            foreach (var e in wm.Entries)
                sb.AppendLine($"  {e.Content} | src={e.Source} | rel={e.Relevance:F2}");

            Log.Message(sb.ToString());
        }

        [DebugAction("RimMind Memory", "Clear Working Memory (selected)", actionType = DebugActionType.Action)]
        public static void ClearWorkingMemory()
        {
            var pawn = Find.Selector.SingleSelectedThing as Pawn;
            if (pawn == null) { Log.Warning("[RimMind-Memory] Select a pawn first."); return; }

            var wc = RimMindMemoryWorldComponent.Instance;
            if (wc == null) { Log.Warning("[RimMind-Memory] WorldComponent not initialized."); return; }

            wc.ClearWorkingMemory(pawn);
            Log.Message($"[RimMind-Memory] Cleared working memory for {pawn.Name.ToStringShort}.");
        }

        [DebugAction("RimMind Memory", "Show Memory Settings Summary", actionType = DebugActionType.Action)]
        public static void ShowMemorySettingsSummary()
        {
            var s = RimMindMemoryMod.Settings;
            var sb = new StringBuilder("=== RimMind Memory Settings ===");
            sb.AppendLine($"\nenableMemory: {s.enableMemory}");
            sb.AppendLine($"maxActive: {s.maxActive}  maxArchive: {s.maxArchive}  darkCount: {s.darkCount}");
            sb.AppendLine($"narratorMaxActive: {s.narratorMaxActive}  narratorMaxArchive: {s.narratorMaxArchive}  narratorDarkCount: {s.narratorDarkCount}");
            sb.AppendLine($"enableDecay: {s.enableDecay}  decayRate: {s.decayRate}  minImportanceThreshold: {s.minImportanceThreshold}");
            sb.AppendLine($"activeInjectRatio: {s.activeInjectRatio}  archiveInjectRatio: {s.archiveInjectRatio}");
            sb.AppendLine($"narratorActiveInjectRatio: {s.narratorActiveInjectRatio}  narratorArchiveInjectRatio: {s.narratorArchiveInjectRatio}");
            sb.AppendLine($"workingMemoryCapacity: {s.workingMemoryCapacity}");
            sb.AppendLine($"pawnToNarratorThreshold: {s.pawnToNarratorThreshold}  narratorEventThreshold: {s.narratorEventThreshold}");
            Log.Message(sb.ToString());
        }

        [DebugAction("RimMind Memory", "Show All Pawn Memory Counts", actionType = DebugActionType.Action)]
        public static void ShowAllPawnMemoryCounts()
        {
            var map = Find.CurrentMap;
            if (map == null) { Log.Warning("[RimMind-Memory] No current map."); return; }

            var wc = RimMindMemoryWorldComponent.Instance;
            if (wc == null) { Log.Warning("[RimMind-Memory] WorldComponent not initialized."); return; }

            var sb = new StringBuilder("=== All Pawn Memory Counts ===");
            foreach (var pawn in map.mapPawns.FreeColonists)
            {
                var store = wc.GetOrCreatePawnStore(pawn);
                sb.AppendLine($"  {pawn.Name.ToStringShort}: active={store.active.Count} archive={store.archive.Count} dark={store.dark.Count}");
            }
            Log.Message(sb.ToString());
        }
    }
}
