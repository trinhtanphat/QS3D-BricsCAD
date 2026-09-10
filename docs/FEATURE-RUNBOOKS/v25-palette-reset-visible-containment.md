# BricsCAD V25 palette reset visibility-read containment

## Scope

Lane C03 owns the BricsCAD V25 Workspace/modeless palette lifecycle in `src/QS3D.BricsCAD.V25/PaletteCoordinator.cs`. This carrier hardens `ResetPreservingVisibility()` when BricsCAD is already tearing down or invalidating one of the native `PaletteSet` instances.

The change is limited to visibility snapshot/read behavior during palette reset. It does not change MCP runtime/transport, installer/release, Quantity engine behavior, CAD geometry, project persistence, or command transaction ownership.

## Defect and invariant

`PaletteSet.Visible` is a native-host property. A reset must not assume that all four native getters remain callable merely because the managed palette fields are non-null. If one getter throws while the host is tearing down a palette, reset must still dispose every stale palette best-effort, recreate the QS3D palette set, and restore every visibility value that was read successfully.

Capture the exact four currently-published `PaletteSet` instances first. Read each captured instance independently through a fail-closed helper. A failed/null read produces `visible = false` and an explicit `read = false`; one failing surface must not suppress reads of the other surfaces or abort disposal/recreation.

The owner-reference BIM dock pattern may be inferred only when all four reads are trustworthy. This prevents a native read failure from being misclassified as a legitimate hidden Properties/Quantity state and accidentally asserting the BIM dock contract from incomplete evidence.

The read helper is exact-instance and side-effect free: it must not resolve `MdiActiveDocument`, access project state, mutate visibility, dispose/recreate palettes, acquire transactions, or dispatch deferred UI work.

## Reset ordering

The reset lifecycle remains:

1. Snapshot the exact currently-published palette instances.
2. Read each captured `Visible` value independently with exception containment.
3. Derive the prior BIM-layout state only from a complete trustworthy four-surface snapshot.
4. Run the existing `Dispose()` lifecycle so layout persistence, Properties `StateChanged` unsubscribe, and best-effort native disposal remain authoritative.
5. Run `EnsureCreated()` to publish one coherent replacement palette set.
6. Restore dedicated Properties host state and the proven BIM dock contract when applicable.
7. Restore the per-surface visibility values; failed reads restore as hidden rather than guessing stale native state.

Do not add rollback into project/CAD state. This reset happens at the UI ownership boundary only.

## Deterministic verification

Run:

```text
python scripts/preflight-v25-palette-reset-visible-containment.py
python scripts/preflight-v25-palette-visibility-rollback.py
python scripts/preflight-all.py
```

The dedicated guard proves exact-instance snapshotting, per-surface native getter containment, trustworthy-read tracking for BIM inference, dispose/recreate/restore ordering, and absence of document/project/native setter side effects inside the read helper. The existing palette visibility rollback guard remains authoritative for setter-transition rollback.

Build BricsCAD V25 with the repository's admitted/locked reference workflow and run the deterministic smoke suite required by protected CI.

## Runtime classification

- `REMOTE_SAFE`: deterministic source guards, aggregate preflight, deterministic smoke, and admitted-reference V25 compilation.
- `LOCAL_ONLY`: licensed BricsCAD V25 native teardown/fault-injection where one or more `PaletteSet.Visible` getters throw during `ResetForNoDocument()` / palette recreation.

Remote green never implies the licensed native scenario passed. Report `LOCAL_ONLY / NO_RESULT` unless a compatible licensed BricsCAD V25 execution tied to the exact candidate actually exercises it.

## Manual licensed scenario

Create the QS3D palettes and preserve a mixed visibility pattern. During a no-document/reset transition, inject or reproduce failure of one native `Visible` getter while the other palette getters remain readable. Verify reset does not throw solely because of the read failure, all stale palette instances are still offered to the existing best-effort disposal path, a coherent replacement set is created, successful reads are restored, and the failed surface returns hidden rather than inheriting guessed state.

Repeat with a failure that would otherwise make Workspace+Right appear to be the owner-reference BIM pattern only because Properties or Quantity could not be read. Verify the BIM dock contract is not inferred from that incomplete snapshot. Also verify repeated reset/recreation does not duplicate `StateChanged` handlers or retain stale palette instances.
