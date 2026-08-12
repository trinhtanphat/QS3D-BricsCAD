# Work claim — Project Browser workspace semantic-version regression

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-browser-workspace-semantic-version-regression`
- Registered: `2026-08-12`
- Completed: `2026-08-12`
- Baseline main SHA: `09694cdd062fd96701483d40a0e02498311a8190`
- Priority: P0 — presentation-only workspace persistence was invalidating semantic preview freshness.
- Integration PR: `#752`
- Main integration commit: `1bac370a427741dd9d37081842b6c89d8d80f17d`

## Confirmed regression

Commit `c2eaf02d45cadeed85e3ffe6da148ec8d3043473` established the explicit contract that persisted Project Browser presentation state must not increment semantic `ProjectState.ChangeVersion`. The current store had regressed to calling `project.Touch()` from both `Save(...)` and `Clear(...)`, and its smoke was rewritten to expect the increment. Expanding/filtering/selecting in the modeless browser could therefore invalidate semantic previews even though no semantic project data changed.

## Implemented scope

- `src/QS3D.Core/Navigation/ProjectBrowserWorkspaceStateStore.cs`
- `tests/QS3D.Core.SmokeTests/ProjectBrowserWorkspaceStateStoreSmoke.cs`
- this claim file for close-out

## Completed contract

- Changed workspace Save now updates only `QS3D.ProjectBrowser.WorkspaceState`; it no longer calls `ProjectState.Touch()`.
- Clear now removes only that presentation metadata entry and does not advance semantic `ChangeVersion`.
- Changed/no-op return values, canonical XML/schema validation, project/tree/selection validation, and all current workspace hardening remain intact.
- Smoke coverage again proves Save, Clear and repeated no-op operations leave semantic `ChangeVersion` unchanged.
- `ProjectState.Touch`, browser query/selection/runtime behavior and BricsCAD integration were not changed.

## Validation evidence

- Claim registration: `e0b58cbe9e6969f320730a1370af08b66f0b0313`.
- Branch source fix: `9b194d6769bc8d5c1e673bce5d6cca876de84816`.
- Branch smoke update: `48ad6b1be625eadc932011351043b19a196e3f1d`.
- Branch was synchronized twice with moving `main` without force-push; final sync head: `43f04a4e59ef615e268f4e93bbc8e970b513e9ef`.
- PR `#752` squash-merged to `main` as `1bac370a427741dd9d37081842b6c89d8d80f17d`.
- Post-merge source readback confirms `Save(...)` assigns metadata directly and `Clear(...)` returns `project.Metadata.Remove(MetadataKey)` with no `project.Touch()` call.
- Post-merge smoke readback confirms `PresentationStateDoesNotInvalidateSemanticVersion`, round-trip Save, Clear and repeated no-op assertions all keep the semantic version unchanged.
- No GitHub Actions/build/release was dispatched and no executable .NET or BricsCAD V25/V26 runtime PASS is claimed from this remote session.

## Completion condition

`COMPLETED`: the prior semantic-version isolation contract is restored on current `main`, focused smoke assertions match it, source/test were re-read after integration, and exact PR/SHA evidence is recorded above.
