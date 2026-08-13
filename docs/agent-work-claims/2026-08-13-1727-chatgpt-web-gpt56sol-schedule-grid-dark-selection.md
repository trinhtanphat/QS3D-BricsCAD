# Work claim — V25 schedule grid dark selection

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-schedule-grid-dark-selection-20260813`
- Registered: `2026-08-13T17:27:00+07:00`
- Completed: `2026-08-13T17:31:00+07:00`
- Baseline main SHA: `8171d325e693f09ca500026148347169927c4680`
- Priority: Continue the user-requested V25 dark-host audit on schedule tables. `DoorOpeningScheduleWindow`, `RoomFinishScheduleWindow`, and `RebarScheduleWindow` each expose a stock-template DataGrid (`ScheduleGrid`, `ScheduleGrid`, `Grid`). Shared dark DataGrid styles do not own the stock WPF row/cell templates, leaving active/inactive host `SystemColors` selection resources able to leak bright chrome.

## Reserved scope

Keep the Door/Opening, Room Finish, and Rebar/BBS schedule DataGrid selections on QS3D-owned dark active/inactive resources. Add one presentation-only guard per window with root + named DataGrid pins. Preserve filtering, refresh/export, locate/double-click and all schedule/quantity/rebar semantics.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/DoorOpeningScheduleWindow.DarkHostTheme.cs`
- `src/QS3D.BricsCAD.V25/UI/RoomFinishScheduleWindow.DarkHostTheme.cs`
- `src/QS3D.BricsCAD.V25/UI/RebarScheduleWindow.DarkHostTheme.cs`
- `scripts/preflight-schedule-grid-dark-selection.py`
- read-only schedule XAML and shared Theme contracts

## Excluded scope

- schedule calculation/grouping/export/locate logic
- shared Theme redesign, other windows, V26, release/installer work
- GitHub Actions dispatch and native BricsCAD PASS claims without licensed runtime evidence

## Result

- Door/Opening implementation: `8496c584b91452a58f9bdc3cbc49317cac43ccf3` (`fix(v25): keep Door Opening schedule selection dark`).
- Room Finish implementation: `fe71154b7a09c4c4075356c2e04eb82e019a9340` (`fix(v25): keep Room Finish schedule selection dark`).
- Rebar/BBS implementation: `f0c87805185283a186b6244e9fc3cbb4e7daaf57` (`fix(v25): keep Rebar schedule selection dark`).
- Regression: `5b92851248cea93ab3fb2753af4907e3cc03ad86` (`test(ui): guard schedule grid dark selection`).
- Each guard shadows all four active/inactive WPF selection background/text resources with QS3D `BgSelectedBrush` / `TextBrush` at the window boundary and directly on the schedule DataGrid; no schedule behavior path is changed.

## Validation actually executed

- Re-fetched the focused regression from current `main`; it requires all three guard files, all four active/inactive resource pins, root + named DataGrid boundaries, and the current schedule behavior tokens.
- Current schedule XAML contracts remain intact: Door/Opening and Room Finish retain `ScheduleGrid` plus search handling; Rebar retains `Grid` plus `OnGridDoubleClick`.
- Shared Theme retains canonical `BgSelectedBrush`, `DataGridRow`, and `DataGridCell` contracts.
- Focused regression logic — `PASS: V25 schedule DataGrid dark host-selection contract` in an isolated connector-derived fixture.
- `compare_commits(5b92851248cea93ab3fb2753af4907e3cc03ad86, main)` returned `identical` at validation time.
- No GitHub Actions were dispatched. Native BricsCAD V25 visual/runtime qualification was not executed and is not claimed as PASS.

## Coordination

Diagnostic grid and prior dark-host lanes are completed. Concurrent drawing/Curtain/runtime/mapping work did not touch this scope.

## Completion condition

Satisfied for repository source/regression: all three schedule guards and focused regression are pushed to `main`, exact source/ancestry were verified, and native visual qualification remains pending a licensed runtime smoke.
