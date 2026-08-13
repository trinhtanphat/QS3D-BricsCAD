# Work claim — V25 Recognition responsive footer

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-recognition-responsive-footer-20260813`
- Registered: `2026-08-13T18:18:00+07:00`
- Baseline main SHA: `bdbe4306a967ea85a2342cd68f03c1f54a617273`
- Priority: P1 user-visible V25 UI reliability. `RecognitionWindow` still uses a default footer `DockPanel` for the warning indicator, trimming `Status`, and final `LOW CONFIDENCE = REVIEW` review boundary. The status has no explicit flexible column and the final child can consume remaining width at narrow hosted sizes, making trimming width-dependent.

## Reserved scope

- `src/QS3D.BricsCAD.V25/UI/RecognitionWindow.xaml`
- `scripts/preflight-recognition-responsive-footer.py` (new focused source regression)
- this claim file

## Intended change

Replace only the Recognition footer with a named `Auto` / `*` / `Auto` grid: preserve warning indicator in column 0, put `Status` in a shrinkable star column with existing ellipsis trimming, and pin `LOW CONFIDENCE = REVIEW` right/no-wrap in column 2. Preserve recognition grid, accept/reject/review commands, confidence behavior, and handlers.

## Excluded scope

- recognition scoring/rules/apply behavior or code-behind
- grid/theme/dark-selection behavior, header/body redesign
- other windows, V26, GitHub Actions, native runtime qualification

## Validation plan

- Add a focused offline XAML preflight requiring named `RecognitionStatusGrid`, exact `Auto`/`*`/`Auto` widths, preserved warning/status/review boundary, recognition handler sentinels, and rejection of stale right-docked boundary label.
- Re-fetch exact pushed XAML/regression and inspect production diff.
- Verify ancestry against moving `main` before closeout.
- Source/static validation only; no native BricsCAD V25 runtime PASS will be claimed.

## Coordination

Recent commit search found no Recognition responsive-footer lane. Current NETLOAD/MeasurementTrace/Model Health and other active work plus completed Floor/Zone/Material/Wall/Curtain lanes are distinct scopes.

## Completion condition

The narrow footer fix and focused regression are on current `main`, exact source/test and ancestry are verified, and this claim is marked `COMPLETED` with actual validation boundaries recorded.
