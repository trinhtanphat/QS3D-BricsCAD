# Work claim — Atomic file path distinctness

- Status: `DONE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T07:53:00+07:00`
- Baseline main SHA observed: `f7d257200861948f09a3c16919374056e5b9737f`
- Priority: P1 — persistence publication safety.

## Confirmed defect

`AtomicFileCommit.PublishNew(tempPath, destinationPath, backupPath)` validated only presence/nonblank paths. If `destinationPath` and `backupPath` canonicalized to the same filesystem path and the pair was initially absent, publication could move the temporary file to the destination, then immediately observe the same file through `backupPath`, delete the newly published primary as rollback, and throw. The invalid path relationship was therefore capable of causing avoidable filesystem mutation and loss of the new publication artifact.

## Implemented

- `src/QS3D.Core/Persistence/AtomicFileCommit.cs`
  - canonicalizes temp/destination/backup paths before publication;
  - rejects temp/destination, temp/backup, and destination/backup aliases before move/replace/delete;
  - uses case-insensitive comparison on Windows and ordinal comparison on case-sensitive path platforms;
  - preserves existing valid distinct-path replace/publish and fallback behavior.
- `tests/QS3D.Core.SmokeTests/AtomicFileCommitPathIdentitySmoke.cs`
  - guards canonical destination/backup alias rejection before `PublishNew` mutation;
  - guards destination/backup alias rejection before replacement;
  - guards temp/destination alias rejection before replacement;
  - retains a valid distinct-path `PublishNew` success contract.

## Commits

- Claim: `01e22bc93c6e0650ffdba8f3c30152975b403ee0`
- Implementation: `9bf794216b657040ae4312db4b2d61693fe4564c`
- Regression: `b67770612cdb9cac4e3cc294d3198bb7e9bec6b0`

## Integration verification

Immediately before closing, `main` was fetched at `b67770612cdb9cac4e3cc294d3198bb7e9bec6b0`, with the implementation and regression commits directly integrated on `main`. The claim had remained an ancestor while concurrent intervening commits changed only unrelated claim/documentation files, and the reserved source blob was unchanged before implementation.

## Explicit exclusions

- No changes to `QsdbProjectStore`, `RevisionSnapshotStore`, Quantity Settings persistence, or caller-specific backup policy.
- No redesign of fallback recovery sequencing beyond preflight identity validation.
- No GitHub Actions dispatch and no BricsCAD runtime qualification.

## Validation boundary

GitHub Actions were not dispatched. Source/diff/static-contract review and deterministic smoke coverage are committed, but executable .NET smoke PASS is not claimed because this environment does not provide the repository's .NET execution toolchain. Licensed BricsCAD V25/V26 runtime PASS is also not claimed; that remains local-only qualification.
