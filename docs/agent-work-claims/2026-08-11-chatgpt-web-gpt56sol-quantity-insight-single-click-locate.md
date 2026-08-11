# Work claim — Quantity Insight single-click viewport locate

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-insight-single-click-locate`
- Registered: `2026-08-11T21:18:00+07:00`
- Baseline main SHA: `4f1a8d04d2f457fdff002809f876f3b424555e67`
- Priority: P1 — direct continuation of the owner requirement that clicking a quantity explanation should reveal the related object in the BricsCAD 3D view.

## Reserved scope

- Make a normal single selection/click on a leaf quantity explanation row in the docked `QuantityInsightPanel` run the existing fail-closed current-row validation and native CAD select/zoom path.
- Preserve explicit `Định vị` and double-click behavior for compatibility; selecting Floor/group nodes must never dispatch CAD locate.
- Prevent selection changes caused by refresh/rebind from locating stale or unvalidated rows.
- Update the visible hint/status wording so the interaction says single-click rather than requiring double-click.
- Add a focused static regression gate for the selection -> validated current row -> Handle -> native selection/zoom contract.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.xaml`
- `src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.xaml.cs`
- `scripts/preflight-quantity-insight-single-click-locate.py`
- `docs/LOCAL-AGENT-INBOX.md` only if the new interaction materially needs an explicit addition beyond the already-parked modeless selection/UI qualification scenario
- this claim file for close-out

## Excluded scope

- No edits to `PaletteCoordinator.cs` or `UserUiLayoutStore.cs`; the active Quantity Insight palette layout-persistence claim owns those files.
- No edits to `Theme.xaml` or Workspace layout; the active premium-theme/workspace-collision claim owns those surfaces.
- No quantity formulas, report arithmetic, Setup & Rules semantics, Wall Takeoff, Ribbon, updater/release, Direct Draw, or Core persistence/domain changes.
- No remote claim of native BricsCAD V25 mouse/focus/viewport PASS.

## Validation plan

- Re-fetch current `main` before source writes and preserve concurrent winners.
- Wire `SelectedItemChanged` only for `QuantityInsightItemViewModel` leaf rows, route through the existing `LocateSelected()`/`ResolveCurrentRow(...)` path, and keep cross-DWG/project/stale-row fail-closed guards ahead of Handle resolution and CAD selection.
- Add an auto-discovered preflight requiring the single-click handler, leaf-row guard, validated locate path, and no direct stale item-Handle resolution.
- Re-fetch final source and current `main`; do not dispatch GitHub Actions.

## Coordination

- The active quantity-palette-layout claim explicitly excludes `QuantityInsightPanel*`, so this interaction lane is non-overlapping.
- The completed Quantity Insight affinity/preview-regeneration contracts remain authoritative and must not be weakened.

## Completion condition

- Single-clicking a current leaf quantity explanation row reaches the existing validated CAD selection + `QS3DZOOMSELECTED` path; group/floor selection remains inert; regression guard is pushed; any materially changed LOCAL_ONLY interaction requirement is updated in the canonical inbox; this claim is marked `COMPLETED` with exact SHAs.
