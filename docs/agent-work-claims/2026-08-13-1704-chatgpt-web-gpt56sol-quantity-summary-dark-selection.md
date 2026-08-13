# Work claim — V25 Quantity Summary dark selection

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-quantity-summary-dark-selection-20260813`
- Registered: `2026-08-13T17:04:00+07:00`
- Completed: `2026-08-13T17:08:00+07:00`
- Baseline main SHA: `c2e2be1e40db7a1b632c3aa7f4a6873ce1cee76a`
- Priority: Follow-up to the user-reported bright WPF/BricsCAD selection fallback. `QuantitySummaryWindow.xaml` has a `CategoryList` ListBox and `QuantityGrid` DataGrid while shared `Theme.xaml` uses stock WPF collection container templates whose active/inactive system selection resources can be supplied by the host.

## Reserved scope

Make `QuantitySummaryWindow` collection selection chrome host-independent by shadowing active and inactive WPF selection background/text resources at the window boundary and directly on `CategoryList` and `QuantityGrid`. Preserve all filter/grid handlers, Follow3D/locate behavior, quantity calculations, exports and CAD/project semantics.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/QuantitySummaryWindow.DarkHostTheme.cs`
- `scripts/preflight-quantity-summary-dark-selection.py`
- read-only: `src/QS3D.BricsCAD.V25/UI/QuantitySummaryWindow.xaml`
- read-only: `src/QS3D.BricsCAD.V25/UI/Theme.xaml`

## Excluded scope

- Quantity Summary calculation/view-model/locate/export behavior
- `QuantitySummaryWindow.CompactShell.cs` sizing/density behavior
- Quantity Insight, WorkspacePanel, RightPanel, shared Theme redesign, V26
- release/installer work, GitHub Actions dispatch, native BricsCAD PASS claims without licensed runtime evidence

## Result

- Implementation: `6e47e3ded0f399219a24950ff1323372c730534b` (`fix(v25): keep Quantity Summary selection dark`).
  - Shadows `SystemColors.HighlightBrushKey` and `InactiveSelectionHighlightBrushKey` with QS3D `BgSelectedBrush`.
  - Shadows `SystemColors.HighlightTextBrushKey` and `InactiveSelectionHighlightTextBrushKey` with QS3D `TextBrush`.
  - Publishes each resource at the `QuantitySummaryWindow` boundary and directly on `CategoryList` / `QuantityGrid` so existing and future collection containers resolve the local dark resources.
  - Contains no quantity/CAD/command mutation path; existing XAML handlers remain canonical.
- Regression: `bce67347e45576b49a816a00fb448fd30e72a5f1` (`test(ui): guard Quantity Summary dark selection`).

## Validation actually executed

- Re-fetched exact current-main implementation and regression after push; all four active/inactive selection keys, window/ListBox/DataGrid pins and presentation-only guard contract are present.
- Re-fetched current `QuantitySummaryWindow.xaml`; `CategoryList`, `OnCategoryChanged`, `QuantityGrid`, `OnQuantityGridSelectionChanged` and `OnQuantityGridDoubleClick` remain unchanged.
- Re-fetched current `Theme.xaml`; canonical `BgSelectedBrush`, `ListBoxItem`, `DataGridRow` and `DataGridCell` contracts remain present.
- `python -m py_compile` for the focused regression logic — PASS in an isolated connector-derived fixture.
- Focused preflight logic — `PASS: V25 Quantity Summary dark host-selection contract` in that isolated connector-derived fixture using the exact pushed guard contract and current XAML/Theme markers.
- `compare_commits(bce67347e45576b49a816a00fb448fd30e72a5f1, main)` returned `status=ahead`, `behind_by=0`, merge-base equal to the regression commit; the newer changed-file set did not touch this lane's source/test surfaces.
- No GitHub Actions were dispatched by this lane. Native BricsCAD V25 visual/runtime qualification was not executed and is not claimed as PASS.

## Coordination

The duplicate Quantity Insight claim from this session was released after discovering that lane was already completed. Existing Quantity Insight, Workspace and RightPanel dark-host lanes are completed and non-overlapping. Concurrent Curtain/runtime work did not modify this lane's files.

## Completion condition

Satisfied for repository source/regression: focused fix and regression are pushed to `main`, exact source/ancestry were verified, and native BricsCAD visual qualification remains explicitly pending a licensed runtime smoke.
