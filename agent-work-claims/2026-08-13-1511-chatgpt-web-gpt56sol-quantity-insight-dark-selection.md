# Work claim — V25 Quantity Insight dark selection

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-quantity-insight-dark-selection-20260813`
- Registered: `2026-08-13T15:11:00+07:00`
- Completed: `2026-08-13T15:14:00+07:00`
- Baseline main SHA: `50bfa41c56f446b12b870815f7c73bacf15ef544`
- Priority: User-visible dark-theme follow-up. `QuantityInsightPanel.xaml` merges the QS3D theme but its `QuantityTree` uses the stock WPF `TreeViewItem` template. The shared style sets selected/background values but does not replace that container template; Workspace already required active/inactive `SystemColors` shadowing for the same host-dependent mechanism. Quantity Insight had no equivalent host guard, so BricsCAD could still inject bright active/inactive tree selection chrome.

## Reserved scope

Add a presentation-only Quantity Insight host-theme guard that shadows active/inactive WPF selection background/text resources at both the panel and `QuantityTree` resource boundaries. Preserve quantity selection semantics, click/double-click locate behavior, bindings and the completed responsive header redesign.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.DarkHostTheme.cs`
- `scripts/preflight-quantity-insight-dark-selection.py`
- read-only references: `src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.xaml`, `src/QS3D.BricsCAD.V25/UI/Theme.xaml`

## Excluded scope

- edits to `QuantityInsightPanel.xaml`, shared `Theme.xaml`, Workspace/RightPanel host guards
- quantity calculation/business logic, tree handlers, CAD locate/selection semantics, project/QSDB state
- palette persistence, V26, installer/release, GitHub Actions dispatch or native BricsCAD PASS claims without runtime evidence

## Result

- Implementation: `c583a484399fa0c98f34bf6528950bd38eeecfaa` (`fix(v25): pin Quantity Insight dark selection chrome`).
  - Adds a presentation-only `QuantityInsightPanel.DarkHostTheme.cs` class-load guard.
  - Resolves canonical `BgSelectedBrush` / `TextBrush` and shadows active/inactive WPF `SystemColors` selection background/text keys.
  - Pins each key at both the panel `Resources` boundary and `QuantityTree.Resources`, covering existing stock containers and inherited descendants without changing selection/locate semantics.
- Regression: `957746ce4250f1433aa904d3ecd6635c18d01275` (`test(ui): guard Quantity Insight dark selection`).
  - Requires all four active/inactive selection keys, both resource boundaries, canonical theme resources, current responsive-header names, and existing tree selection/double-click handlers; rejects production/project/quantity mutation dependencies in the guard.

## Validation actually executed

- Re-fetched exact current-main `QuantityInsightPanel.DarkHostTheme.cs` and focused preflight; all four selection pins and both resource-boundary assignments are present.
- Re-checked the current Quantity Insight XAML/theme contracts during this lane: `QuantityTree`, selection/double-click handlers, the responsive header grids, `BgSelectedBrush`, `TextBrush`, and the stock-template TreeViewItem style contract remain present.
- Verified registration commit `358c602d1c9fb00997588526d62e8b019817e030` is an ancestor of current `main`; intervening commits were Core/smoke-only and did not overlap this UI partial/test lane.
- No GitHub Actions were dispatched by this lane. Native BricsCAD V25 visual/runtime qualification was not executed and is not claimed as PASS.

## Coordination

The Quantity Insight responsive-header claim is completed and excluded host-theme behavior. RightPanel and Workspace host-theme lanes are completed on separate partial classes. LOCAL-004 and current Core/local qualification lanes remain unrelated.

## Completion condition

Satisfied for repository source/regression: focused Quantity Insight dark-selection guard + regression are pushed to current `main`, exact source was re-fetched, and remaining native BricsCAD visual qualification is explicitly unclaimed pending a licensed local runtime smoke.
