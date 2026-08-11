# Agent Work Claim — Curtain Wall exact project identity

- Agent: `chatgpt-web-gpt56sol-curtain-wall-project-identity-20260811-2317`
- Registered: `2026-08-11T23:17:00+07:00`
- Completed: `2026-08-11T23:42:00+07:00`
- Status: `COMPLETED`
- Baseline `main`: `8ae9a7296c718c300ebc4e87dc3271e3cab47e71`
- Claim registration on `main`: `fbe27c119176cee8a190aa86706654e5b50a9b0d`
- Source implementation commit: `e606ac4730a3b00d99f5eb5087767503f372e77d`
- Initial regression preflight commit: `21bed99ab9430818bfc2409b89038454774ef464`
- Preflight compatibility hardening: `fe75db39bb243e361d9a344d433b85a31b5efdc6`
- Integration PR: `#533`
- Integrated `main`: `cbea736e5df40a3aa8d8e29fe959f79b45376aa0`

## Evidence

`CurtainWallWindow` was already bound to the exact BricsCAD `Document`, but a successful Refresh did not bind the modeless window to the exact canonical `ProjectState`. `OnSaveClick` and `OnRecalculateClick` resolved the current project again at action time. If the same DWG reloaded/replaced its QS3D project while the window remained open, stale controls could therefore target the replacement project.

## Delivered behavior

- Added an exact `ProjectState` binding owned by the modeless Curtain Wall Hub.
- `RefreshAll()` invalidates the previous binding first and establishes the new binding only after a successful canonical read-only refresh.
- Missing/failed project refresh and `ClearProjectView()` clear the binding.
- Save and Recalculate resolve the canonical existing mutation context, then fail closed unless that project is reference-equal to the project that populated the window.
- Parameterless summary refresh also rejects replacement-project data instead of silently reading a new project with stale controls.
- Refresh remains the explicit rebind operation after a same-DWG reload/replacement.
- Existing exact-document guard, Family/category revalidation, rollback snapshot/restore, dirty regeneration, command dispatch and post-commit palette/UI synchronization were preserved.
- Added `scripts/preflight-curtain-wall-project-identity.py`; its implementation avoids Python 3.10-only union/generic syntax and uses exact source markers for the checked method slices.
- No fake/parallel WPF viewport was introduced.

## Reserved scope

- `src/QS3D.BricsCAD.V25/UI/CurtainWallWindow.xaml.cs`
- `scripts/preflight-curtain-wall-project-identity.py`
- this claim file

## Excluded scope

No changes to Core formulas/geometry/persistence, RightPanel/BQ, Ribbon, Start Center, Project Tools, Workspace, Theme, updater/release/signing, native BricsCAD runtime setup, or GitHub Actions.

## Validation

- Source was re-fetched from `main` immediately before integration; the claimed Curtain Wall source blob was still unchanged by concurrent agents.
- PR #533 changed only the claimed source and focused regression preflight and was mergeable after reconciling the branch with the then-current `main`.
- Integration used squash merge onto `main` as `cbea736e5df40a3aa8d8e29fe959f79b45376aa0`; subsequent concurrent commits remained descendants of that integration.
- The focused preflight was source-reviewed in this connector session but was **not executed** because no local repository checkout/runtime was available here.
- GitHub Actions were **not dispatched**.
- Native BricsCAD V25 modeless WPF/runtime verification remains local-only and is not claimed as remotely passed.
