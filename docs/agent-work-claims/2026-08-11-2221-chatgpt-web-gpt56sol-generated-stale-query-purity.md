# Agent Work Claim

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol`
- Started: `2026-08-11 22:21 +07`
- Completed: `2026-08-11 22:32 +07`
- Baseline `main`: `69478a0e1e9f8371746647a137c700718ec68226`
- Claim commit: `f5ea20191b9ec6c824b6c82469aeb9e36789a625`
- Pull request: `#503`
- Squash merge: `dba801b10c492376370886f304ccd873260f5e27`

## Scope

Make generated-stale inspection query-pure in Core. `ProjectElement.IsGeneratedGeometryStale()` and every `IsGenerated*Stale()` query must only read semantic metadata and return a `bool`; they must not remove obsolete stale markers as a side effect. Cleanup remains explicit through the existing `ClearGenerated*Stale()` mutation APIs.

Regression coverage locks:

- genuine stale markers remain observable without query mutation;
- obsolete/replaced-handle markers evaluate non-stale but remain byte-for-byte present after queries;
- repeated `GeneratedGeometryStaleHealthService.Inspect(project)` calls are metadata-pure;
- explicit `ClearGenerated*Stale()` calls still remove the corresponding marker;
- Curtain Panel uses the same query-purity contract as the regular generated-output paths.

## Reserved files

- `src/QS3D.Core/Domain/ProjectElement.cs`
- `tests/QS3D.Core.SmokeTests/GeneratedGeometryStaleSmoke.cs`
- `tests/QS3D.Core.SmokeTests/GeneratedGeometryStaleHealthSmoke.cs`

`src/QS3D.Core/Diagnostics/GeneratedGeometryStaleHealthService.cs` remained unchanged; the defect was entirely in the query methods it calls.

## Implementation

- Removed aggregate stale metadata cleanup from `IsGeneratedGeometryStale()`.
- Removed state/snapshot cleanup from `IsGeneratedOutputStale(...)`.
- Removed state/snapshot cleanup from `IsGeneratedCurtainPanelOutputStale()`.
- Preserved explicit cleanup in `ClearGenerated*Stale()` and `ClearGeneratedGeometryStale()`.
- Reworked stale smoke expectations so replaced handles become logically fresh without silently deleting their obsolete marker metadata.
- Added repeated query and repeated Model Health inspection checks that compare metadata before/after inspection.

Feature-branch implementation commits included:

- `87a6e189008e44424218592f159770cee764f95d` — Core query-purity source fix.
- `4c98e52dc34900d063f80baa10336bce941ff11e` — stale lifecycle/query-purity regression coverage.
- `e1b05013e6d129e33042b6e405a16f446a8bed75` — Model Health metadata-purity regression coverage.
- Final merge-safe feature head before squash: `097a88c468c81dc03484648565e534bb21dbdd4f`.

## Concurrency / validation

- Re-checked fast-moving `main` repeatedly before implementation, PR creation, and merge.
- Concurrent commits did not touch any of the three reserved files; feature syncs overlaid only those exact blobs and used no force updates.
- PR #503 changed exactly three files.
- Static source review passed: no `Remove(...)` remains inside `IsGeneratedGeometryStale()`, `IsGeneratedOutputStale(...)`, or `IsGeneratedCurtainPanelOutputStale()`; mutation remains in explicit clear helpers.
- Executable Core smoke was attempted, but the remote runtime had no existing checkout and direct clone failed with `Could not resolve host: github.com`; therefore no runtime smoke PASS is claimed.
- GitHub Actions/release workflows were not run because `continue all` does not authorize them.
- No BricsCAD V25 runtime PASS is claimed.

## Completion

PR #503 was squash-merged to `main` as `dba801b10c492376370886f304ccd873260f5e27`. Claim closed after verifying the query-purity source is present on `main`.
