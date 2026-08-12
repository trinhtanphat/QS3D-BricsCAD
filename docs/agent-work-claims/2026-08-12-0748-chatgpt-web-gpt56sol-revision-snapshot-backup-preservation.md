# Work claim — Revision Snapshot validated-backup preservation

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T07:48:00+07:00`
- Baseline main SHA observed: `f1017910a419bd095c36c5b471d9507311482809`
- Priority: P1 — deterministic persistence/recovery integrity.

## Confirmed defect

`RevisionSnapshotStore` explicitly supports recovery from `<snapshot>.bak` through `LoadWithBackupFallback()`, but `Save()` always publishes through `AtomicFileCommit.ReplaceWithBackup(...)`. If the primary revision snapshot is corrupt while `.bak` is still strict-valid, a subsequent save can rotate the corrupt primary over the only validated recovery artifact before publishing the new primary. This destroys the fallback that the same store treats as recoverable state.

`QsdbProjectStore.SavePreservingValidatedBackup()` already establishes the repository recovery invariant: strict-validate the existing backup, replace only the primary, then strict-validate both primary and preserved backup.

## Reserved scope

- `src/QS3D.Core/Revisions/RevisionSnapshotStore.cs` — save/publication behavior needed to preserve an already validated backup when the primary is invalid.
- Focused Core smoke regression for corrupt-primary + valid-backup recovery followed by save.
- Focused static preflight and planning documentation.

## Explicit exclusions

- No changes to `QsdbProjectStore` or `AtomicFileCommit`; they are reference infrastructure only.
- No changes to `RevisionService` capture/compare semantics, snapshot XML schema/canonicalization, quantity revision semantics, or native/UI revision workflows.
- No BricsCAD V25/V26 runtime qualification.

## Implementation plan

1. Re-fetch moving `main` after this claim and confirm `RevisionSnapshotStore.Save()` still unconditionally rotates the primary into `.bak`.
2. Add a recovery-safe save mode/path mirroring the existing QSDB invariant: require the existing `.bak` to strict-load before preserving it, publish the new primary without backup rotation, then strict-load both resulting primary and preserved backup.
3. Keep the normal `ReplaceWithBackup` path unchanged when there is no validated backup that must be preserved.
4. Add filesystem smoke coverage: create valid primary/backup history, corrupt primary, prove fallback loads backup, save a new snapshot, prove new primary strict-loads and old validated backup still strict-loads; then corrupt/remove the new primary and prove fallback remains available.
5. Add a focused static preflight that requires preservation and normal publication paths and forbids regression to a single unconditional backup rotation path.
6. Refresh moving `main`, verify no reserved-source overlap, review the full PR diff, squash-merge with expected-head protection, and close this claim with exact evidence.

## Validation policy

GitHub Actions remain manual-only and will not be dispatched. Source/diff/static-contract review plus committed deterministic smoke/preflight coverage may be reported; executable smoke/preflight PASS and licensed BricsCAD V25/V26 runtime PASS will not be claimed without actual execution evidence.
