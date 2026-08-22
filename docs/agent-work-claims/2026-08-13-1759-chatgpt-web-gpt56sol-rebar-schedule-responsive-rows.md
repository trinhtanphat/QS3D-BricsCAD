# Work claim — V25 Rebar Schedule responsive header/footer

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-rebar-schedule-responsive-rows-20260813`
- Registered: `2026-08-13T17:59:00+07:00`
- Completed: `2026-08-13T18:02:00+07:00`
- Baseline main SHA: `556cab23fe3ee03265ef1617d77304418cf09195`
- Priority: user-visible V25 UI hardening. Source inspection confirmed `RebarScheduleWindow.xaml` had two final-child right-docking rows under default `DockPanel.LastChildFill=True`: the header command group (`Locate` / `Xuất XLSX`) and footer provenance/export label. The final right-docked child could fill remaining width instead of occupying a bounded right edge, making header/footer behavior width-dependent.

## Reserved scope

Replace only the Rebar Schedule header and footer DockPanels with deterministic responsive grids. Header uses shrinkable `*` title/description plus `Auto` command group; footer uses indicator + shrinkable `*` totals + right-aligned `Auto` provenance/export label. Preserve all DataGrid columns/bindings, locate/double-click/export handlers, totals binding/name and provenance wording.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/RebarScheduleWindow.xaml`
- `scripts/preflight-rebar-schedule-responsive-rows.py`
- this claim file

## Excluded scope

- BBS calculation/fabrication/export/locate code-behind or Core logic
- DataGrid schema/content, dark-host selection theme, shared Theme
- other windows, V26/release/GitHub Actions/native runtime claims

## Result

- Implementation: `bb9d0cf458c40db54e3cc5bec5f45e33d6eb35b0` (`fix(ui): make Rebar Schedule header and footer responsive`).
  - Replaced the header DockPanel with named `RebarScheduleHeaderGrid` using `*` + `Auto`; title/description content is shrinkable and the Locate/XLSX command group is bounded/right-aligned.
  - Replaced the footer DockPanel with named `RebarScheduleStatusGrid` using `Auto` + `*` + `Auto`; indicator stays in column 0, named `Totals` is shrinkable/ellipsized in column 1, and the provenance/locate/export label is right-aligned/no-wrap in column 2.
  - Implementation diff confirms the DataGrid schema and code-behind surfaces were not modified.
- Regression: `15346a78d00ccd7e654c9fe7f71c5419c0c9cd2c` (`test(ui): guard Rebar Schedule responsive rows`).
  - Parses XAML, validates both responsive grid contracts, verifies Locate/export header wiring, preserves double-click/read-only review behavior, asserts the exact 15 current DataGrid header/binding pairs, and rejects both stale right-docked patterns.

## Validation actually executed

- Re-fetched current-main `RebarScheduleWindow.xaml`; `RebarScheduleHeaderGrid` and `RebarScheduleStatusGrid` are present with intended column contracts, command group, `Totals`, provenance label and exact current DataGrid schema visible.
- Re-fetched the focused preflight from current `main` and reviewed its XML/schema/handler continuity checks against the pushed XAML.
- Fetched implementation commit `bb9d0cf458c40db54e3cc5bec5f45e33d6eb35b0`; its diff is confined to the header/footer presentation rows.
- `compare_commits(76e14dc337430bc01a40c4c238b420e87cb4e479, main)` reported the claim commit as merge base with `behind_by=0`; the only unrelated intervening file was the concurrent Curtain Wall responsive-footer claim completion.
- Fresh commit search found only this lane's claim/implementation for `Rebar Schedule responsive` at validation time.
- The Python preflight was not executed in a repository checkout from this connector environment, so no executable PASS is claimed. No GitHub Actions or licensed BricsCAD V25 visual/runtime smoke was run by this lane.

## Coordination

Concurrent Curtain Wall responsive-footer work was a distinct XAML surface and did not overlap this lane. Existing BBS/dark-selection work remained non-overlapping.

## Completion condition

Satisfied for repository source/regression: the narrow responsive redesign and focused regression are on current `main`, exact source/test and implementation diff were read back, ancestry was checked, and native visual qualification remains explicitly unclaimed pending licensed local runtime evidence.