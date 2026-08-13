# Work claim — V25 schedule grid dark selection

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-schedule-grid-dark-selection-20260813`
- Registered: `2026-08-13T17:27:00+07:00`
- Baseline main SHA: `8171d325e693f09ca500026148347169927c4680`
- Priority: Continue the user-requested V25 dark-host audit on schedule tables. `DoorOpeningScheduleWindow`, `RoomFinishScheduleWindow`, and `RebarScheduleWindow` each expose a stock-template DataGrid (`ScheduleGrid`, `ScheduleGrid`, `Grid`). Shared dark DataGrid styles do not own the stock WPF row/cell templates, leaving active/inactive host `SystemColors` selection resources able to leak bright chrome.

## Reserved scope

Keep the Door/Opening, Room Finish, and Rebar/BBS schedule DataGrid selections on QS3D-owned dark active/inactive resources. Add one presentation-only guard per window with root + named DataGrid pins. Preserve filtering, refresh/export, locate/double-click and all schedule/quantity/rebar semantics.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/DoorOpeningScheduleWindow.DarkHostTheme.cs` (new)
- `src/QS3D.BricsCAD.V25/UI/RoomFinishScheduleWindow.DarkHostTheme.cs` (new)
- `src/QS3D.BricsCAD.V25/UI/RebarScheduleWindow.DarkHostTheme.cs` (new)
- `scripts/preflight-schedule-grid-dark-selection.py` (new)
- read-only schedule XAML and shared Theme contracts

## Excluded scope

- schedule calculation/grouping/export/locate logic
- shared Theme redesign, other windows, V26, release/installer work
- GitHub Actions dispatch and native BricsCAD PASS claims without licensed runtime evidence

## Validation plan

- Require all four active/inactive WPF selection background/text keys in each guard.
- Require root + named DataGrid local pins.
- Preserve existing search/refresh/export/locate/double-click contracts; forbid project/CAD/command mutation paths in the presentation partials.
- Re-fetch exact pushed source/test and verify ancestry against advancing `main`.

## Coordination

Diagnostic grid and prior dark-host lanes are completed. Recent drawing/Curtain/runtime/mapping work is unrelated. No recent schedule dark-selection reservation was found.

## Completion condition

All three schedule guards + focused regression are pushed to current `main`, exact source/ancestry are verified, and this claim is marked `COMPLETED` with exact SHAs and validation actually executed.
