# Work claim — V25 Quantity Insight dark selection

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-insight-dark-selection-20260813`
- Registered: `2026-08-13T15:11:00+07:00`
- Baseline main SHA: `50bfa41c56f446b12b870815f7c73bacf15ef544`
- Priority: User-visible dark-theme follow-up. `QuantityInsightPanel.xaml` merges the QS3D theme but its `QuantityTree` still uses the stock WPF `TreeViewItem` template. The shared style sets selected/background values but does not replace that container template; Workspace already required active/inactive `SystemColors` shadowing for the same host-dependent mechanism. Quantity Insight currently has no equivalent host guard, so BricsCAD can still inject bright active/inactive tree selection chrome.

## Reserved scope

Add a presentation-only Quantity Insight host-theme guard that shadows active/inactive WPF selection background/text resources at both the panel and `QuantityTree` resource boundaries. Preserve quantity selection semantics, click/double-click locate behavior, bindings and the just-completed responsive header redesign.

## Expected surfaces

- new `src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.DarkHostTheme.cs`
- new `scripts/preflight-quantity-insight-dark-selection.py`
- read-only references: `src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.xaml`, `src/QS3D.BricsCAD.V25/UI/Theme.xaml`

## Excluded scope

- edits to `QuantityInsightPanel.xaml`, shared `Theme.xaml`, Workspace/RightPanel host guards
- quantity calculation/business logic, tree handlers, CAD locate/selection semantics, project/QSDB state
- palette persistence, V26, installer/release, GitHub Actions dispatch or native BricsCAD PASS claims without runtime evidence

## Validation plan

- Require all four active/inactive `SystemColors` selection brush keys to be pinned through a bounded Quantity Insight guard.
- Require both `Resources[key]` and `QuantityTree.Resources[key]` assignment so existing containers and future descendants remain dark-host independent.
- Verify canonical `BgSelectedBrush` / `TextBrush`, `QuantityTree`, and existing selection/double-click handlers remain present.
- Re-fetch current `main` after registration for overlap, then exact pushed source/test after implementation. No Actions dispatch.

## Coordination

The Quantity Insight responsive-header claim is completed and excluded host-theme behavior. RightPanel and Workspace host-theme lanes are completed on separate partial classes. LOCAL-004 and current Core/local qualification lanes are unrelated.

## Completion condition

Focused Quantity Insight dark-selection guard + regression are pushed to current `main`, exact source/claim ancestry are verified, and this claim is marked `COMPLETED` with only validation actually executed.
