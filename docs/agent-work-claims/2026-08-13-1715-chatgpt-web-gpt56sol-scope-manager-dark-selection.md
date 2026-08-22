# Work claim — V25 Zone/Floor manager dark selection

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-scope-manager-dark-selection-20260813`
- Registered: `2026-08-13T17:15:00+07:00`
- Completed: `2026-08-13T17:18:00+07:00`
- Baseline main SHA: `96c6c960e29a1720790988f46cf55ccaca359a7d`
- Priority: Direct follow-up to the user's Zone/Tầng white-selection report. The dedicated `ZoneManagerWindow` and `FloorLevelWindow` each contain a ListView using the shared stock `ListViewItem` template.

## Reserved scope

Keep the dedicated Zone and Floor manager ListView selections on QS3D dark active/inactive resources. Add presentation-only guards local to each window, with window + named ListView resource pins. Preserve CRUD, activation, assignment, level, inspection and project/CAD semantics.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/ZoneManagerWindow.DarkHostTheme.cs`
- `src/QS3D.BricsCAD.V25/UI/FloorLevelWindow.DarkHostTheme.cs`
- `scripts/preflight-scope-manager-dark-selection.py`
- read-only manager XAML and shared Theme contracts

## Excluded scope

- Zone/Floor business logic, CAD movement, semantic assignment behavior
- Workspace scope ComboBoxes, shared Theme redesign, V26
- release/installer work, GitHub Actions dispatch, native BricsCAD PASS claims without licensed runtime evidence

## Result

- Zone implementation: `c0c0cccb632989eecca2f01e1579496cf004d7ec` (`fix(v25): keep Zone Manager selection dark`).
- Floor implementation: `c4bb9af0d0bbf68f1340ec1180959f159722ec3f` (`fix(v25): keep Floor Manager selection dark`).
- Regression: `109395e0dbdec97728dfe0d122db282d4f96acdf` (`test(ui): guard Zone Floor dark selection`).
- Each guard shadows active/inactive selection background/text `SystemColors` keys with `BgSelectedBrush` / `TextBrush` at its window boundary and directly on `ZoneList` or `FloorList`; no semantic/CAD handlers are changed.

## Validation actually executed

- Re-fetched exact current-main Zone and Floor dark-host partials plus the focused regression; all four system selection keys and each local ListView resource boundary are present.
- Current `ZoneManagerWindow.xaml` retains `ZoneList` / `OnZoneSelectionChanged`; current `FloorLevelWindow.xaml` retains `FloorList` / `OnFloorSelectionChanged`.
- Shared Theme still exposes `BgSelectedBrush` and the `ListViewItem` contract.
- `python -m py_compile` for the focused preflight logic — PASS in an isolated connector-derived fixture.
- Focused preflight logic — `PASS: V25 Zone/Floor manager dark host-selection contract` in that fixture.
- `compare_commits(109395e0dbdec97728dfe0d122db282d4f96acdf, main)` returned `status=ahead`, `behind_by=0`, merge-base equal to the regression commit; newer changed files did not touch this lane.
- No GitHub Actions were dispatched. Native BricsCAD V25 visual/runtime qualification was not executed and is not claimed as PASS.

## Coordination

Family Manager and Quantity Summary dark-selection lanes are completed. Concurrent Curtain/runtime/MAP changes did not touch this scope.

## Completion condition

Satisfied for repository source/regression: both manager guards and regression are pushed to `main`, exact source/ancestry were verified, and native visual qualification remains pending a licensed runtime smoke.
