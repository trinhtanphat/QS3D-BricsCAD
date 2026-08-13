# Work claim — V25 Quantity Summary responsive footer

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-summary-responsive-footer-20260813`
- Registered: `2026-08-13T18:22:00+07:00`
- Baseline main SHA: `06e8080551f68b2ed698da5da94c2eb665d7f4ed`
- Priority: P1 user-visible V25 UI reliability. `QuantitySummaryWindow` still uses a footer `DockPanel` where `TotalsText` appears before a final right-docked long interaction/export hint. The totals text therefore has no explicit shrinkable column and the final hint can consume remaining width, producing width-dependent clipping/crowding in narrower BricsCAD-hosted layouts.

## Reserved scope

- `src/QS3D.BricsCAD.V25/UI/QuantitySummaryWindow.xaml`
- `scripts/preflight-quantity-summary-responsive-footer.py` (new focused source regression)
- this claim file

## Intended change

Replace only the Quantity Summary footer row with a named `Auto` / `*` / `Auto` grid: preserve the SuccessBrush indicator in column 0, place `TotalsText` in a shrinkable star column with ellipsis trimming, and pin the long `BÁM 3D...EXPORT XLSX` interaction hint right/no-wrap in column 2. Preserve the takeoff grid, follow/locate/export behavior, column visibility controls, bindings, and handlers.

## Excluded scope

- quantity calculation/export/follow-3D logic or code-behind
- DataGrid/list/theme/dark-selection behavior, header/body redesign
- other windows, V26, GitHub Actions, native runtime qualification

## Validation plan

- Add a focused offline XAML preflight requiring named `QuantitySummaryStatusGrid`, exact `Auto`/`*`/`Auto` widths, preserved indicator/totals/interaction hint, key takeoff handler sentinels, and rejection of the stale right-docked hint pattern.
- Re-fetch exact pushed XAML/regression and inspect production diff.
- Verify ancestry against moving `main` before closeout.
- Source/static validation only; no native BricsCAD V25 runtime PASS will be claimed.

## Coordination

Recent commit search found no Quantity Summary responsive-footer lane. Prior Quantity Summary dark-selection, Follow3D, and callback-containment work is completed and outside this XAML-only footer scope. Current V25 startup/runtime-diagnostics work is also disjoint.

## Completion condition

The narrow footer fix and focused regression are on current `main`, exact source/test and ancestry are verified, and this claim is marked `COMPLETED` with actual validation boundaries recorded.
