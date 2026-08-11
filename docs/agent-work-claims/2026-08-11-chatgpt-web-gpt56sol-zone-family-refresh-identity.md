# Work claim — Zone/Family refresh identity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-zone-family-refresh-identity`
- Registered: `2026-08-11T20:36:00+07:00`
- Completed: `2026-08-11T20:51:00+07:00`
- Baseline main SHA: `af0fc42ea0ee94ea67e5a0bcc4bde42760568e0a`
- Priority: fail closed modeless Zone/Family writes after project reload/replacement, even when semantic definition IDs are reused

## Confirmed defect

`MaterialCatalogWindow` already binds the canonical `ProjectState` reference on successful Refresh and requires `ReferenceEquals(currentProject, _boundProject)` before mutations. `ZoneManagerWindow` and `FamilyManagerWindow` previously re-resolved stale UI selections by Zone/Family ID only. If a project was reload/replaced and the replacement reused a ZoneId/FamilyId, a still-open modeless window could therefore mutate the replacement project before the user refreshed the manager.

## Implemented contract

- Zone Manager now stores the exact canonical `ProjectState` instance accepted by a successful Refresh.
- Family Manager now stores the exact canonical `ProjectState` instance accepted by a successful Refresh.
- A Refresh that can no longer resolve a project clears the binding.
- `EnsureActive` now requires both the original active document and `ReferenceEquals(currentProject, _boundProject)` before mutation handlers proceed.
- Existing mutation binding, semantic ID re-resolution, assignment ProjectId checks, selection equality checks, rollback snapshots and post-commit UI warning boundaries were retained.
- `FamilyManagerWindow.Active.cs` already calls `EnsureActive` before its mutation binding, so the shared canonical-instance guard protects Family activation without a source edit to that partial file.
- Added `scripts/preflight-zone-family-refresh-identity.py` to lock Refresh binding/reset, mutation guard ordering and Family Active guard coverage.

## Merged commits

- `f0d2dfbb40885bca613d34ea2e6055b687302809` — `fix(zone): reject stale modeless project mutations`
- `55409c3dab4e0ce7c6304b09901952bdc9841e2a` — `fix(family): reject stale modeless project mutations`
- `90ae3e2a7df55758b215eeb38906746bf401dda1` — `test(ui): lock Zone Family refresh identity`

## Validation performed

- Refetched and inspected the committed Zone and Family source on live `main` after both source commits.
- Refetched and inspected the committed focused regression gate on live `main`.
- Confirmed the existing Family Active mutation path calls `EnsureActive` before `ExistingProjectMutationContext.Require`.
- No force-push was used; stale Git ref attempts were rejected as non-fast-forward and abandoned, then source changes were committed through current-branch Contents API without overwriting concurrent lanes.

## Validation not claimed

- Did not run BricsCAD V25 or Windows UI runtime in this remote session.
- Did not dispatch or claim GitHub Actions.
- Did not claim a local repository build or execution of the Python preflight because the session container could not resolve `github.com` for a checkout.

## Excluded scope preserved

- No Floor/Level/vertical-placement changes (`LOCAL-003` owns that lane).
- No Material Catalog changes.
- No schedule/revision/modeless viewer files reserved by the active viewer-identity claim.
- No Core mutation/schedule-reporting changes, Ribbon/Start Center/Create Similar/Quick Workflow changes, Documentation #77 edits, XData changes, local inbox edits, release, CI or GitHub Actions.

## Completion

Zone/Family stale modeless writes now fail closed after project replacement until Refresh rebinds the manager, and focused source/static regression coverage is merged. Native V25 runtime validation remains explicitly unclaimed.
