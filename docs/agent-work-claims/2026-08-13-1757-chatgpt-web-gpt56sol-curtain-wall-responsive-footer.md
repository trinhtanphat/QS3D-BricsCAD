# Work claim — V25 Curtain Wall responsive footer

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-curtain-wall-responsive-footer-20260813`
- Registered: `2026-08-13T17:57:00+07:00`
- Baseline main SHA: `b23ed90c9cad8cd1db2f5056ec874197f26f8368`
- Priority: P1 user-visible V25 UI reliability. `CurtainWallWindow` still uses a default footer `DockPanel` for the warning indicator, wrapping `StatusText`, and final `CURVE FRAME = V25 GATE` label. The status text has no explicit flexible column and the final child can consume the remaining row, so narrow hosted widths can produce width-dependent clipping/wrapping.

## Reserved scope

- `src/QS3D.BricsCAD.V25/UI/CurtainWallWindow.xaml`
- `scripts/preflight-curtain-wall-responsive-footer.py` (new focused source regression)
- this claim file

## Intended change

Replace only the Curtain Wall footer row with a named `Auto` / `*` / `Auto` grid: preserve the warning indicator in column 0, keep wrapping `StatusText` shrinkable in column 1, and pin `CURVE FRAME = V25 GATE` right/no-wrap in column 2. Preserve all curtain inputs, workflow command Tags/handlers, metrics, and V25 gate wording.

## Excluded scope

- Curtain native/semantic undo, generated-frame ownership, geometry/business logic, code-behind
- header/body redesign, shared Theme, other windows
- V26, release policy, GitHub Actions dispatch, native runtime claims

## Validation plan

- Add a focused offline XAML preflight requiring named `CurtainWallStatusGrid`, exact `Auto`/`*`/`Auto` widths, warning/status/gate placement, preserved workflow sentinels, and rejection of the stale right-docked gate label.
- Re-fetch exact pushed XAML/regression and inspect production diff.
- Verify ancestry against moving `main` before closeout.
- Source/static validation only; no native V25 runtime PASS will be claimed.

## Coordination

Recent commit search found no Curtain Wall responsive-footer lane. The recent Curtain native-undo implementation was merged before this claim and concerns native/semantic state, not this XAML-only footer. Current NETLOAD, Room Finish Schedule responsive-footer, closed-polyline, and other active work is disjoint.

## Completion condition

The narrow footer fix and focused regression are on current `main`, exact source/ancestry are verified, and this claim is marked `COMPLETED` with actual validation boundaries recorded.
