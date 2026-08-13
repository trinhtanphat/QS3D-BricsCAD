# Work claim — V25 Material Catalog responsive footer

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-material-catalog-responsive-footer-20260813`
- Registered: `2026-08-13T18:06:00+07:00`
- Baseline main SHA: `bd5a788a04a4d0d2bec81a6313a644f474893077`
- Priority: P1 user-visible V25 UI reliability. `MaterialCatalogWindow` still uses a default footer `DockPanel` for the success indicator, wrapping `StatusText`, and final `AMBIGUOUS HANDLE = FAIL CLOSED` contract label. The status text has no explicit flexible column and the final child can consume the remaining row at narrow host widths.

## Reserved scope

- `src/QS3D.BricsCAD.V25/UI/MaterialCatalogWindow.xaml`
- `scripts/preflight-material-catalog-responsive-footer.py` (new focused source regression)
- this claim file

## Intended change

Replace only the Material Catalog footer row with a named `Auto` / `*` / `Auto` grid: preserve success indicator in column 0, keep `StatusText` wrapping/shrinkable in column 1, and pin the fail-closed ambiguity label right/no-wrap in column 2. Preserve material list/editor/apply behavior, copy, bindings, and handlers.

## Excluded scope

- material semantics, inheritance, handle resolution, apply/save/delete code-behind
- list/theme/dark-selection behavior, header/body redesign
- other windows, V26, GitHub Actions, native runtime qualification

## Validation plan

- Add a focused offline XAML preflight requiring named `MaterialCatalogStatusGrid`, exact `Auto`/`*`/`Auto` widths, preserved status/fail-closed label, selected material/apply/delete handler sentinels, and rejection of stale right-docked contract label.
- Re-fetch exact pushed XAML/regression and inspect production diff.
- Verify ancestry against moving `main` before closeout.
- Source/static validation only; no native BricsCAD V25 runtime PASS will be claimed.

## Coordination

Recent commit search found no Material Catalog responsive-footer lane. Current Room Finish/Rebar Schedule responsive work, NETLOAD, closed-polyline, Wall Quantity, and Curtain work are distinct scopes.

## Completion condition

The narrow footer fix and focused regression are on current `main`, exact source/test and ancestry are verified, and this claim is marked `COMPLETED` with actual validation boundaries recorded.
