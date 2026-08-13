# Work claim — V25 Floor / Level responsive footer

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-floor-level-responsive-footer-20260813`
- Registered: `2026-08-13T18:14:00+07:00`
- Baseline main SHA: `91f44f9d1dbf99bf3b741b2e2e4ff35534d8fcab`
- Priority: P1 user-visible V25 UI reliability. `FloorLevelWindow` still uses a default footer `DockPanel` for the success indicator, wrapping `StatusText`, and final `NO CAD MOVE • STALE ON LEVEL CHANGE` lifecycle label. The status text has no explicit flexible column and the final child can consume remaining width at narrow hosted sizes.

## Reserved scope

- `src/QS3D.BricsCAD.V25/UI/FloorLevelWindow.xaml`
- `scripts/preflight-floor-level-responsive-footer.py` (new focused source regression)
- this claim file

## Intended change

Replace only the Floor/Level footer with a named `Auto` / `*` / `Auto` grid: preserve success indicator in column 0, keep `StatusText` wrapping/shrinkable in column 1, and pin the no-CAD-move/stale-on-level-change lifecycle label right/no-wrap in column 2. Preserve floor/level CRUD, assignment, elevation semantics, and handlers.

## Excluded scope

- floor/level domain logic, placement/regeneration/stale behavior, CAD movement, code-behind
- list/theme/dark-selection behavior, header/body redesign
- other windows, V26, GitHub Actions, native runtime qualification

## Validation plan

- Add a focused offline XAML preflight requiring named `FloorLevelStatusGrid`, exact `Auto`/`*`/`Auto` widths, preserved status/lifecycle label, key floor/level handler sentinels, and rejection of stale right-docked lifecycle label.
- Re-fetch exact pushed XAML/regression and inspect production diff.
- Verify ancestry against moving `main` before closeout.
- Source/static validation only; no native BricsCAD V25 runtime PASS will be claimed.

## Coordination

Recent commit search found no Floor/Level responsive-footer lane. Current Model Health/MeasurementTrace/Room Finish/NETLOAD/closed-polyline work and completed Zone/Material/Wall/Curtain lanes are distinct scopes.

## Completion condition

The narrow footer fix and focused regression are on current `main`, exact source/test and ancestry are verified, and this claim is marked `COMPLETED` with actual validation boundaries recorded.
