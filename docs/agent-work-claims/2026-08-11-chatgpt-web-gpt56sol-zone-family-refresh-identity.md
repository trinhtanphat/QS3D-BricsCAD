# Work claim — Zone/Family refresh identity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-zone-family-refresh-identity`
- Registered: `2026-08-11T20:36:00+07:00`
- Baseline main SHA: `af0fc42ea0ee94ea67e5a0bcc4bde42760568e0a`
- Priority: fail closed modeless Zone/Family writes after project reload/replacement, even when semantic definition IDs are reused

## Confirmed defect

`MaterialCatalogWindow` already binds the canonical `ProjectState` reference on successful Refresh and requires `ReferenceEquals(currentProject, _boundProject)` before mutations. `ZoneManagerWindow` and `FamilyManagerWindow` currently re-resolve stale UI selections by Zone/Family ID only. If a project is reload/replaced and the replacement reuses a ZoneId/FamilyId, a still-open modeless window can therefore mutate the replacement project before the user Refreshes.

## Reserved scope

Harden Zone Manager and Family Manager mutation boundaries so a successful Refresh is the only operation that rebinds the window to a replacement canonical `ProjectState`; stale windows fail closed before Save/Delete/Activate/Assign/property mutations.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/ZoneManagerWindow.xaml.cs`
- `src/QS3D.BricsCAD.V25/UI/FamilyManagerWindow.xaml.cs`
- `src/QS3D.BricsCAD.V25/UI/FamilyManagerWindow.Active.cs`
- focused source/static regression gate under `scripts/` if no existing suitable gate covers this contract
- this claim file for close-out

## Intended contract

- Bind the exact canonical `ProjectState` reference only after a successful Zone/Family Refresh.
- Before every UI/editor-originated Zone/Family mutation, require the current existing project to be the exact bound instance.
- Assignment preview and mutation phases must remain on the same canonical project instance in addition to existing selection/project-ID checks.
- Re-resolve Zone/Family IDs inside the verified canonical project; never trust stale UI object references.
- Project reload/replacement must require Refresh even when ZoneId, FamilyId, or ProjectId values are reused.
- Preserve existing atomic rollback and post-commit UI-warning boundaries.

## Excluded scope

- No Floor/Level/vertical-placement changes (`LOCAL-003` owns that lane).
- No Material Catalog changes; it is the established reference pattern only.
- No schedule/revision/modeless viewer files reserved by the active viewer-identity claim.
- No Core mutation/schedule-reporting changes, Ribbon/Start Center/Create Similar/Quick Workflow changes, Documentation #77 edits, XData changes, local inbox edits, release, CI or GitHub Actions.
- No licensed BricsCAD V25 / Windows UI runtime PASS claim.

## Validation plan

- Re-fetch fresh `main` and all target files immediately before implementation.
- Add/extend a focused static regression gate requiring canonical reference binding, Refresh-only rebind, mutation guards, and assignment preview/mutation same-instance checks for both managers.
- Inspect the resulting diff and preserve all existing rollback/selection checks.
- No GitHub Actions/build/release dispatch.

## Coordination

The current Ribbon reconciliation claim owns only Ribbon bootstrap surfaces. `LOCAL-003` owns Level/Floor vertical placement. Current Core, Create Similar, Room, Start Center and schedule/revision viewer claims do not reserve the three Zone/Family files above.

## Completion condition

Zone/Family stale modeless writes fail closed after project replacement until Refresh rebinds the window, focused static regression coverage is merged, and this claim is marked `COMPLETED` without claiming native V25 execution.
