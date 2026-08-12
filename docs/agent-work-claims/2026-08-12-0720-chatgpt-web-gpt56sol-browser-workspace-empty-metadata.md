# Work claim — Browser workspace empty metadata fail-closed load

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-20260812-browser-workspace-empty-metadata`
- Registered: `2026-08-12T07:20:00+07:00`
- Baseline main SHA: `1ee9cd3d18c30a9549ee056e3ccff838bc4d8981`
- Priority: evidence-driven remote-safe persistence hardening found during owner-requested `continue all`

## Reserved scope

Harden `ProjectBrowserWorkspaceStateStore.Load(ProjectState)` so a persisted workspace metadata key is treated as present persisted state and must deserialize validly. A missing metadata key may still produce the default workspace state, but a present empty/whitespace value must fail closed instead of being silently interpreted as “no saved state”.

## Expected surfaces

- `src/QS3D.Core/Navigation/ProjectBrowserWorkspaceStateStore.cs` — `Load(ProjectState)` presence/corruption boundary only
- one focused CAD-independent Core smoke under `tests/QS3D.Core.SmokeTests/`
- module-initializer registration for that smoke if needed
- this claim file for completion close-out

## Excluded scope

- no changes to workspace enum/boolean/query/primary canonicality lanes already completed on current `main`;
- no changes to `ProjectBrowserWorkspaceStateStore.Save()` or `Clear()`; those methods are reserved by the concurrent `browser-workspace-revision-atomicity` claim at `3dc86e27db785071930110dbf710fe91554d8603`;
- no selection, query, virtualization, Workspace WPF/UI, BricsCAD V25/V26 adapter/runtime, QSDB project schema, release/package or workflow changes;
- no change to the semantics of a genuinely absent workspace metadata key.

## Validation plan

- absent `QS3D.ProjectBrowser.WorkspaceState` metadata returns the default workspace state without mutating project freshness;
- present empty-string metadata throws `InvalidDataException`;
- present whitespace-only metadata throws `InvalidDataException`;
- canonical serialized metadata still round-trips through `Load` unchanged;
- malformed-state load attempts do not mutate metadata, `UpdatedUtc`, or `ChangeVersion`;
- review exact branch diff against moving `main` before merge; do not claim executable smoke/build or BricsCAD runtime PASS unless actually run.

## Coordination

Recent workspace canonicality claims for enum, boolean, query, and primaryElementId are completed. A concurrent claim registered at `3dc86e27db785071930110dbf710fe91554d8603` owns only workspace `Save()/Clear()` project-revision atomicity and explicitly excludes workspace XML/canonicality. This claim owns only `Load()` presence-vs-corruption semantics and will not edit `Save()/Clear()`. The two lanes are independently verifiable despite sharing the same source file.

This lane also does not overlap active XLSX, release, Grid, source-reconcile, rebar, floor/foundation, health or formula-token claims observed on current `main`.

## Completion condition

Current `main` distinguishes absent workspace metadata from present invalid empty/whitespace metadata, with focused regression source and exact integration evidence, and this claim is marked `COMPLETED`.