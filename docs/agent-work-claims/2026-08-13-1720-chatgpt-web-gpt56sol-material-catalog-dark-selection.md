# Work claim — V25 Material Catalog dark selection

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-material-catalog-dark-selection-20260813`
- Registered: `2026-08-13T17:20:00+07:00`
- Baseline main SHA: `8c1dc9b316ddaa19124aecd84d2a04495615f67e`
- Priority: Continue the user-requested V25 dark-host audit. `MaterialCatalogWindow.xaml` contains `MaterialList`, a stock-template ListView. Shared `Theme.xaml` sets dark ListViewItem selected values but does not own the stock item template, leaving WPF active/inactive system highlight resources available to the BricsCAD host.

## Reserved scope

Make Material Catalog list selection host-independent by shadowing active/inactive WPF selection background/text resources at the window and `MaterialList` resource boundaries. Preserve material CRUD, apply/export behavior, semantic assignment and project/CAD semantics.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/MaterialCatalogWindow.DarkHostTheme.cs` (new presentation-only partial)
- `scripts/preflight-material-catalog-dark-selection.py` (new focused regression)
- read-only Material Catalog XAML and shared Theme contracts

## Excluded scope

- material business logic, selection assignment, export, persistence
- shared Theme redesign, other windows, V26, release/installer work
- GitHub Actions dispatch and native BricsCAD PASS claims without licensed runtime evidence

## Validation plan

- Require all four active/inactive `SystemColors` selection keys.
- Require window and `MaterialList` resource pins.
- Preserve `OnMaterialSelectionChanged`; assert the presentation partial contains no material/project/CAD mutation path.
- Re-fetch current main after registration and exact pushed source/test after implementation; verify ancestry.

## Coordination

Quantity Settings and prior dark-host lanes are completed. Recent drawing/Curtain/mapping/runtime work is unrelated. No recent Material Catalog dark-selection claim was found.

## Completion condition

Focused fix + regression are pushed to current `main`, exact source/ancestry are verified, and this claim is marked `COMPLETED` with exact SHAs and only validation actually executed.
