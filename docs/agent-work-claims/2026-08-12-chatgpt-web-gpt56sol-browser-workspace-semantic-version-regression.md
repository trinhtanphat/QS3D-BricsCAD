# Work claim — Project Browser workspace semantic-version regression

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-browser-workspace-semantic-version-regression`
- Registered: `2026-08-12`
- Baseline main SHA: `09694cdd062fd96701483d40a0e02498311a8190`
- Priority: P0 — presentation-only workspace persistence is currently invalidating semantic preview freshness.

## Confirmed regression

Commit `c2eaf02d45cadeed85e3ffe6da148ec8d3043473` established the explicit contract that persisted Project Browser presentation state must not increment semantic `ProjectState.ChangeVersion`. Current `ProjectBrowserWorkspaceStateStore.Save(...)` and `Clear(...)` have regressed to calling `project.Touch()`, and current smoke coverage has likewise been rewritten to expect `ChangeVersion + 1`. Expanding/filtering/selecting in the modeless browser can therefore invalidate semantic previews even though no semantic project data changed.

## Reserved scope

- `src/QS3D.Core/Navigation/ProjectBrowserWorkspaceStateStore.cs`
- `tests/QS3D.Core.SmokeTests/ProjectBrowserWorkspaceStateStoreSmoke.cs`
- this claim file for close-out

## Contract

- Restore the completed presentation-state contract: changed workspace Save/Clear operations update only the workspace metadata entry and must not increment semantic `ChangeVersion`.
- Preserve changed/no-op return values, canonical XML/schema validation, project/tree/selection validation, and all current workspace hardening.
- Restore regression coverage proving Save, Clear and repeated no-op operations leave semantic `ChangeVersion` unchanged.
- Do not change ProjectState.Touch semantics, browser query/selection/runtime behavior, or BricsCAD integration.
- No GitHub Actions/build/release dispatch and no BricsCAD runtime PASS claim from this remote lane.

## Completion condition

The prior semantic-version isolation contract is restored on current `main`, focused smoke assertions match it, source/test are re-read after integration, and this claim is marked `COMPLETED` with exact PR/SHA evidence.
