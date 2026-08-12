# Work claim — Atomic file path distinctness

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T07:53:00+07:00`
- Baseline main SHA observed: `f7d257200861948f09a3c16919374056e5b9737f`
- Priority: P1 — persistence publication safety.

## Confirmed defect

`AtomicFileCommit.PublishNew(tempPath, destinationPath, backupPath)` validates only presence/nonblank paths. If `destinationPath` and `backupPath` canonicalize to the same filesystem path and the pair is initially absent, publication moves the temporary file to the destination, then immediately observes the same file through `backupPath`, deletes the newly published primary as rollback, and throws. The caller loses the only newly produced publication artifact even though the invalid path relationship could have been rejected before mutation.

The same helper should fail closed on any aliased temp/destination/backup identities before filesystem mutation, including case-insensitive aliases on Windows, instead of delegating invalid identity combinations to `File.Replace`/fallback behavior.

## Reserved scope

- `src/QS3D.Core/Persistence/AtomicFileCommit.cs` — canonical path identity preflight for replace/publish entry points.
- `tests/QS3D.Core.SmokeTests/AtomicFileCommitPathIdentitySmoke.cs` — focused regression for destination/backup and temp/destination aliases plus valid distinct-path behavior.

## Explicit exclusions

- No changes to `QsdbProjectStore`, `RevisionSnapshotStore`, Quantity Settings persistence, or caller-specific backup policy.
- No redesign of fallback recovery sequencing beyond preflight identity validation.
- No GitHub Actions dispatch and no BricsCAD runtime qualification.

## Implementation plan

1. Re-fetch moving `main` after this claim and confirm `AtomicFileCommit` is unchanged and unclaimed by another active agent.
2. Canonicalize input paths and reject aliased identities before any move/replace/delete operation; use OS-appropriate path comparison so Windows case aliases fail closed.
3. Preserve all current valid distinct-path `ReplaceWithBackup`, `ReplaceWithoutBackup`, and `PublishNew` behavior.
4. Add deterministic filesystem smoke coverage proving invalid aliases throw before mutation and leave temp/destination state intact, while a normal distinct `PublishNew` still succeeds.
5. Refresh moving `main`, verify the claim commit is an ancestor and the reserved source stayed unchanged by other agents, then commit source and regression separately if safe.
6. Close this claim with exact implementation/regression SHAs and the honest validation boundary.

## Validation policy

GitHub Actions remain manual-only and will not be dispatched. Static source/diff review plus committed deterministic smoke coverage may be reported; executable .NET smoke PASS and licensed BricsCAD V25/V26 runtime PASS will not be claimed without actual execution evidence.
