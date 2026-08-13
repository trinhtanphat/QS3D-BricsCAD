# Work claim — V25 Door/Opening Schedule responsive footer

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-door-schedule-responsive-footer-20260813`
- Registered: `2026-08-13T17:51:00+07:00`
- Baseline main SHA: `b8262e08daf4e5b820dd25628bcf2349e9ff8159`
- Priority: user-visible V25 UI hardening. `DoorOpeningScheduleWindow.xaml` footer uses `StatusText` followed by a final `TextBlock DockPanel.Dock="Right"` under the default `DockPanel.LastChildFill=True`; the `READ-ONLY SCHEDULE • EXPORT XLSX` label can therefore fill the remaining row instead of occupying a bounded right edge.

## Reserved scope

Replace only the Door/Opening Schedule footer DockPanel with a deterministic responsive `Auto` + `*` + `Auto` grid. Preserve the success indicator, named `StatusText`, read-only/export wording, search/schedule grid bindings and refresh/export handlers.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/DoorOpeningScheduleWindow.xaml`
- new `scripts/preflight-door-opening-schedule-responsive-footer.py`
- this claim file

## Excluded scope

- schedule aggregation/export logic, XLSX writer behavior, code-behind
- DataGrid schema/content, shared Theme, other windows
- project/Core/QSDB/release/V26/GitHub Actions/native runtime claims

## Validation plan

- Require named `DoorOpeningScheduleStatusGrid` with `Auto` + `*` + `Auto` columns.
- Preserve indicator, `StatusText`, `READ-ONLY SCHEDULE • EXPORT XLSX`, search box, schedule grid, metric fields, refresh/export handlers and all existing DataGrid columns.
- Reject the stale final-child right-docked footer label.
- Re-fetch current `main` before source write and exact pushed XAML/regression after implementation; inspect intervening files for overlap.

## Coordination

Recent commit/code search found no Door/Opening Schedule responsive-footer lane. Existing schedule/export/dark-selection work is completed or non-overlapping.

## Completion condition

The narrow responsive-footer redesign and focused regression are on current `main`, exact source/test are read back, ancestry is checked, and this claim is closed `COMPLETED` with only actually executed validation reported.