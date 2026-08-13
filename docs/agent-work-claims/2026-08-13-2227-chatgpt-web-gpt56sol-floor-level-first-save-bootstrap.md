# Work claim — V25 Floor / Level first-save project bootstrap

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-floor-level-first-save-bootstrap-20260813`
- Registered: `2026-08-13T22:27:00+07:00`
- Baseline main SHA: `ba2932267f1ca168cb9a043faa88b3b58ea49cc7`
- Priority: P1 user-visible bug. On a fresh/projectless drawing, `QS3DLEVELS` lets the user enter the first floor but `OnSaveFloorClick` immediately requires a project bound by the last Refresh, so the first explicit Save fails instead of creating the canonical QS3D project.

## Reserved scope

- `src/QS3D.BricsCAD.V25/UI/FloorLevelWindow.xaml.cs`
- `scripts/preflight-floor-level-stale-project-guard.py`
- `scripts/preflight-floor-level-first-save-bootstrap.py` (new focused regression)
- this claim file

## Intended change

Allow only the explicit **new-floor Save** path to bootstrap the canonical project when the latest successful Level Picker refresh observed no project. Validate floor name/elevation before bootstrap; require the bound drawing to still be active; re-check that no project has appeared since Refresh; then use the existing canonical `ProjectContextCoordinator.GetOrCreate` creation path. Existing-floor edits and every other mutation keep the exact refreshed-project/reference-equality fail-closed contract.

If the newly-created project cannot complete floor creation/audit, restore the project snapshot and forget the newly bootstrapped context so a failed first Save does not strand an empty replacement project. Read-only Refresh/inspection remain non-creating.

## Excluded scope

- `ExistingProjectMutationContext` semantics
- floor/level domain math, assignment, vertical placement or CAD movement
- unrelated project lifecycle/capture flows
- V26 and native BricsCAD runtime qualification
- GitHub Actions dispatch

## Validation plan

- Update the existing stale-project source guard so project creation is permitted only inside the explicit first-save bootstrap helper and rejected everywhere else in the modeless window.
- Add a focused source regression requiring: validation before bootstrap, no-project re-check, exact new-save-only bootstrap, rollback/Forget on failed bootstrap mutation, existing-edit path still using `RequireBoundProjectForMutation`, and read-only callbacks remaining non-creating.
- Re-fetch exact pushed source/guards and verify final ancestry against moving `main` before closeout.
- Source/static evidence only; real V25 UI/NETLOAD qualification remains LOCAL_ONLY.

## Coordination

Recent Floor/Level responsive-footer work is already completed and reserved only XAML, not code-behind. The older stale-project lane is completed. Current NETLOAD/model-health claims are distinct. This claim deliberately preserves their stale-project safety invariant while fixing the explicit first-save UX hole.

## Completion condition

The first-floor Save fix and focused regression are on current `main`, exact source/guards and ancestry are verified, and this claim is marked `COMPLETED` with the remaining LOCAL_ONLY validation boundary recorded.