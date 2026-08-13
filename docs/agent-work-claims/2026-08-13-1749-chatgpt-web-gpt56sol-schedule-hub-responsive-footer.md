# Work claim — V25 Schedule Hub responsive footer

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-schedule-hub-responsive-footer-20260813`
- Registered: `2026-08-13T17:49:00+07:00`
- Baseline main SHA: `b1475e5fe7bcb995bea7b468ec17b632da4ff69a`
- Priority: P1 user-visible V25 UI reliability. `ScheduleHubWindow` still places the status indicator, wrapping status text, and right context-lock label in one default `DockPanel`. The final right label can fill the remaining row while the status text has no explicit flexible column, so narrow BricsCAD-host widths can produce unstable clipping/wrapping rather than deterministic status-first shrink behavior.

## Reserved scope

- `src/QS3D.BricsCAD.V25/UI/ScheduleHubWindow.xaml`
- `scripts/preflight-schedule-hub-responsive-footer.py` (new focused source regression)
- this claim file

## Intended change

Replace only the Schedule Hub footer docking row with a named three-column grid: success indicator in `Auto`, `StatusText` in shrinkable `*` with its existing wrapping semantics, and `SCHEDULE-SAFE • DWG CONTEXT LOCK` in a right-aligned `Auto` column. Preserve all command Tags/handlers, body copy, colors, and schedule/project behavior.

## Excluded scope

- Schedule Hub command routing/code-behind and schedule/DWG-table business logic
- header/body redesign, other windows, shared Theme
- V26, release policy, GitHub Actions dispatch, native BricsCAD runtime claims

## Validation plan

- Add a focused offline XAML preflight that parses `ScheduleHubWindow.xaml`, requires the `Auto`/`*`/`Auto` footer contract, and preserves the status/context-lock wording and command wiring surface.
- Reject the stale right-docked footer form.
- Re-fetch exact pushed XAML/regression and verify commit ancestry against moving `main`.
- Source/static validation only; no native V25 runtime PASS will be claimed.

## Coordination

Recent commit search found no Schedule Hub responsive-footer lane. The canonical active Domain Hub responsive-footer claim is explicitly different scope. Current Source Reconcile, Curtain, runtime/NETLOAD, Project Tools, closed-polyline, and ribbon-startup work is also outside this XAML-only lane.

## Completion condition

The narrow footer fix and focused regression are on current `main`, exact source/ancestry are verified, and this claim is marked `COMPLETED` with only actually executed validation reported.
