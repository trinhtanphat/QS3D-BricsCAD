# Work claim — V25 Quantity Insight dark selection

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-insight-dark-selection-20260813`
- Registered: `2026-08-13T16:59:00+07:00`
- Baseline main SHA: `3b3ad9e76789f9ab440c02fb4067cee7d5df333e`
- Priority: Follow-up to the screenshot-visible bright selection defect. Current `QuantityInsightPanel.xaml` still contains a `QuantityTree` that relies on the shared implicit `TreeViewItem` style without an owned container template. `Theme.xaml` sets dark `IsSelected` background/foreground values, but stock WPF `TreeViewItem` templates can still resolve active/inactive selection brushes from `SystemColors`, which is the same BricsCAD host fallback mechanism already fixed in Workspace and RightPanel.

## Reserved scope

Make `QuantityInsightPanel.QuantityTree` selection chrome host-independent by shadowing active and inactive WPF selection background/text resources at the panel and tree resource boundaries. Preserve item templates, quantity bindings, click/double-click locate behavior, CAD selection semantics and all quantity calculations.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.DarkHostTheme.cs` (new presentation-only partial)
- `scripts/preflight-quantity-insight-dark-selection.py` (new focused source regression)
- read-only contract references: `src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.xaml`, `src/QS3D.BricsCAD.V25/UI/Theme.xaml`

## Excluded scope

- responsive header layout (completed separately)
- Quantity Insight handlers/view models/business calculations
- WorkspacePanel, RightPanel, shared `Theme.xaml` redesign, V26
- installer/release work and native BricsCAD PASS claims without licensed runtime evidence

## Validation plan

- Require all four active/inactive `SystemColors` selection keys to be shadowed with QS3D `BgSelectedBrush` / `TextBrush`.
- Require both panel-level and `QuantityTree.Resources` pins so already-created and future tree containers resolve the dark resources locally.
- Regression must assert the partial remains presentation-only and preserve existing `QuantityTree` selection/double-click handler contracts.
- Re-fetch exact pushed source/test and verify ancestry. No GitHub Actions dispatch.

## Coordination

The earlier Quantity Insight responsive-header claim is `COMPLETED`; it touched only header XAML and excluded this selection-host lane. Workspace and RightPanel dark-host lanes are completed and serve only as precedent. Recent active Curtain/source-gap/runtime claims are unrelated to this presentation-only Quantity Insight tree selection scope.

## Completion condition

Focused fix + regression are pushed to current `main`, source/ancestry are verified, this claim is marked `COMPLETED` with exact SHAs and only actually executed validation is reported.
