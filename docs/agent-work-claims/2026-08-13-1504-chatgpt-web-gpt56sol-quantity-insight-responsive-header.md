# Work claim — V25 Quantity Insight responsive header

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-insight-responsive-header-20260813`
- Registered: `2026-08-13T15:04:00+07:00`
- Baseline main SHA: `50c15ad0601da230ec5a73bd0db82f2c5031d59c`
- Priority: User-visible V25 UI redesign follow-up. Source inspection confirms the Quantity Insight palette still uses header `DockPanel`s whose final child is marked `DockPanel.Dock="Right"` while `LastChildFill` remains enabled. In WPF the final child fills instead of honoring its dock, making badge/read-only chrome width-dependent and brittle in the compact 260–330 DIP palette host.

## Reserved scope

Redesign only the Quantity Insight header/summary title rows into deterministic two-column responsive grids (`*` content + `Auto` badge/status), add safe text trimming/min-width behavior, and preserve all current commands, bindings, tree behavior and read-only quantity semantics.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.xaml`
- new `scripts/preflight-quantity-insight-responsive-header.py`

## Excluded scope

- `RightPanel.xaml` / `RightPanel.DarkHostTheme.cs` and the currently active RightPanel dark-host lane
- WorkspacePanel, shared `Theme.xaml`, compact-shell behavior, palette persistence
- quantity calculation/business logic, QSDB/project mutations, commands/handlers
- V26, installer/release work, GitHub Actions dispatch, native BricsCAD PASS claims without runtime evidence

## Validation plan

- Parse the XAML and require named `QuantityHeaderGrid` and `QuantitySummaryHeaderGrid` surfaces with exactly two columns (`*`, `Auto`).
- Require title stacks to remain shrinkable and text-trimmed while the count/read-only badge remains in the auto column.
- Preserve existing action handler tokens (`OnRefreshClick`, `OnRegenerateClick`, `OnOpenBqClick`, `OnLocateClick`) and the QuantityTree selection/double-click handlers.
- Re-fetch current `main` after registration, recheck overlapping claims, then re-fetch exact pushed source/test after implementation. No GitHub Actions dispatch and no native visual PASS claim without a licensed runtime smoke.

## Coordination

The active RightPanel dark-host claim explicitly excludes QuantityInsight. LOCAL-004 and current Core/local qualification lanes are unrelated. Prior Workspace UI claims are completed. This lane intentionally avoids all RightPanel files and all shared theme resources.

## Completion condition

Focused Quantity Insight responsive-header redesign + regression are pushed to current `main`, registration ancestry/source are verified, and this claim is marked `COMPLETED` with exact implementation/test SHAs and only validation actually executed.
