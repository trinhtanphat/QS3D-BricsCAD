# Work claim — V25 Zone/Floor manager dark selection

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-scope-manager-dark-selection-20260813`
- Registered: `2026-08-13T17:15:00+07:00`
- Baseline main SHA: `96c6c960e29a1720790988f46cf55ccaca359a7d`
- Priority: Direct follow-up to the user's Zone/Tầng white-selection report. The dedicated `ZoneManagerWindow` and `FloorLevelWindow` each contain a ListView (`ZoneList`, `FloorList`) using the shared stock `ListViewItem` template; active/inactive WPF system highlight resources can therefore leak host-bright selection chrome in those scope-management windows.

## Reserved scope

Keep the dedicated Zone and Floor manager ListView selections on QS3D dark active/inactive resources. Add presentation-only guards local to each window, with window + named ListView resource pins. Preserve CRUD, activation, assignment, level, inspection and project/CAD semantics.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/ZoneManagerWindow.DarkHostTheme.cs` (new)
- `src/QS3D.BricsCAD.V25/UI/FloorLevelWindow.DarkHostTheme.cs` (new)
- `scripts/preflight-scope-manager-dark-selection.py` (new)
- read-only manager XAML and shared Theme contracts

## Excluded scope

- Zone/Floor business logic, CAD movement, semantic assignment behavior
- Workspace scope ComboBoxes (already completed), shared Theme redesign, V26
- release/installer work, GitHub Actions dispatch, native BricsCAD PASS claims without licensed runtime evidence

## Validation plan

- Require all four active/inactive `SystemColors` selection keys in each guard.
- Require each guard to pin its root Resources plus `ZoneList` or `FloorList` Resources.
- Preserve `OnZoneSelectionChanged` and `OnFloorSelectionChanged`; forbid CAD/project/command mutation paths in presentation partials.
- Re-fetch exact source/test and verify ancestry.

## Coordination

Family Manager and Quantity Summary dark-selection lanes are completed. Current Curtain/runtime/MAP lanes are unrelated. No recent Zone/Floor dark-selection reservation was found.

## Completion condition

Both focused manager guards + regression are pushed to `main`, exact source/ancestry are verified, and this claim is marked `COMPLETED` with exact SHAs and validation actually executed.
