using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RimMind.Contracts.Npc;
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
            var driver = GetStorageDriver();
            if (driver == null) return;

            try
            {
                var snapshot = new MemoryStorageSnapshot
                {
                    pawnStores = _pawnStores.ToDictionary(
                        kv => kv.Key,
                        kv => kv.Value.active.Concat(kv.Value.archive).Concat(kv.Value.dark).ToList()),
                    narratorActive = _narratorStore.active.ToList(),
                    narratorArchive = _narratorStore.archive.ToList(),
                    narratorDark = _narratorStore.dark.ToList(),
                };
                var json = JsonConvert.SerializeObject(snapshot, Formatting.None);
                Task.Run(async () =>
                {
                    try { await driver.SaveAllEntriesAsync(json); }
                    catch (Exception ex) { Log.Warning($"[RimMind-Memory] SaveAllEntriesAsync failed: {ex.Message}"); }
                });
            }
            catch (Exception ex) { Log.Warning($"[RimMind-Memory] SaveAllEntriesToStorage failed: {ex.Message}"); }
        }

        private void LoadAllEntriesFromStorage()
        {
            var driver = GetStorageDriver();
            if (driver == null) return;

            Task.Run(async () =>
            {
                try
                {
                    var json = await driver.LoadAllEntriesAsync();
                    if (string.IsNullOrEmpty(json)) return;
                    var capturedJson = json;

                    LongEventHandler.ExecuteWhenFinished(() =>
                    {
                        try { MergeFromSnapshot(capturedJson); }
                        catch (Exception ex) { Log.Warning($"[RimMind-Memory] MergeFromSnapshot failed: {ex.Message}"); }
                    });
                }
                catch (Exception ex) { Log.Warning($"[RimMind-Memory] LoadAllEntriesAsync failed: {ex.Message}"); }
            });
        }

        private void MergeFromSnapshot(string? json)
        {
            if (string.IsNullOrEmpty(json)) return;
            var snapshot = JsonConvert.DeserializeObject<MemoryStorageSnapshot>(json!);
            if (snapshot == null) return;

            var settings = RimMindMemoryMod.Settings;

            if (snapshot.pawnStores != null)
            {
                foreach (var kv in snapshot.pawnStores)
                {
                    var store = GetOrCreatePawnStoreById(kv.Key);
                    foreach (var entry in kv.Value)
                        store.AddIfNotExists(entry);
                    PawnMemoryStore.EnforceLimit(store.active, settings.maxActive, store.archive, settings.maxArchive);
                    PawnMemoryStore.EnforceLimit(store.archive, settings.maxArchive, store.dark, int.MaxValue);
                }
            }

            if (snapshot.narratorActive != null)
                foreach (var entry in snapshot.narratorActive)
                    _narratorStore.AddIfNotExists(entry, isActive: true);

            if (snapshot.narratorArchive != null)
                foreach (var entry in snapshot.narratorArchive)
                    _narratorStore.AddIfNotExists(entry, isActive: false);

            if (snapshot.narratorDark != null)
                foreach (var entry in snapshot.narratorDark)
                    _narratorStore.AddIfNotExists(entry, isActive: false);

            PawnMemoryStore.EnforceLimit(_narratorStore.active, settings.narratorMaxActive, _narratorStore.archive, settings.narratorMaxArchive);
            PawnMemoryStore.EnforceLimit(_narratorStore.archive, settings.narratorMaxArchive, _narratorStore.dark, int.MaxValue);
        }

        private PawnMemoryStore GetOrCreatePawnStoreById(int thingId)
        {
            if (!_pawnStores.TryGetValue(thingId, out var store))
            {
                store = new PawnMemoryStore();
                _pawnStores[thingId] = store;
            }
            return store;
        }

        private class MemoryStorageSnapshot
        {
            public Dictionary<int, List<MemoryEntry>>? pawnStores;
            public List<MemoryEntry>? narratorActive;
            public List<MemoryEntry>? narratorArchive;
            public List<MemoryEntry>? narratorDark;
        }

        private IStorageDriver? GetStorageDriver()
        {
            try { return RimMind.Core.Npc.StorageDriverFactory.GetDriver(); }
            catch { return null; }
        }

        public void AddPawnMemory(Pawn pawn, MemoryEntry e, int maxActive, int maxArchive)
        {
            var store = GetOrCreatePawnStore(pawn);
            store.AddActive(e, maxActive, maxArchive);
        }

        public void AddNarratorMemory(MemoryEntry e, int maxActive, int maxArchive)
        {
            _narratorStore.AddActive(e, maxActive, maxArchive);
            var driver = GetStorageDriver();
            if (driver != null && driver.IsRemote)
            {
                Task.Run(async () =>
                {
                    try { await driver.PutAsync($"NPC-storyteller:narrator_{e.id}", e.content); }
                    catch { }
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
