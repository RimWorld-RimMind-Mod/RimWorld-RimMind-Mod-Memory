# RimMind Memory compact contracts

Active contract sources are `Contracts/*.cs`. The compact suite contains:

- `MemoryStoreContracts` — entry identity, layer routing, limits, importance ordering and legacy working-memory save compatibility.
- `MemoryParsingContextContracts` — dark-memory parsing, decay boundaries and time context.
- `RemoteSyncBoundaryContracts` — remote failure isolation, main-thread scheduling and successful merge boundaries.

Count actual discovered cases with the root test-budget script; all projects per mod must total fewer than 1000. Add behavior and failure-boundary tests, not private-shape assertions or unrelated aggregate scenarios.

## Cutover handoff

- Active include: `Contracts/**/*.cs`
- Shared support include:

  ```xml
  <Compile Include="..\..\RimMind-Core\TestSupport\ContractCaseRunner.cs"
           Link="Support\ContractCaseRunner.cs" />
  ```

- Required retained stub include: `VerseStubs.cs`
- Required production includes: `TimeFormatter.cs`, `ImportanceDecayCalculator.cs`,
  `MemoryEntry.cs`, `MemoryStoreBase.cs`, `PawnMemoryStore.cs`,
  `NarratorMemoryStore.cs`, `MemorySnapshotService.cs`,
  `ImportanceDecayManager.cs`, `WorkingMemory.cs`,
  `WorkingMemoryEntry.cs`, and `DarkMemoryResultParserPure.cs`
- Legacy compile categories to remove from the project entry during cutover:
  entry/store extended matrices, working-memory matrices, parser extended
  matrices, decay matrices, time formatter matrices and snapshot helper tests.

Snapshot serialization and merge now live in the pure `MemorySnapshotService`
seam. Its active contract covers layer round-trips, cross-layer deduplication,
legacy routing and capacity enforcement without compiling the Verse world
component.

## Support files

`VerseStubs.cs` and project metadata remain outside `Contracts/`; no retired
legacy test sources remain in this directory.
