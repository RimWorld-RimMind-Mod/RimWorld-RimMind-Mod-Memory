using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RimMind.Domain.ValueObjects;

namespace RimMind.Memory.Data
{
    internal sealed class MemorySnapshotLimits
    {
        public MemorySnapshotLimits(
            int pawnActive,
            int pawnArchive,
            int narratorActive,
            int narratorArchive)
        {
            PawnActive = pawnActive;
            PawnArchive = pawnArchive;
            NarratorActive = narratorActive;
            NarratorArchive = narratorArchive;
        }

        public int PawnActive { get; }
        public int PawnArchive { get; }
        public int NarratorActive { get; }
        public int NarratorArchive { get; }
    }

    internal sealed class MemoryLayerSnapshot
    {
        public List<MemoryEntry>? active;
        public List<MemoryEntry>? archive;
        public List<MemoryEntry>? dark;
    }

    internal sealed class MemoryStorageSnapshot
    {
        public Dictionary<int, MemoryLayerSnapshot>? pawnLayers;

        // Compatibility with the pre-layered remote snapshot. Legacy entries
        // are routed by MemoryType when no layered payload is present.
        public Dictionary<int, List<MemoryEntry>>? pawnStores = null;

        public List<MemoryEntry>? narratorActive;
        public List<MemoryEntry>? narratorArchive;
        public List<MemoryEntry>? narratorDark;
    }

    internal static class MemorySnapshotService
    {
        public static string Serialize(
            IReadOnlyDictionary<int, PawnMemoryStore> pawnStores,
            NarratorMemoryStore narratorStore)
        {
            var snapshot = new MemoryStorageSnapshot
            {
                pawnLayers = new Dictionary<int, MemoryLayerSnapshot>(),
                narratorActive = new List<MemoryEntry>(narratorStore.active),
                narratorArchive = new List<MemoryEntry>(narratorStore.archive),
                narratorDark = new List<MemoryEntry>(narratorStore.dark)
            };

            foreach (var pair in pawnStores)
            {
                snapshot.pawnLayers[pair.Key] = new MemoryLayerSnapshot
                {
                    active = new List<MemoryEntry>(pair.Value.active),
                    archive = new List<MemoryEntry>(pair.Value.archive),
                    dark = new List<MemoryEntry>(pair.Value.dark)
                };
            }

            return JsonConvert.SerializeObject(snapshot, Formatting.None);
        }

        public static void Merge(
            string? json,
            IDictionary<int, PawnMemoryStore> pawnStores,
            NarratorMemoryStore narratorStore,
            MemorySnapshotLimits limits)
        {
            if (string.IsNullOrWhiteSpace(json))
                return;

            var snapshot = JsonConvert.DeserializeObject<MemoryStorageSnapshot>(json!);
            if (snapshot == null)
                return;

            if (snapshot.pawnLayers != null)
            {
                foreach (var pair in snapshot.pawnLayers)
                {
                    PawnMemoryStore store = GetOrCreate(pawnStores, pair.Key);
                    MemoryLayerSnapshot? layers = pair.Value;
                    if (layers == null)
                        continue;

                    MergeLayer(store, store.active, layers.active);
                    MergeLayer(store, store.archive, layers.archive);
                    MergeLayer(store, store.dark, layers.dark);
                    EnforcePawnLimits(store, limits);
                }
            }
            else if (snapshot.pawnStores != null)
            {
                foreach (var pair in snapshot.pawnStores)
                {
                    PawnMemoryStore store = GetOrCreate(pawnStores, pair.Key);
                    if (pair.Value != null)
                    {
                        foreach (MemoryEntry entry in pair.Value)
                            store.AddIfNotExists(entry);
                    }
                    EnforcePawnLimits(store, limits);
                }
            }

            MergeLayer(narratorStore, narratorStore.active, snapshot.narratorActive);
            MergeLayer(narratorStore, narratorStore.archive, snapshot.narratorArchive);
            MergeLayer(narratorStore, narratorStore.dark, snapshot.narratorDark);
            MemoryStoreBase.EnforceLimit(
                narratorStore.active,
                limits.NarratorActive,
                narratorStore.archive,
                limits.NarratorArchive);
            MemoryStoreBase.EnforceLimit(
                narratorStore.archive,
                limits.NarratorArchive,
                narratorStore.dark,
                int.MaxValue);
        }

        private static PawnMemoryStore GetOrCreate(
            IDictionary<int, PawnMemoryStore> stores,
            int pawnId)
        {
            if (!stores.TryGetValue(pawnId, out PawnMemoryStore? store))
            {
                store = new PawnMemoryStore();
                stores[pawnId] = store;
            }
            return store;
        }

        private static void MergeLayer(
            MemoryStoreBase store,
            ICollection<MemoryEntry> target,
            IEnumerable<MemoryEntry>? entries)
        {
            if (entries == null)
                return;

            foreach (MemoryEntry? entry in entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.id) || store.ContainsId(entry.id))
                    continue;
                target.Add(entry);
            }
        }

        private static void EnforcePawnLimits(
            PawnMemoryStore store,
            MemorySnapshotLimits limits)
        {
            MemoryStoreBase.EnforceLimit(
                store.active,
                limits.PawnActive,
                store.archive,
                limits.PawnArchive);
            MemoryStoreBase.EnforceLimit(
                store.archive,
                limits.PawnArchive,
                store.dark,
                int.MaxValue);
        }
    }

    internal static class MemoryRemoteSyncCoordinator
    {
        public static async Task PullAndMergeAsync(
            Func<Task<Result<string?, RimMindError>>> pull,
            Action<Action> scheduleOnMainThread,
            Action<string> merge,
            Action<string> warn)
        {
            Result<string?, RimMindError> result;
            try
            {
                result = await pull().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                warn($"Remote pull failed: {ex.Message}");
                return;
            }

            if (result.IsErr)
            {
                warn($"Remote pull failed: {result.Error}");
                return;
            }

            string? json = result.Value;
            if (string.IsNullOrWhiteSpace(json))
                return;

            scheduleOnMainThread(() =>
            {
                try
                {
                    merge(json!);
                }
                catch (Exception ex)
                {
                    warn($"MergeFromSnapshot failed: {ex.Message}");
                }
            });
        }
    }
}
