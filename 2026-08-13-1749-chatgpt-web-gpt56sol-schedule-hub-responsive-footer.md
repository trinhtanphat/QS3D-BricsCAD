# Work claim — V25 Schedule Hub responsive footer

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-schedule-hub-responsive-footer-20260813`
- Registered: `2026-08-13T17:49:00+07:00`
- Completed: `2026-08-13T17:55:00+07:00`
- Baseline main SHA: `b1475e5fe7bcb995bea7b468ec17b632da4ff69a`
- Priority: P1 user-visible V25 UI reliability. `ScheduleHubWindow` used a default footer `DockPanel` where the wrapping status text had no explicit flexible column and the final context-lock label could fill the remaining row, making narrow-host layout width-dependent.

## Reserved scope

- `src/QS3D.BricsCAD.V25/UI/ScheduleHubWindow.xaml`
- `scripts/preflight-schedule-hub-responsive-footer.py`
- this claim file

## Result

- Production fix: `90edc8659f6aa8a1a2bb744b690f8172153c58c7` (`fix(ui): make Schedule Hub footer responsive`).
- Focused regression: `455759887d9a34ac5f91a7aff3914abc47f2009c` (`test(ui): guard Schedule Hub responsive footer`).
- Footer now uses named `ScheduleHubStatusGrid` with `Auto` / `*` / `Auto` columns.
- Success indicator remains in column 0; `StatusText` remains wrapping but is explicitly shrinkable with `MinWidth="0"` in column 1; `SCHEDULE-SAFE • DWG CONTEXT LOCK` is right-aligned and `NoWrap` in column 2.
- All other Schedule Hub body/header content and command wiring were left unchanged; the production commit diff is footer-only.

## Validation actually executed

- Re-fetched the exact pushed XAML blob after the source commit and confirmed the named three-column footer, preserved status/context-lock wording, `MinWidth="0"`, wrapping status, and right-aligned no-wrap gate label.
- Re-fetched the exact pushed regression script after the test commit.
- `python -m py_compile` on the exact fetched regression text in an isolated fixture exited `0`; the hosted Python startup emitted an unrelated `artifact_tool` spreadsheet-warmup warning on stderr, but compilation itself succeeded.
- Focused positive fixture using the exact footer contract: PASS with `PASS: Schedule Hub footer uses deterministic auto/star/auto layout while preserving schedule/context-lock contracts.`
- Focused negative fixture with the required footer-grid name removed: expected FAIL with `missing responsive footer grid: ScheduleHubStatusGrid`.
- Fetched production commit `90edc...`; GitHub diff confirms only lines 196–215 of `ScheduleHubWindow.xaml` changed from `DockPanel` to the responsive footer grid.
- `compare_commits(455759887d9a34ac5f91a7aff3914abc47f2009c, 68bbfc36f3ce1f178db8b716454c1435a9027fe5)` returned `status=ahead`, `behind_by=0`, merge-base equal to the regression commit; the only newer file at that checkpoint was an unrelated closed-polyline claim.
- No GitHub Actions were dispatched. No full repository build or native BricsCAD V25 visual/runtime smoke was executed, so no native runtime PASS is claimed.

## Coordination

The pre-existing canonical Domain Hub responsive-footer claim was a different window/scope. A duplicate Domain Hub claim accidentally created during discovery was immediately marked `RELEASED` before any source/test change after ancestry exposed the older canonical claim. This Schedule Hub lane remained disjoint from Domain Hub, Source Reconcile, Curtain, runtime/NETLOAD, Project Tools, closed-polyline, and ribbon-startup work.

## Completion condition

Satisfied for this bounded source/static lane: fix + focused regression are on `main`, exact source/test and ancestry were verified, the claim is closed, and no unrelated production behavior was changed.
