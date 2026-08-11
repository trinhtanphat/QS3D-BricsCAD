# Agent Work Claim — Curtain Wall exact project identity

- Agent: `chatgpt-web-gpt56sol-curtain-wall-project-identity-20260811-2317`
- Registered: `2026-08-11T23:17:00+07:00`
- Status: `ACTIVE`
- Baseline `main`: `8ae9a7296c718c300ebc4e87dc3271e3cab47e71`

## Evidence

`CurtainWallWindow` is already bound to the exact BricsCAD `Document`, but a successful Refresh does not bind the modeless window to the exact canonical `ProjectState`. `OnSaveClick` and `OnRecalculateClick` resolve the current project again at action time. If the same DWG reloads/replaces its QS3D project while the window remains open, stale controls can therefore target the replacement project.

## Reserved scope

- `src/QS3D.BricsCAD.V25/UI/CurtainWallWindow.xaml.cs`
- `scripts/preflight-curtain-wall-project-identity.py`
- this claim file

## Intended change

- Bind the window to the exact canonical `ProjectState` after a successful Refresh.
- Clear that binding whenever Refresh cannot resolve the project.
- Fail closed before Save/Recalculate when the canonical current project is not reference-equal to the project that populated the window.
- Preserve the existing document guard, Family/category revalidation, rollback, dirty/regeneration behavior, command dispatch, audit/UI sync behavior, and Refresh-as-explicit-rebind UX.
- Add a source/static regression preflight; do not dispatch GitHub Actions.

## Excluded scope

No changes to Core formulas/geometry/persistence, RightPanel/BQ, Ribbon, Start Center, Project Tools, Workspace, Theme, updater/release/signing, native BricsCAD runtime setup, or GitHub Actions.

## Validation boundary

Remote/source validation only. Native BricsCAD V25 modeless WPF behavior remains local-only and must not be reported as remotely passed.
