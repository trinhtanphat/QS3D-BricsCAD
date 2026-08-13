# Work claim — V25 Domain Hub responsive footer

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-domain-hub-responsive-footer-20260813`
- Registered: `2026-08-13T17:47:00+07:00`
- Baseline main SHA: `13f9424ae1bec4d436b5976aed44ac0b282c84e4`
- Priority: user-visible V25 UI hardening. The Domain Hub footer uses a left status StackPanel followed by a final `TextBlock DockPanel.Dock="Right"` while `DockPanel.LastChildFill` remains at its default. The runtime-gate label can therefore fill the remaining row instead of occupying a bounded right edge, making footer alignment width-dependent.

## Reserved scope

Replace only the Domain Hub footer DockPanel with a deterministic responsive grid: success indicator in an auto column, shrinkable/ellipsized `StatusText` in `*`, and the native-runtime gate label in a right-aligned auto column. Preserve every command Tag/handler and all release/runtime wording.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/DomainHubWindow.xaml`
- new `scripts/preflight-domain-hub-responsive-footer.py`
- this claim file

## Excluded scope

- Domain Hub command routing/code-behind and domain/rebar/quantity/review logic
- runtime/release gate semantics, Core/QSDB/project state
- shared Theme, other windows, V26/release/GitHub Actions/native runtime claims

## Validation plan

- Require named `DomainHubStatusGrid` with `Auto` + `*` + `Auto` columns.
- Preserve the success indicator, named `StatusText`, and `3D native cần runtime gate V25 thật trước release.` wording.
- Preserve all current Domain Hub command Tags and `OnCommandClick` wiring.
- Reject the stale final-child right-docked runtime-gate label.
- Re-fetch current `main` before source write and exact pushed source/regression after implementation; inspect intervening files for overlap.

## Coordination

Recent commit/code search found no Domain Hub responsive-footer lane. Current Source Reconcile/runtime diagnostics work is outside this XAML-only scope.

## Completion condition

The narrow responsive-footer redesign and focused regression are on current `main`, exact source/test are read back, ancestry is checked, and this claim is closed `COMPLETED` with only actually executed validation reported.