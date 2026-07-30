using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RimMind.Domain.ValueObjects;
using RimMind.Presentation.Api;
using RimWorld.Planet;
using Verse;
using WM = RimMind.Memory.WorkingMemory.WorkingMemory;

namespace RimMind.Memory.Data
{
    public class RimMindMemoryWorldComponent : WorldComponent
    {
        private Dictionary<int, PawnMemoryStore> _pawnStores = new Dictionary<int, PawnMemoryStore>();
        private NarratorMemoryStore _narratorStore = new NarratorMemoryStore();
        private Dictionary<int, WM> _workingMemories = new Dictionary<int, WM>();

        private static RimMindMemoryWorldComponent? _instance;
        public static RimMindMemoryWorldComponent? Instance => _instance;

        public RimMindMemoryWorldComponent(World world) : base(world)
        {
            _instance = this;
        }

        public PawnMemoryStore GetOrCreatePawnStore(Pawn pawn)
        {
            int id = pawn.thingIDNumber;
            if (!_pawnStores.TryGetValue(id, out var store))
            {
                store = new PawnMemoryStore();
                _pawnStores[id] = store;
            }
            return store;
        }

        public NarratorMemoryStore NarratorStore => _narratorStore;

        public IEnumerable<PawnMemoryStore> AllPawnStores => _pawnStores.Values;

        private bool _needsRemoteLoad;

        public void ClearPawnStore(Pawn pawn) => _pawnStores.Remove(pawn.thingIDNumber);

        private void SaveAllEntriesToStorage()
        {
            if (!RimMindAPI.RemoteSync.IsConfigured) return;

            try
            {
                string json = MemorySnapshotService.Serialize(_pawnStores, _narratorStore);
                var version = Find.TickManager.TicksGame;
                Task.Run(async () =>
                {
                    var result = await RimMindAPI.RemoteSync.EnqueuePushAsync(
                        "rimmind:memory:full",
                        json,
                        version);
                    if (result.IsErr) RimMindErrors.Warn($"[RimMind-Memory] Remote push failed: {result.Error}");
                });
            }
            catch (Exception ex) { RimMindErrors.Warn($"[RimMind-Memory] SaveAllEntriesToStorage failed: {ex.Message}"); }
        }

        private void LoadAllEntriesFromStorage()
        {
            if (!RimMindAPI.RemoteSync.IsConfigured) return;

            Task.Run(async () =>
            {
                await MemoryRemoteSyncCoordinator.PullAndMergeAsync(
                    () => RimMindAPI.RemoteSync.SyncOnLoadAsync("rimmind:memory:full", 0),
                    LongEventHandler.ExecuteWhenFinished,
                    MergeFromSnapshot,
                    message => RimMindErrors.Warn($"[RimMind-Memory] {message}"));
            });
        }

        private void MergeFromSnapshot(string? json)
        {
            var settings = RimMindMemoryMod.Settings;
            MemorySnapshotService.Merge(
                json,
                _pawnStores,
                _narratorStore,
                new MemorySnapshotLimits(
                    settings.maxActive,
                    settings.maxArchive,
                    settings.narratorMaxActive,
                    settings.narratorMaxArchive));
        }

        public void AddPawnMemory(Pawn pawn, MemoryEntry e, int maxActive, int maxArchive)
        {
            var store = GetOrCreatePawnStore(pawn);
            store.AddActive(e, maxActive, maxArchive);
        }

        public void AddNarratorMemory(MemoryEntry e, int maxActive, int maxArchive)
        {
            _narratorStore.AddActive(e, maxActive, maxArchive);
            if (RimMindAPI.RemoteSync.IsConfigured)
            {
                int tick = Find.TickManager.TicksGame;
                Task.Run(async () =>
                {
                    var key = "rimmind:memory:narrator";
                    var result = await RimMindAPI.RemoteSync.EnqueuePushAsync(key, e.content, tick);
                    if (result.IsErr) RimMindErrors.Warn($"[RimMind-Memory] Remote push narrator failed: {result.Error}");
                });
            }
        }

        public IReadOnlyList<MemoryEntry> GetNarratorMemories()
        {
            var result = new List<MemoryEntry>(_narratorStore.active.Count + _narratorStore.dark.Count);
            result.AddRange(_narratorStore.active);
            result.AddRange(_narratorStore.dark);
            return result;
        }

        public WM GetOrCreateWorkingMemory(Pawn pawn)
        {
            int id = pawn.thingIDNumber;
            if (!_workingMemories.TryGetValue(id, out var wm))
            {
                wm = new WM(RimMindMemoryMod.Settings.workingMemoryCapacity);
                _workingMemories[id] = wm;
            }
            else
            {
                wm.UpdateCapacity(RimMindMemoryMod.Settings.workingMemoryCapacity);
            }
            return wm;
        }

        public WM? GetWorkingMemory(Pawn pawn)
        {
            return _workingMemories.TryGetValue(pawn.thingIDNumber, out var wm) ? wm : null;
        }

        public void ClearWorkingMemory(Pawn pawn) => _workingMemories.Remove(pawn.thingIDNumber);

        public override void ExposeData()
        {
            base.ExposeData();
            MemoryEntry.ExposeNextSeq();
            Scribe_Collections.Look(ref _pawnStores, "pawnStores", LookMode.Value, LookMode.Deep);
            _pawnStores ??= new Dictionary<int, PawnMemoryStore>();
            Scribe_Deep.Look(ref _narratorStore, "narratorStore");
            _narratorStore ??= new NarratorMemoryStore();
            Scribe_Collections.Look(ref _workingMemories, "workingMemories", LookMode.Value, LookMode.Deep);
            _workingMemories ??= new Dictionary<int, WM>();

            if (Scribe.mode == LoadSaveMode.Saving)
                SaveAllEntriesToStorage();
            else if (Scribe.mode == LoadSaveMode.LoadingVars)
                _needsRemoteLoad = true;
        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            if (_needsRemoteLoad)
            {
                _needsRemoteLoad = false;
                LoadAllEntriesFromStorage();
            }
        }
    }
}
