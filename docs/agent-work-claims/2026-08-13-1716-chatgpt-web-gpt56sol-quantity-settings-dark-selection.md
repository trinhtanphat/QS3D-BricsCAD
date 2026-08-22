# Work claim — V25 Quantity Settings dark selection

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-quantity-settings-dark-selection-20260813`
- Registered: `2026-08-13T17:16:00+07:00`
- Completed: `2026-08-13T17:19:00+07:00`
- Baseline main SHA: `25a19924f1bf73359953796e39efb7a0fdfd1f00`
- Priority: Continue the user-requested V25 dark-host audit. `QuantitySettingsWindow.xaml` contains stock-template collection surfaces: the intersection-rule `PrimaryCategoryList` / `ReferenceCategoryList` ListBoxes plus DataGrid tables. The window adds local DataGrid styling but does not own the stock row/item selection templates or shadow WPF active/inactive system highlight resources, so BricsCAD host colors can still leak into selected rows/items.

## Reserved scope

Make Quantity Settings collection selection host-independent by shadowing active/inactive WPF selection background/text resources at `QuantitySettingsWindow.Resources` and directly on the two named ListBoxes. Window-level resources cover the DataGrid containers, including the currently unnamed category/settings tables. Preserve all settings bindings, save/reset/template actions, intersection-rule editing and project/business semantics.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/QuantitySettingsWindow.DarkHostTheme.cs`
- `scripts/preflight-quantity-settings-dark-selection.py`
- read-only `src/QS3D.BricsCAD.V25/UI/QuantitySettingsWindow.xaml`
- read-only `src/QS3D.BricsCAD.V25/UI/Theme.xaml`

## Excluded scope

- quantity calculation/settings semantics, validation, save/reset/template behavior
- existing QuantitySettings partials and XAML layout
- shared `Theme.xaml` redesign, Workspace/RightPanel/other windows, V26
- release/installer work, GitHub Actions dispatch, native BricsCAD PASS claims without licensed runtime evidence

## Result

- Implementation: `70da37e3bb3a8153a63c7064a7419c18ed0ebdb9` (`fix(v25): keep Quantity Settings selection dark`).
  - Shadows active/inactive WPF selection background keys with QS3D `BgSelectedBrush`.
  - Shadows active/inactive WPF selection text keys with QS3D `TextBrush`.
  - Publishes each key at the window resource boundary, covering the stock DataGrid containers, and directly on `PrimaryCategoryList` / `ReferenceCategoryList` for already-realized ListBox items.
  - Leaves all settings, validation, persistence and rule-editing paths untouched.
- Regression: `7a11df1aabfac51e4e21c15db5530cea21e25771` (`test(ui): guard Quantity Settings dark selection`).

## Validation actually executed

- Re-fetched exact current-main implementation and regression; all four active/inactive `SystemColors` keys plus root and both ListBox resource pins are present.
- Current `QuantitySettingsWindow.xaml` still exposes `PrimaryCategoryList`, `ReferenceCategoryList`, `IntersectionCategorySelectionChanged`, `MissingCategoryRuleList`, `MissingCategoryRuleSelectionChanged`, and DataGrid surfaces; no handler/layout source was changed by this lane.
- Shared Theme still supplies the canonical selected brush and stock ListBox/DataGrid container contracts used by the guard.
- `python -m py_compile` for the focused preflight logic — PASS in an isolated connector-derived fixture.
- Focused preflight logic — `PASS: V25 Quantity Settings dark host-selection contract` in that fixture.
- `compare_commits(7a11df1aabfac51e4e21c15db5530cea21e25771, main)` returned `status=ahead`, `behind_by=0`, merge-base equal to the regression commit; the only newer changed file at that check was an unrelated MAP claim document.
- No GitHub Actions were dispatched. Native BricsCAD V25 visual/runtime qualification was not executed and is not claimed as PASS.

## Coordination

Workspace, RightPanel, Quantity Insight/Summary, Family Manager, and Zone/Floor dark-host lanes are completed. Concurrent drawing/Curtain/runtime/mapping work did not touch this scope.

## Completion condition

Satisfied for repository source/regression: the focused fix and regression are pushed to `main`, exact source/ancestry were verified, and native visual qualification remains pending a licensed runtime smoke.
