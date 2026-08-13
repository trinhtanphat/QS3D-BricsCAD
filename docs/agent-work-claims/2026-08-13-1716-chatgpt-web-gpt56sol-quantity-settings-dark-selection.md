# Work claim — V25 Quantity Settings dark selection

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-settings-dark-selection-20260813`
- Registered: `2026-08-13T17:16:00+07:00`
- Baseline main SHA: `25a19924f1bf73359953796e39efb7a0fdfd1f00`
- Priority: Continue the user-requested V25 dark-host audit. `QuantitySettingsWindow.xaml` contains stock-template collection surfaces: the intersection-rule `PrimaryCategoryList` / `ReferenceCategoryList` ListBoxes plus DataGrid tables. The window adds local DataGrid styling but does not own the stock row/item selection templates or shadow WPF active/inactive system highlight resources, so BricsCAD host colors can still leak into selected rows/items.

## Reserved scope

Make Quantity Settings collection selection host-independent by shadowing active/inactive WPF selection background/text resources at `QuantitySettingsWindow.Resources` and directly on the two named ListBoxes. Window-level resources cover the DataGrid containers, including the currently unnamed category/settings tables. Preserve all settings bindings, save/reset/template actions, intersection-rule editing and project/business semantics.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/QuantitySettingsWindow.DarkHostTheme.cs` (new presentation-only partial)
- `scripts/preflight-quantity-settings-dark-selection.py` (new focused regression)
- read-only `src/QS3D.BricsCAD.V25/UI/QuantitySettingsWindow.xaml`
- read-only `src/QS3D.BricsCAD.V25/UI/Theme.xaml`

## Excluded scope

- quantity calculation/settings semantics, validation, save/reset/template behavior
- existing QuantitySettings partials and XAML layout
- shared `Theme.xaml` redesign, Workspace/RightPanel/other windows, V26
- release/installer work, GitHub Actions dispatch, native BricsCAD PASS claims without licensed runtime evidence

## Validation plan

- Require all four active/inactive `SystemColors` highlight background/text keys using QS3D `BgSelectedBrush` / `TextBrush`.
- Require root Resources plus direct `PrimaryCategoryList` and `ReferenceCategoryList` resource pins; verify current XAML still contains DataGrid surfaces inheriting the root resources.
- Preserve `IntersectionCategorySelectionChanged` and `MissingCategoryRuleSelectionChanged`; assert the partial contains no settings/project/CAD mutation path.
- Re-fetch current main after claim registration, then re-fetch exact pushed source/test and verify ancestry.

## Coordination

Workspace, RightPanel, Quantity Insight/Summary, Family Manager, and Zone/Floor dark-host lanes are completed. Recent active drawing/Curtain/runtime/mapping work is unrelated. No recent Quantity Settings dark-selection reservation was found in commit history.

## Completion condition

Focused fix + regression are pushed to current `main`, exact source/ancestry are verified, and this claim is marked `COMPLETED` with exact SHAs and only validation actually executed.
