# Work claim — Project Browser workspace selection freshness

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-browser-workspace-selection-freshness-20260812-1238`
- Registered: `2026-08-12T12:38:00+07:00`
- Baseline main SHA: `7b0ac3733a216729a6fa3c9a3bc237321d7c85e2`
- Priority: P1 — workspace selection updates must not return presentation state derived from a stale Project Browser query after caller-controlled enumeration mutates project semantics/structure.
- Task Key: `CORE-BROWSER-WORKSPACE-SELECTION-FRESHNESS`

## Confirmed defect

`ProjectBrowserWorkspaceCoordinator.ApplySelection(...)` builds a `ProjectBrowserQueryResult` from the current project and then passes caller-provided `selectedElementIds` to `ProjectBrowserSelectionPlanner.PlanReveal(...)`. A lazy selection enumerable can mutate the project while it is being enumerated. The coordinator currently performs no post-enumeration freshness check, so it can return a workspace state whose selected IDs/expansion paths were resolved against a pre-mutation query snapshot while the live project has already changed. Direct edits to the public `project.Elements` list can also remove/reorder/replace semantic instances without advancing `ChangeVersion`.

## Reserved scope

- `src/QS3D.Core/Navigation/ProjectBrowserWorkspaceCoordinator.cs`
- `tests/QS3D.Core.SmokeTests/ProjectBrowserWorkspaceSelectionFreshnessSmoke.cs`
- this claim file

## Intended contract

- Snapshot `ProjectState.ChangeVersion` and exact ordered element references before building the query / enumerating caller selections.
- After `PlanReveal(...)` has materialized caller selection IDs, reject ordinary semantic mutation through `ChangeVersion` first.
- Also reject add/remove/reorder/same-ID element replacement when `ChangeVersion` is unchanged.
- Preserve query/grouping/filter semantics, selection canonicality/bounds, reveal/expansion behavior, presentation-only version isolation and all non-selection coordinator methods.
- Do not change `ProjectBrowserSelectionPlanner`, query/virtualization planners, workspace persistence, `ProjectState` collections or UI/native code.

## Validation plan

Add focused auto-registered Core smoke coverage where a lazy selected-ID source (1) calls `project.Touch()` and (2) replaces selected `B-001` with a same-ID element without `Touch()`. Both must fail before returning workspace state. Include a stable selection control proving selection remains presentation-only and returns the canonical selected ID.

## Validation boundary

No GitHub Actions will be dispatched. No local .NET/full executable smoke or licensed BricsCAD V25/V26 runtime PASS will be claimed unless actually executed.
