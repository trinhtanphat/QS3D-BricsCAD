# Work claim — V25 Rebar Schedule responsive header/footer

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-rebar-schedule-responsive-rows-20260813`
- Registered: `2026-08-13T17:59:00+07:00`
- Baseline main SHA: `556cab23fe3ee03265ef1617d77304418cf09195`
- Priority: user-visible V25 UI hardening. `RebarScheduleWindow.xaml` has two final-child right-docking rows under default `DockPanel.LastChildFill=True`: the header command group (`Locate` / `Xuất XLSX`) and footer provenance/export label. The final right-docked child can fill remaining width instead of occupying a bounded right edge, making header/footer behavior width-dependent.

## Reserved scope

Replace only the Rebar Schedule header and footer DockPanels with deterministic responsive grids. Header uses shrinkable `*` title/description plus `Auto` command group; footer uses indicator + shrinkable `*` totals + right-aligned `Auto` provenance/export label. Preserve all DataGrid columns/bindings, locate/double-click/export handlers, totals binding/name and provenance wording.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/RebarScheduleWindow.xaml`
- new `scripts/preflight-rebar-schedule-responsive-rows.py`
- this claim file

## Excluded scope

- BBS calculation/fabrication/export/locate code-behind or Core logic
- DataGrid schema/content, dark-host selection theme, shared Theme
- other windows, V26/release/GitHub Actions/native runtime claims

## Validation plan

- Require named `RebarScheduleHeaderGrid` with `*` + `Auto` columns, shrinkable title content and bounded right command group.
- Require named `RebarScheduleStatusGrid` with `Auto` + `*` + `Auto`, preserving indicator, named `Totals`, provenance warning and export wording.
- Preserve Locate/export/double-click handlers and exact current 15 DataGrid header/binding pairs.
- Reject both stale final-child right-docked patterns.
- Re-fetch current `main` before source write and exact pushed XAML/regression after implementation; inspect intervening files for overlap.

## Coordination

Recent commit/code search found no Rebar Schedule responsive lane. Existing BBS/dark-selection work is completed or non-overlapping.

## Completion condition

The narrow responsive redesign and focused regression are on current `main`, exact source/test are read back, ancestry is checked, and this claim is closed `COMPLETED` with only actually executed validation reported.