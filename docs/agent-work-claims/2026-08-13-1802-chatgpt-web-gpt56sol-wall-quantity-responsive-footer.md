# Work claim — V25 Wall Quantity responsive footer

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-wall-quantity-responsive-footer-20260813`
- Registered: `2026-08-13T18:02:00+07:00`
- Baseline main SHA: `6cbb918b1f8d9a8c69518d6938e0a7b593efaa2b`
- Priority: P1 user-visible V25 UI reliability. `WallQuantityWindow` footer gives totals an `Auto` column while the left status area is a horizontal `StackPanel` inside `*`. A horizontal StackPanel measures `StatusText` with unconstrained horizontal space, so its current `TextWrapping="Wrap"` cannot reliably make the status shrink/wrap as the totals consume width. This can make the footer unstable at narrow BricsCAD-hosted widths.

## Reserved scope

- `src/QS3D.BricsCAD.V25/UI/WallQuantityWindow.xaml`
- `scripts/preflight-wall-quantity-responsive-footer.py` (new focused source regression)
- this claim file

## Intended change

Replace only the left footer status `StackPanel` with a deterministic two-column grid (`Auto`, `*`) so the success indicator keeps fixed width and `StatusText` receives a constrained, shrinkable column. Preserve the existing outer footer `*`/`Auto` split, totals labels/bindings, DataGrid, commands, colors, and takeoff behavior.

## Excluded scope

- wall quantity calculation/filter/export/locate behavior or code-behind
- totals redesign or removal of footer metrics
- DataGrid/theme/dark-selection work
- other windows, V26, GitHub Actions, native runtime qualification

## Validation plan

- Add a focused offline XAML preflight requiring a named left status grid with `Auto`/`*`, `StatusText` in flexible column 1 with `MinWidth="0"` and wrapping, preserved totals names/units, and no horizontal StackPanel around the status text.
- Re-fetch exact pushed XAML/regression and inspect production diff.
- Verify ancestry against moving `main` before closeout.
- Source/static validation only; no native BricsCAD V25 runtime PASS will be claimed.

## Coordination

Recent commit search found no Wall Quantity responsive-footer lane. Current Room Finish Schedule/Rebar Schedule responsive work, NETLOAD, closed-polyline, and Curtain work are different scopes.

## Completion condition

The narrow footer fix and focused regression are on current `main`, exact source/test and ancestry are verified, and this claim is marked `COMPLETED` with actual validation boundaries recorded.
