# Agent Work Claim

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol`
- Started: `2026-08-11 22:21 +07`
- Baseline `main`: `69478a0e1e9f8371746647a137c700718ec68226`

## Scope

Make generated-stale inspection query-pure in Core. `ProjectElement.IsGeneratedGeometryStale()` and every `IsGenerated*Stale()` query must only read semantic metadata and return a `bool`; they must not remove obsolete stale markers as a side effect. Cleanup remains explicit through the existing `ClearGenerated*Stale()` mutation APIs.

Regression coverage will lock:

- genuine stale markers remain observable without query mutation;
- obsolete/replaced-handle markers evaluate non-stale but remain byte-for-byte present after queries;
- repeated `GeneratedGeometryStaleHealthService.Inspect(project)` calls are metadata-pure;
- explicit `ClearGenerated*Stale()` calls still remove the corresponding marker.

## Reserved files

- `src/QS3D.Core/Domain/ProjectElement.cs`
- `tests/QS3D.Core.SmokeTests/GeneratedGeometryStaleSmoke.cs`
- `tests/QS3D.Core.SmokeTests/GeneratedGeometryStaleHealthSmoke.cs`

`src/QS3D.Core/Diagnostics/GeneratedGeometryStaleHealthService.cs` is a read-only validation dependency and is not reserved for modification unless the source audit proves that is necessary.

## Exclusions / concurrency boundaries

- Do not touch `QsdbProjectStore.cs`, `ProjectSession.cs`, `ProjectUnitPolicy.cs`, or other files reserved by active Core claims.
- Do not overlap completed generated-ownership/source-recognition lanes, reporting null-element work, or unrelated UI work.
- Do not run GitHub Actions or release workflows under `continue all`.
- Do not claim BricsCAD V25 runtime validation from a remote/source-only environment.

## Validation

- Focused Core stale-marker smoke coverage.
- Focused Model Health stale inspection smoke coverage.
- Source review confirming no `Properties.Remove(...)` remains in any `IsGenerated*Stale()` query path.
- No GitHub Actions/release run.

## Completion

Pending implementation, focused smoke validation, PR, squash merge, and claim closure.
