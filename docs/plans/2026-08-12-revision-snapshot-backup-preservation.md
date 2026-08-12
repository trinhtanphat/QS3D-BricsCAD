# Plan — Revision Snapshot validated-backup preservation

## Goal

Keep the last strict-valid Revision Snapshot backup recoverable when the primary snapshot is missing/corrupt and a new valid snapshot is saved.

## Existing contract

- `RevisionSnapshotStore.LoadWithBackupFallback()` treats `<path>.bak` as recovery state when the primary has a recoverable data failure.
- `RevisionSnapshotStore.Load()` is the strict snapshot/schema/canonical validator.
- Normal saves rotate a valid primary to `.bak` with `AtomicFileCommit.ReplaceWithBackup()`.
- `QsdbProjectStore.SavePreservingValidatedBackup()` establishes the repository precedent for preserving a validated backup by replacing only the primary and validating both artifacts afterward.

## Defect

`RevisionSnapshotStore.Save()` currently always calls `ReplaceWithBackup()`. If primary is corrupt/missing while `.bak` remains strict-valid, a save can replace the good backup with the bad primary before publishing the new snapshot. The store then loses the fallback artifact it explicitly supports.

## Implementation

1. Before publication, determine whether the existing sidecar must be preserved:
   - no `.bak` => normal save;
   - primary strict-loads => normal save;
   - primary fails only with an existing recoverable-data failure => strict-load `.bak`;
   - if `.bak` strict-loads, preserve it; if it is also a recoverable-data failure, use the normal rotation path because there is no validated backup to protect;
   - non-recoverable I/O/permission failures remain visible and abort the save.
2. Serialize and strict-validate the new temporary snapshot exactly as today.
3. Preservation path: `AtomicFileCommit.ReplaceWithoutBackup(temp, full)` so `<path>.bak` is untouched, then strict-load both new primary and preserved backup.
4. Normal path: retain `AtomicFileCommit.ReplaceWithBackup(temp, full, backup)` unchanged.

## Regression

Filesystem smoke in an isolated temp directory:

1. Save snapshot A, then B, producing primary B + backup A.
2. Corrupt primary B and prove `LoadWithBackupFallback()` returns A.
3. Save snapshot C.
4. Prove primary strict-loads C and `.bak` strict-loads A.
5. Corrupt the new primary and prove fallback still returns A.
6. Separate normal-path check: with valid primary + backup, saving a later snapshot continues rotating the valid primary into `.bak`.

## Static guard

Require:

- strict primary/backup probe based on `Load()` and `IsRecoverableDataFailure`;
- preservation publication through `ReplaceWithoutBackup`;
- post-publication strict validation of primary + backup;
- normal `ReplaceWithBackup` path retained;
- behavioral smoke markers for corrupt-primary recovery and preserved `.bak`.

## Validation boundary

GitHub Actions remain manual-only and are not dispatched. Hosted evidence is source/diff/static-contract review plus committed deterministic smoke/preflight coverage. Executable smoke/preflight PASS and licensed BricsCAD V25/V26 runtime PASS are not claimed without actual execution evidence.
