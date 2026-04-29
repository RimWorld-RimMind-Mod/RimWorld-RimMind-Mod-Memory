using System.Collections.Generic;
using System.Threading.Tasks;
using RimMind.Core.Npc;
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

        public void ClearPawnStore(Pawn pawn) => _pawnStores.Remove(pawn.thingIDNumber);

        private IStorageDriver? GetStorageDriver()
        {
            try { return StorageDriverFactory.GetDriver(); }
            catch { return null; }
        }

        public void AddPawnMemory(Pawn pawn, MemoryEntry e, int maxActive, int maxArchive)
        {
            var store = GetOrCreatePawnStore(pawn);
            store.AddActive(e, maxActive, maxArchive);
            var driver = GetStorageDriver();
            if (driver != null && driver.IsRemote)
            {
                var npcId = $"NPC-{pawn.thingIDNumber}";
                Task.Run(async () =>
                {
                    try { await driver.PutAsync($"{npcId}:memory_{e.type}_{e.id}", e.content); }
                    catch { }
                });
            }
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
        }
    }
}
