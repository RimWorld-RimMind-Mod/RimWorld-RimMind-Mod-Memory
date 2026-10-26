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
        private readonly object _storesLock = new object();

        private static RimMindMemoryWorldComponent? _instance;
        public static RimMindMemoryWorldComponent? Instance =>
            Current.Game?.World != null && _instance?.world == Current.Game.World ? _instance : null;

        public RimMindMemoryWorldComponent(World world) : base(world)
        {
            _instance = this;
        }

        public PawnMemoryStore GetOrCreatePawnStore(Pawn pawn)
        {
            int id = pawn.thingIDNumber;
            lock (_storesLock)
            {
                if (!_pawnStores.TryGetValue(id, out var store))
                {
                    store = new PawnMemoryStore();
                    _pawnStores[id] = store;
                }
                return store;
            }
        }

        internal PawnMemoryStore? GetPawnStore(Pawn pawn)
        {
            lock (_storesLock)
            {
                return _pawnStores.TryGetValue(pawn.thingIDNumber, out var store) ? store : null;
            }
        }

        public NarratorMemoryStore NarratorStore => _narratorStore;

        public IEnumerable<PawnMemoryStore> AllPawnStores
        {
            get
            {
                lock (_storesLock)
                {
                    return new List<PawnMemoryStore>(_pawnStores.Values);
                }
            }
        }

        private bool _needsRemoteLoad;
        private int _lastPruneTick;
        private const int PruneIntervalTicks = 60000;
        public const int MaxRetainedInactivePawnStores = 8;

        public void ClearPawnStore(Pawn pawn)
        {
            lock (_storesLock)
            {
                _pawnStores.Remove(pawn.thingIDNumber);
            }
        }

        private void SaveAllEntriesToStorage()
        {
            if (!RimMindAPI.RemoteSync.IsConfigured) return;

            try
            {
                Dictionary<int, PawnMemoryStore> pawnStoresSnapshot;
                lock (_storesLock)
                {
                    pawnStoresSnapshot = new Dictionary<int, PawnMemoryStore>(_pawnStores);
                }
                string json = MemorySnapshotService.Serialize(pawnStoresSnapshot, _narratorStore);
                var version = Find.TickManager.TicksGame;
                Task.Run(async () =>
                {
                    var result = await RimMindAPI.RemoteSync.EnqueuePushAsync(
                        "rimmind:memory:full",
                        json,
                        version);
                    if (result.IsErr && result.Error.Code != RimMindErrorCode.Cancelled) RimMindErrors.Warn($"[RimMind-Memory] Remote push failed: {result.Error}");
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
                    RimMindAPI.MainThread.Post,
                    MergeFromSnapshot,
                    message => RimMindErrors.Warn($"[RimMind-Memory] {message}"));
            });
        }

        private void MergeFromSnapshot(string? json)
        {
            var settings = RimMindMemoryMod.Settings;
            lock (_storesLock)
            {
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
                    if (result.IsErr && result.Error.Code != RimMindErrorCode.Cancelled) RimMindErrors.Warn($"[RimMind-Memory] Remote push narrator failed: {result.Error}");
                });
            }
        }

        public IReadOnlyList<MemoryEntry> GetNarratorMemories()
        {
            lock (_narratorStore.SyncRoot)
            {
                var result = new List<MemoryEntry>(_narratorStore.active.Count + _narratorStore.dark.Count);
                result.AddRange(_narratorStore.active);
                result.AddRange(_narratorStore.dark);
                return result;
            }
        }

        // Read legacy save data only; inspecting context must not create a new buffer.
        public WM? GetWorkingMemory(Pawn pawn)
        {
            lock (_storesLock)
            {
                return _workingMemories.TryGetValue(pawn.thingIDNumber, out var wm) ? wm : null;
            }
        }

        public void ClearWorkingMemory(Pawn pawn)
        {
            lock (_storesLock)
            {
                _workingMemories.Remove(pawn.thingIDNumber);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            MemoryEntry.ExposeNextSeq();
            lock (_storesLock)
            {
                Scribe_Collections.Look(ref _pawnStores, "pawnStores", LookMode.Value, LookMode.Deep);
                _pawnStores ??= new Dictionary<int, PawnMemoryStore>();
                Scribe_Deep.Look(ref _narratorStore, "narratorStore");
                _narratorStore ??= new NarratorMemoryStore();
                Scribe_Collections.Look(ref _workingMemories, "workingMemories", LookMode.Value, LookMode.Deep);
                _workingMemories ??= new Dictionary<int, WM>();
            }

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

            int ticksGame = Find.TickManager?.TicksGame ?? 0;
            if (_lastPruneTick == 0)
            {
                _lastPruneTick = ticksGame;
                return;
            }
            if (ticksGame >= _lastPruneTick && (ticksGame - _lastPruneTick) < PruneIntervalTicks)
                return;

            _lastPruneTick = ticksGame;
            PruneInactivePawnState();
        }

        public void PruneInactivePawnState()
        {
            var retainedIds = CollectRetainedPawnIds();
            lock (_storesLock)
            {
                PrunePawnStores(_pawnStores, retainedIds, MaxRetainedInactivePawnStores);
                if (_workingMemories.Count > 0)
                {
                    var staleWorking = new List<int>();
                    foreach (var id in _workingMemories.Keys)
                    {
                        if (!retainedIds.Contains(id))
                            staleWorking.Add(id);
                    }
                    for (int i = 0; i < staleWorking.Count; i++)
                        _workingMemories.Remove(staleWorking[i]);
                }
            }

            Aggregation.WorkSessionAggregator.Instance?.PruneInactiveSessions(retainedIds);
        }

        public static int PrunePawnStores(
            Dictionary<int, PawnMemoryStore> pawnStores,
            ISet<int> retainedPawnIds,
            int maxRetainedInactive = MaxRetainedInactivePawnStores)
        {
            if (pawnStores == null || pawnStores.Count == 0) return 0;

            var inactiveCandidates = new List<(int id, int latestTick)>();
            var toRemove = new List<int>();

            foreach (var kv in pawnStores)
            {
                if (retainedPawnIds != null && retainedPawnIds.Contains(kv.Key))
                    continue;

                var store = kv.Value;
                if (store == null || store.IsEmpty)
                {
                    toRemove.Add(kv.Key);
                    continue;
                }

                int latestTick = GetLatestStoreTick(store);
                inactiveCandidates.Add((kv.Key, latestTick));
            }

            int cap = Math.Max(0, maxRetainedInactive);
            if (inactiveCandidates.Count > cap)
            {
                inactiveCandidates.Sort((a, b) => b.latestTick.CompareTo(a.latestTick));
                for (int i = cap; i < inactiveCandidates.Count; i++)
                {
                    toRemove.Add(inactiveCandidates[i].id);
                }
            }

            for (int i = 0; i < toRemove.Count; i++)
            {
                pawnStores.Remove(toRemove[i]);
            }

            return toRemove.Count;
        }

        private static int GetLatestStoreTick(PawnMemoryStore store)
        {
            int latest = int.MinValue;
            lock (store.SyncRoot)
            {
                for (int i = 0; i < store.active.Count; i++)
                    if (store.active[i].tick > latest) latest = store.active[i].tick;
                for (int i = 0; i < store.archive.Count; i++)
                    if (store.archive[i].tick > latest) latest = store.archive[i].tick;
                for (int i = 0; i < store.dark.Count; i++)
                    if (store.dark[i].tick > latest) latest = store.dark[i].tick;
            }
            return latest;
        }

        private static HashSet<int> CollectRetainedPawnIds()
        {
            var retained = new HashSet<int>();

            var allAliveColonists = RimWorld.PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_Colonists;
            if (allAliveColonists != null)
            {
                foreach (var pawn in allAliveColonists)
                {
                    if (pawn != null && pawn.thingIDNumber > 0)
                        retained.Add(pawn.thingIDNumber);
                }
            }

            if (Find.Maps != null)
            {
                foreach (var map in Find.Maps)
                {
                    var allMapPawns = map?.mapPawns?.AllPawns;
                    if (allMapPawns != null)
                    {
                        foreach (var p in allMapPawns)
                        {
                            if (p != null && p.thingIDNumber > 0 && !p.Destroyed
                                && (RimMindPawnLookup.IsEligibleColonist(p) || p.IsColonist || p.IsPrisonerOfColony || p.IsSlaveOfColony))
                            {
                                retained.Add(p.thingIDNumber);
                            }
                        }
                    }

                    var corpses = map?.listerThings?.ThingsInGroup(ThingRequestGroup.Corpse);
                    if (corpses != null)
                    {
                        foreach (var thing in corpses)
                        {
                            if (thing is Corpse corpse && corpse.InnerPawn is Pawn inner
                                && inner.thingIDNumber > 0 && !inner.Destroyed
                                && (inner.IsColonist || inner.IsPrisonerOfColony || inner.IsSlaveOfColony))
                            {
                                retained.Add(inner.thingIDNumber);
                            }
                        }
                    }
                }
            }

            var factions = Find.FactionManager?.AllFactionsListForReading;
            if (factions != null)
            {
                for (int i = 0; i < factions.Count; i++)
                {
                    var kidnapped = factions[i]?.kidnapped?.KidnappedPawnsListForReading;
                    if (kidnapped == null) continue;
                    for (int j = 0; j < kidnapped.Count; j++)
                    {
                        var p = kidnapped[j];
                        if (p != null && p.thingIDNumber > 0 && !p.Dead && !p.Destroyed)
                            retained.Add(p.thingIDNumber);
                    }
                }
            }

            return retained;
        }
    }
}
