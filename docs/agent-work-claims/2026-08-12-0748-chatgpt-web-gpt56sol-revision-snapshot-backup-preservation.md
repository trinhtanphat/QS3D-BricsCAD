# Work claim — Revision Snapshot validated-backup preservation

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T07:48:00+07:00`
- Completed: `2026-08-12T07:53:00+07:00`
- Baseline main SHA observed: `f1017910a419bd095c36c5b471d9507311482809`
- Claim commit: `c711cebba3b967b02113519b1c2875e8ed3e9e79`
- PR: `#628`
- Squash merge on `main`: `f7d257200861948f09a3c16919374056e5b9737f`
- Priority: P1 — deterministic persistence/recovery integrity.

## Defect closed

`RevisionSnapshotStore` explicitly supports recovery from `<snapshot>.bak` through `LoadWithBackupFallback()`, but `Save()` previously always published through `AtomicFileCommit.ReplaceWithBackup(...)`. If the primary revision snapshot was corrupt while `.bak` was still strict-valid, a subsequent save could rotate the corrupt primary over the only validated recovery artifact before publishing the new primary.

## Implemented

- `Save()` now probes whether an existing backup must be preserved after the new temp snapshot has itself been strict-validated.
- If `.bak` exists, primary `Load()` fails with the store's existing recoverable-data classification, and backup `Load()` succeeds, the save publishes through `AtomicFileCommit.ReplaceWithoutBackup(...)` so `<path>.bak` remains untouched.
- The preservation path strict-loads both the newly published primary and the preserved backup after publication, matching the repository's established QSDB recovery invariant.
- Non-recoverable I/O/permission failures are not swallowed by the recovery probe.
- If the primary is valid, or there is no strict-valid backup to protect, the existing `ReplaceWithBackup(...)` rotation path remains unchanged.
- Added filesystem smoke coverage proving corrupt-primary fallback survives a later save and a second primary corruption, plus a normal-path regression proving valid-primary saves still rotate the prior primary into `.bak`.
- Added isolated smoke registration, focused static preflight, and planning documentation.

## Reserved scope

- `src/QS3D.Core/Revisions/RevisionSnapshotStore.cs` — save/publication behavior needed to preserve an already validated backup when the primary is invalid.
- Focused Core smoke regression for corrupt-primary + valid-backup recovery followed by save.
- Focused static preflight and planning documentation.

## Explicit exclusions honored

- `QsdbProjectStore` and `AtomicFileCommit` were not changed.
- `RevisionService` capture/compare semantics, snapshot XML schema/canonicalization, quantity revision semantics, and native/UI revision workflows were not changed.
- No BricsCAD V25/V26 runtime qualification.

## Validation evidence

- Post-claim source at `c711cebba3b967b02113519b1c2875e8ed3e9e79` confirmed `RevisionSnapshotStore.Save()` still used a single unconditional backup-rotation path before implementation.
- Production diff was limited to `RevisionSnapshotStore.cs` at +32/-1; branch changed exactly five files including plan, smoke, registration, and preflight.
- PR #628 full diff was reviewed before merge.
- Moving-main comparison found 33 concurrent commits after the claim point with zero overlap in `RevisionSnapshotStore.cs` or the lane's four new files; two further commits after PR base were also unrelated.
- Squash merge used expected head `ffede3efec15afd63d5cff6eac1e4f7b5c472d13` and succeeded as `f7d257200861948f09a3c16919374056e5b9737f`.
- GitHub Actions were not dispatched because repository policy is manual-only.
- Executable smoke/preflight PASS and licensed BricsCAD V25/V26 runtime PASS are not claimed from this connector-only environment.
