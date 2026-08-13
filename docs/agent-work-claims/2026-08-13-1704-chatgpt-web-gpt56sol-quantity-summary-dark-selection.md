# Work claim — V25 Quantity Summary dark selection

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-summary-dark-selection-20260813`
- Registered: `2026-08-13T17:04:00+07:00`
- Baseline main SHA: `c2e2be1e40db7a1b632c3aa7f4a6873ce1cee76a`
- Priority: Follow-up to the user-reported bright WPF/BricsCAD selection fallback. Current `QuantitySummaryWindow.xaml` has a `CategoryList` ListBox and `QuantityGrid` DataGrid. Shared `Theme.xaml` sets dark selection properties but retains stock WPF ListBoxItem/DataGridRow/DataGridCell templates, leaving active/inactive system highlight resources available to the host.

## Reserved scope

Make `QuantitySummaryWindow` collection selection chrome host-independent by shadowing active and inactive WPF selection background/text resources at the window boundary and directly on `CategoryList` and `QuantityGrid`. Preserve all filter/grid handlers, Follow3D/locate behavior, quantity calculations, exports and CAD/project semantics.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/QuantitySummaryWindow.DarkHostTheme.cs` (new presentation-only partial)
- `scripts/preflight-quantity-summary-dark-selection.py` (new focused source regression)
- read-only: `src/QS3D.BricsCAD.V25/UI/QuantitySummaryWindow.xaml`
- read-only: `src/QS3D.BricsCAD.V25/UI/Theme.xaml`

## Excluded scope

- Quantity Summary calculation/view-model/locate/export behavior
- `QuantitySummaryWindow.CompactShell.cs` sizing/density behavior
- Quantity Insight, WorkspacePanel, RightPanel, shared Theme redesign, V26
- release/installer work, GitHub Actions dispatch, native BricsCAD PASS claims without licensed runtime evidence

## Validation plan

- Require all four active/inactive `SystemColors` highlight background/text keys to resolve to QS3D `BgSelectedBrush` / `TextBrush`.
- Require resource pins at `QuantitySummaryWindow.Resources`, `CategoryList.Resources`, and `QuantityGrid.Resources`.
- Regression must preserve `OnCategoryChanged`, `OnQuantityGridSelectionChanged`, and `OnQuantityGridDoubleClick`, and assert the new partial contains no CAD/project/command mutation paths.
- Re-fetch exact pushed source/test and verify ancestry against advancing `main`. No GitHub Actions dispatch.

## Coordination

The duplicate Quantity Insight claim from this session was released after discovering that lane was already completed. Existing Quantity Insight, Workspace and RightPanel dark-host lanes are completed and non-overlapping. Recent active Curtain/source-gap/runtime lanes do not own this Quantity Summary presentation surface.

## Completion condition

Focused fix and regression are pushed to current `main`, exact source/ancestry are verified, this claim is marked `COMPLETED` with exact SHAs and only validation actually executed is reported.
