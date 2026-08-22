# Work claim — V25 Door/Opening Schedule responsive footer

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-door-schedule-responsive-footer-20260813`
- Registered: `2026-08-13T17:51:00+07:00`
- Completed: `2026-08-13T17:54:00+07:00`
- Baseline main SHA: `b8262e08daf4e5b820dd25628bcf2349e9ff8159`
- Priority: user-visible V25 UI hardening. Source inspection confirmed `DoorOpeningScheduleWindow.xaml` footer used `StatusText` followed by a final `TextBlock DockPanel.Dock="Right"` under the default `DockPanel.LastChildFill=True`; the `READ-ONLY SCHEDULE • EXPORT XLSX` label could therefore fill the remaining row instead of occupying a bounded right edge.

## Reserved scope

Replace only the Door/Opening Schedule footer DockPanel with a deterministic responsive `Auto` + `*` + `Auto` grid. Preserve the success indicator, named `StatusText`, read-only/export wording, search/schedule grid bindings and refresh/export handlers.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/DoorOpeningScheduleWindow.xaml`
- `scripts/preflight-door-opening-schedule-responsive-footer.py`
- this claim file

## Excluded scope

- schedule aggregation/export logic, XLSX writer behavior, code-behind
- DataGrid schema/content, shared Theme, other windows
- project/Core/QSDB/release/V26/GitHub Actions/native runtime claims

## Result

- Implementation: `ab9fc66938164cf8134da4a6da1b6dbc839e4c8b` (`fix(ui): make Door Schedule footer responsive`).
  - Replaced only the footer DockPanel with named `DoorOpeningScheduleStatusGrid` using `Auto` + `*` + `Auto` columns.
  - Keeps the success indicator in column 0, named/wrapping `StatusText` shrinkable in column 1, and `READ-ONLY SCHEDULE • EXPORT XLSX` right-aligned/no-wrap in column 2.
  - Implementation commit diff confirms no schedule grid, search, metric, refresh/export or code-behind changes.
- Regression: `4d047d17caf2f9cc4f9ae4cf8e8aa114667707e4` (`test(ui): guard Door Schedule responsive footer`).
  - Parses XAML, validates the responsive footer contract, preserves search/refresh/export/read-only surfaces and metric controls, asserts the exact 12 current DataGrid header/binding pairs, and rejects the stale right-docked label.

## Validation actually executed

- Re-fetched current-main `DoorOpeningScheduleWindow.xaml`; `DoorOpeningScheduleStatusGrid`, `Auto` + `*` + `Auto` columns, indicator, `StatusText`, and read-only/export label are present with intended alignment/wrapping.
- Re-fetched the focused preflight from current `main` and reviewed its XML/schedule-schema continuity checks against the pushed XAML.
- Fetched implementation commit `ab9fc66938164cf8134da4a6da1b6dbc839e4c8b`; its diff is confined to the footer replacement.
- `compare_commits(cd4bb6aa833d18dd3fa3ea3ef14cb9176df40479, main)` reported the claim commit as merge base with `behind_by=0`. Intervening files were the expected Door/Opening Schedule XAML/preflight plus an unrelated concurrent Schedule Hub responsive-footer lane; no competing Door/Opening Schedule edit was present.
- A fresh commit search found only this lane's claim/implementation/regression for `Door Schedule responsive footer`.
- The Python preflight was not executed in a repository checkout from this connector environment, so no executable PASS is claimed. No GitHub Actions or licensed BricsCAD V25 visual/runtime smoke was run by this lane.

## Coordination

The concurrent Schedule Hub responsive-footer work is a different XAML/preflight surface and did not overlap this lane. Existing schedule/export/dark-selection work remained non-overlapping.

## Completion condition

Satisfied for repository source/regression: the narrow responsive-footer redesign and focused regression are on current `main`, exact source/test and implementation diff were read back, ancestry was checked, and native visual qualification remains explicitly unclaimed pending licensed local runtime evidence.