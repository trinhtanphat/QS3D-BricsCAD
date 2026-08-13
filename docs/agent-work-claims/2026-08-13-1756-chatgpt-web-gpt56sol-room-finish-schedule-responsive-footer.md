# Work claim — V25 Room Finish Schedule responsive footer

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-room-finish-schedule-responsive-footer-20260813`
- Registered: `2026-08-13T17:56:00+07:00`
- Baseline main SHA: `b37961d94d75ef943c569f01713fba8045b0693f`
- Priority: user-visible V25 UI hardening. `RoomFinishScheduleWindow.xaml` footer uses `StatusText` followed by a final `TextBlock DockPanel.Dock="Right"` under default `DockPanel.LastChildFill=True`; the `ROOM FINISH SCHEDULE • EXPORT XLSX` label can therefore fill the remaining row instead of occupying a bounded right edge.

## Reserved scope

Replace only the Room Finish Schedule footer DockPanel with a deterministic responsive `Auto` + `*` + `Auto` grid. Preserve success indicator, named `StatusText`, room-finish/export wording, search/schedule bindings, metrics and refresh/export handlers.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/RoomFinishScheduleWindow.xaml`
- new `scripts/preflight-room-finish-schedule-responsive-footer.py`
- this claim file

## Excluded scope

- Room Finish aggregation/export logic and code-behind
- DataGrid schema/content, shared Theme, other windows
- project/Core/QSDB/release/V26/GitHub Actions/native runtime claims

## Validation plan

- Require named `RoomFinishScheduleStatusGrid` with `Auto` + `*` + `Auto` columns.
- Preserve indicator, `StatusText`, `ROOM FINISH SCHEDULE • EXPORT XLSX`, search box, metric controls, refresh/export handlers and the existing DataGrid schema/bindings.
- Reject the stale final-child right-docked footer label.
- Re-fetch current `main` before source write and exact pushed XAML/regression after implementation; inspect intervening files for overlap.

## Coordination

Recent commit/code search found no Room Finish Schedule responsive-footer lane. Concurrent Schedule Hub/Door Schedule/runtime work is on distinct surfaces.

## Completion condition

The narrow responsive-footer redesign and focused regression are on current `main`, exact source/test are read back, ancestry is checked, and this claim is closed `COMPLETED` with only actually executed validation reported.