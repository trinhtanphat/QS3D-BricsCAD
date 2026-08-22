# Work claim — V25 Material Catalog dark selection

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-material-catalog-dark-selection-20260813`
- Registered: `2026-08-13T17:20:00+07:00`
- Completed: `2026-08-13T17:22:00+07:00`
- Baseline main SHA: `8c1dc9b316ddaa19124aecd84d2a04495615f67e`
- Priority: Continue the user-requested V25 dark-host audit. `MaterialCatalogWindow.xaml` contains `MaterialList`, a stock-template ListView. Shared `Theme.xaml` sets dark ListViewItem selected values but does not own the stock item template, leaving WPF active/inactive system highlight resources available to the BricsCAD host.

## Reserved scope

Make Material Catalog list selection host-independent by shadowing active/inactive WPF selection background/text resources at the window and `MaterialList` resource boundaries. Preserve material CRUD, apply/export behavior, semantic assignment and project/CAD semantics.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/MaterialCatalogWindow.DarkHostTheme.cs`
- `scripts/preflight-material-catalog-dark-selection.py`
- read-only Material Catalog XAML and shared Theme contracts

## Excluded scope

- material business logic, selection assignment, export, persistence
- shared Theme redesign, other windows, V26, release/installer work
- GitHub Actions dispatch and native BricsCAD PASS claims without licensed runtime evidence

## Result

- Implementation: `caf1612b284abfaf3948dcf87f126492f5be1433` (`fix(v25): keep Material Catalog selection dark`).
  - Shadows active/inactive WPF selection background resources with QS3D `BgSelectedBrush`.
  - Shadows active/inactive WPF selection text resources with QS3D `TextBrush`.
  - Publishes each key at `MaterialCatalogWindow.Resources` and directly on `MaterialList.Resources`.
  - Does not change material CRUD, semantic assignment, export or project/CAD paths.
- Regression: `b56458b49eab5e038ad10923bed8df77e682377e` (`test(ui): guard Material Catalog dark selection`).

## Validation actually executed

- Re-fetched exact current-main implementation; all four `SystemColors` selection keys and both root/ListView resource pins are present.
- Current `MaterialCatalogWindow.xaml` retains `MaterialList` and `OnMaterialSelectionChanged`; no XAML or behavior source was modified by this lane.
- Shared Theme retains canonical `BgSelectedBrush` and stock `ListViewItem` contract.
- Focused regression logic — `PASS: V25 Material Catalog dark host-selection contract` in an isolated connector-derived fixture.
- `compare_commits(b56458b49eab5e038ad10923bed8df77e682377e, main)` returned `status=ahead`, `behind_by=0`, merge-base equal to the regression commit; the only newer changed file at that check was an unrelated CI runtime-preflight claim.
- No GitHub Actions were dispatched. Native BricsCAD V25 visual/runtime qualification was not executed and is not claimed as PASS.

## Coordination

Quantity Settings and prior dark-host lanes are completed. Concurrent drawing/Curtain/mapping/runtime work did not touch this scope.

## Completion condition

Satisfied for repository source/regression: focused fix and regression are pushed to `main`, exact source/ancestry were verified, and native visual qualification remains pending a licensed runtime smoke.
