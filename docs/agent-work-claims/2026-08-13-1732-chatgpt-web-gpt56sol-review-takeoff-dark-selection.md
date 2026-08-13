# Work claim — V25 review/takeoff dark selection

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-review-takeoff-dark-selection-20260813`
- Registered: `2026-08-13T17:32:00+07:00`
- Completed: `2026-08-13T17:36:00+07:00`
- Baseline main SHA: `9d915770a3ecf3637f3b3523202973ec8de8acac`
- Priority: Continue the user-requested V25 dark-host audit on core review/takeoff windows. `RevisionWindow` contains two stock-template DataGrids (`Grid`, `SemanticGrid`), `RecognitionWindow` contains stock-template DataGrid `Grid`, and `WallQuantityWindow` contains stock-template `WallList` ListBox plus `TakeoffGrid` DataGrid. These surfaces can still resolve active/inactive WPF selection resources from the BricsCAD host.

## Reserved scope

Keep selection chrome in Revision Review, Recognition Review, and Wall Takeoff on QS3D-owned dark active/inactive resources. Add presentation-only guards local to each window, pinning root plus every named collection control. Preserve locate/double-click, auto-reveal, filtering, recognition apply, quantity calculations/export and all project/CAD semantics.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/RevisionWindow.DarkHostTheme.cs`
- `src/QS3D.BricsCAD.V25/UI/RecognitionWindow.DarkHostTheme.cs`
- `src/QS3D.BricsCAD.V25/UI/WallQuantityWindow.DarkHostTheme.cs`
- `scripts/preflight-review-takeoff-dark-selection.py`
- read-only corresponding XAML and shared Theme contracts

## Excluded scope

- revision diff/locate, recognition/apply, wall takeoff/locate/export business logic
- shared Theme redesign, other windows, V26, release/installer work
- GitHub Actions dispatch and native BricsCAD PASS claims without licensed runtime evidence

## Result

- Revision implementation: `3b4789f656b09fe69eec3b69305ff41ca9285075` (`fix(v25): keep Revision review selection dark`).
- Recognition implementation: `10ba78bd958573621eae7a1949f163170484f2e4` (`fix(v25): keep Recognition review selection dark`).
- Wall Takeoff implementation: `9506bad8edb361a764b0b950abe0a53945840cd3` (`fix(v25): keep Wall Takeoff selection dark`).
- Regression: `3bf2755b5d1f40396325bce2a62ab84091b2cf1b` (`test(ui): guard review takeoff dark selection`).
- Every guard shadows all four active/inactive WPF selection background/text resources using QS3D `BgSelectedBrush` / `TextBrush` at the window boundary and directly on each named ListBox/DataGrid; no review/takeoff behavior path is changed.

## Validation actually executed

- Re-fetched the focused regression from current `main`; it requires all three guard files, all four system-resource pins, every named collection boundary, current locate/selection/double-click contracts, and the shared Theme contracts.
- Current XAML behavior remains unchanged: Revision retains both double-click locate grids; Recognition retains its review grid/double-click path; Wall Takeoff retains `WallList`, `TakeoffGrid`, selection handlers and double-click locate.
- Focused regression logic — `PASS: V25 review/takeoff dark host-selection contract` in an isolated connector-derived fixture.
- `compare_commits(3bf2755b5d1f40396325bce2a62ab84091b2cf1b, main)` returned `identical` at validation time.
- No GitHub Actions were dispatched. Native BricsCAD V25 visual/runtime qualification was not executed and is not claimed as PASS.

## Coordination

Schedule/diagnostic/settings/material and earlier dark-host lanes are completed. Concurrent drawing/Curtain/runtime/mapping work did not touch this scope.

## Completion condition

Satisfied for repository source/regression: all three guards and focused regression are pushed to `main`, exact source/ancestry were verified, and native visual qualification remains pending a licensed runtime smoke.
