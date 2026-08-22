# Work claim — V25 review/takeoff dark selection

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-review-takeoff-dark-selection-20260813`
- Registered: `2026-08-13T17:32:00+07:00`
- Baseline main SHA: `9d915770a3ecf3637f3b3523202973ec8de8acac`
- Priority: Continue the user-requested V25 dark-host audit on core review/takeoff windows. `RevisionWindow` contains two stock-template DataGrids (`Grid`, `SemanticGrid`), `RecognitionWindow` contains stock-template DataGrid `Grid`, and `WallQuantityWindow` contains stock-template `WallList` ListBox plus `TakeoffGrid` DataGrid. These surfaces can still resolve active/inactive WPF selection resources from the BricsCAD host.

## Reserved scope

Keep selection chrome in Revision Review, Recognition Review, and Wall Takeoff on QS3D-owned dark active/inactive resources. Add presentation-only guards local to each window, pinning root plus every named collection control. Preserve locate/double-click, auto-reveal, filtering, recognition apply, quantity calculations/export and all project/CAD semantics.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/RevisionWindow.DarkHostTheme.cs` (new)
- `src/QS3D.BricsCAD.V25/UI/RecognitionWindow.DarkHostTheme.cs` (new)
- `src/QS3D.BricsCAD.V25/UI/WallQuantityWindow.DarkHostTheme.cs` (new)
- `scripts/preflight-review-takeoff-dark-selection.py` (new)
- read-only corresponding XAML and shared Theme contracts

## Excluded scope

- revision diff/locate, recognition/apply, wall takeoff/locate/export business logic
- shared Theme redesign, other windows, V26, release/installer work
- GitHub Actions dispatch and native BricsCAD PASS claims without licensed runtime evidence

## Validation plan

- Require all four active/inactive WPF selection background/text keys in every guard.
- Require root plus `Grid`/`SemanticGrid`, Recognition `Grid`, and Wall `WallList`/`TakeoffGrid` resource pins.
- Preserve the existing selection/double-click behavior tokens; forbid command/CAD/project mutation paths in the presentation partials.
- Re-fetch exact pushed source/test and verify ancestry against current main.

## Coordination

Schedule/diagnostic/settings/material and earlier dark-host lanes are completed. Recent drawing/Curtain/runtime/mapping work is unrelated. No recent dark-selection claims were found for these three windows.

## Completion condition

All three guards + focused regression are pushed to current `main`, exact source/ancestry are verified, and this claim is marked `COMPLETED` with exact SHAs and validation actually executed.
