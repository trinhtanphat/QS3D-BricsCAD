# ProjectState snapshot active-context referential integrity

## Purpose

`ProjectStateSnapshot` must not retain or transport a project whose `ActiveZoneId` or `ActiveFloorId` points at a catalog entry that no longer exists. `ProjectState.Zones` and `ProjectState.Floors` are mutable collections, so callers can create this inconsistent state after assigning an otherwise valid active context.

## Contract

Before `Capture` or `CreateDetachedCopy` materializes snapshot state:

- collection count/null/duplicate validation runs first;
- a non-empty `ActiveZoneId` must resolve through `ProjectState.FindZone`;
- a non-empty `ActiveFloorId` must resolve through `ProjectState.FindFloor`;
- empty active-context IDs remain valid;
- valid stored identity text is preserved exactly, including case;
- rejection is fail-closed and must not clear, repair, normalize, or otherwise mutate source state.

The lookup deliberately reuses `ProjectState.FindZone` / `FindFloor`, so snapshot semantics stay aligned with the canonical case-insensitive unique-ID lookup policy. QSDB load already rejects orphan active-context references; snapshot retention now enforces the same referential-integrity boundary.

## Deterministic coverage

`ProjectStateSnapshotActiveContextIntegritySmoke` proves both `Capture` and `CreateDetachedCopy` reject dangling Zone/Floor contexts without changing the source `ChangeVersion`, `UpdatedUtc`, active identity, or catalog contents. It also proves resolved and empty controls remain valid and resolved identity text is preserved byte-semantically.

`scripts/preflight-project-state-snapshot-active-context-integrity.py` pins production ordering, canonical lookup reuse, fail-closed behavior, smoke registration, and parity with the existing QSDB active-context regression coverage.

## Validation

Run the focused source guard:

```text
python scripts/preflight-project-state-snapshot-active-context-integrity.py
```

Then run the registered Core smoke suite/build through the repository's normal shared CI. This feature is deterministic Core persistence/model-lifecycle behavior and requires no licensed BricsCAD runtime evidence.
