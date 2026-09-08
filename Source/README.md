# Memory runtime map

RimMind-Memory owns per-game pawn, narrator, and working memory. It has no
request coordinator: external writes enter through Core's memory capability,
and event producers write to the same world-component state owner.

## Reading order

1. `RimMindMemoryMod.cs` — composition, Harmony setup, and Core extension registration.
2. `RimMindMemoryAPI.cs` and `Core/RimMindMemoryBridge.cs` — public compatibility and typed Core boundary.
3. `Data/RimMindMemoryWorldComponent.cs` — per-game state ownership and lifecycle.
4. `Data/MemoryStoreBase.cs`, `PawnMemoryStore.cs`, and `NarratorMemoryStore.cs` — layer, identity, capacity, and ordering rules.
5. `Triggers/MemoryTriggerHelper.cs`, `Aggregation/`, `Decay/`, and `DarkMemory/` — memory producers and policies.
6. `Injection/` and `Data/MemorySnapshotService.cs` — context consumers and remote snapshot boundary.
7. `../Tests/README.md` — compact permanent contracts.

## Main flows

```text
game event or external API
  -> MemoryTriggerHelper or RimMindMemoryBridge
  -> RimMindMemoryWorldComponent
  -> PawnMemoryStore / NarratorMemoryStore / WorkingMemory
  -> context providers and snapshot synchronization
```

```text
RimWorld settings or Core settings tab
  -> MemorySettingsDrawer
  -> RimMindMemorySettings
```

## Boundaries

- Child modules write through `RimMindAPI.Memory`; they do not access stores or the world component.
- `RimMindMemoryWorldComponent` owns persistent per-game state.
- `MemoryStoreBase` owns shared three-layer rules; derived stores express target-specific entry behavior.
- Providers read state and format context. They do not own storage transitions.
- `MemoryContextProvider` also publishes synchronous pawn and narrator briefs through Core's public provider registry for optional consumers; Verse reads occur on the main thread.
- Network and serialization work stays behind the snapshot and Core storage boundaries.
- Verse and Unity side effects remain on the main thread.

## Focused verification

```powershell
dotnet test RimMind-Memory/Tests/RimMindMemory.Tests.csproj -c Release
dotnet build RimMind-Memory/Source/RimMindMemory.csproj -c Release
```

The permanent suite contains eight aggregate Facts. The game Autotester remains
separate and is currently blocked by missing resources.
