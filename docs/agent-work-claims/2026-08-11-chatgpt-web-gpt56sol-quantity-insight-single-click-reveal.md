# Work claim — Quantity Insight single-click 3D reveal

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-insight-single-click-reveal`
- Registered: `2026-08-11T21:18:00+07:00`
- Baseline main SHA: `8bfb74a049083105d58d65ecdd9ef74739050fc4`
- Priority: P1

## Reserved scope

- Complete the screenshot/reference interaction where clicking a quantity explanation row reveals that row in the real BricsCAD viewport.
- Add an explicit user-facing auto-reveal toggle, enabled by default, so single selection of a leaf quantity row runs the same fail-closed current-row locate path already used by the `Định vị` button.
- Preserve double-click as a manual locate fallback when auto-reveal is disabled.

## Expected files

- `src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.xaml`
- `src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.xaml.cs`
- `scripts/preflight-quantity-insight-single-click-reveal.py`
- this claim file for close-out

## Excluded scope

- Quantity formulas/preview regeneration, palette layout persistence, Wall Takeoff viewport-locate lane, Ribbon/Start Center, quantity settings/rules, Core persistence/domain.
- No embedded/fake 3D viewport: reveal must continue selecting native CAD objects and use the existing `QS3DZOOMSELECTED` workflow.
- No native BricsCAD V25 click-through PASS claim from the remote connector environment.

## Functional contract

- `TreeView.SelectedItemChanged` handles leaf `QuantityInsightItemViewModel` rows only; floor/group selection must never send a locate command.
- When `AutoRevealCheck.IsChecked == true`, selecting a leaf calls the existing `LocateSelected()` path, which retains bound-document/project identity checks, detached preview row revalidation, current Handle resolution, native selection and zoom.
- When auto-reveal is off, single selection is passive and double-click remains the manual locate gesture.
- No duplicate locate on double-click while auto-reveal is enabled.
- Selection sync/highlighting from CAD remains read-only and must not trigger a semantic project mutation.

## Validation plan

- Re-fetch current XAML/code-behind before edits and preserve concurrent winners.
- Add focused static preflight for XAML wiring, leaf-only auto-reveal, manual fallback, no duplicate double-click path, and continued native current-row locate ordering.
- Re-fetch final source/ancestry; do not dispatch GitHub Actions.

## Completion condition

- A normal click on a quantity leaf row reveals the corresponding current semantic/CAD objects in BricsCAD by default, with a visible opt-out and fail-closed regression coverage.
