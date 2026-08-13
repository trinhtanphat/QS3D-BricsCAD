# Work claim — V25 Revision Review responsive subheader

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-revision-review-responsive-header-20260813`
- Registered: `2026-08-13T18:03:00+07:00`
- Baseline main SHA: `f8abf0a572b4aac5c2cfde2542ae335036a23cce`
- Priority: user-visible V25 UI hardening. In `RevisionWindow.xaml`, the `REVISION REVIEW` subheader uses a left title stack followed by a final `TextBlock DockPanel.Dock="Right"` under default `DockPanel.LastChildFill=True`; the `COMPARE • INSPECT • LOCATE` label can therefore fill the remaining row rather than occupying a bounded right edge. The footer DockPanel is intentionally correct because its final `Totals` child fills after a right-docked status pill and is excluded.

## Reserved scope

Replace only the `REVISION REVIEW` subheader DockPanel with a deterministic responsive `*` + `Auto` grid. Preserve the title/accent marker, `COMPARE • INSPECT • LOCATE` wording, quantity/semantic DataGrid schemas, locate/double-click handlers, read-only semantics, named controls, and the intentionally-correct footer DockPanel.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/RevisionWindow.xaml`
- new `scripts/preflight-revision-review-responsive-header.py`
- this claim file

## Excluded scope

- revision diff/calculation/code-behind/Core behavior
- footer layout, dark-host selection theme, shared Theme
- other windows, V26/release/GitHub Actions/native runtime claims

## Validation plan

- Require named `RevisionReviewHeaderGrid` with `*` + `Auto` columns and a right-aligned/no-wrap compare label.
- Preserve `Header`, `Tabs`, `Grid`, `SemanticGrid`, `Totals`, Locate/double-click handlers, read-only DataGrid contracts, and both exact current DataGrid schemas.
- Require the existing footer `DockPanel LastChildFill="True"` with final `Totals` to remain intact.
- Reject only the stale subheader final-child right docking.
- Re-fetch current `main` before source write and exact pushed XAML/regression after implementation; inspect intervening files for overlap.

## Coordination

Recent commit/code search found no Revision responsive lane. Current Wall Quantity and other UI work is on distinct surfaces.

## Completion condition

The narrow responsive subheader redesign and focused regression are on current `main`, exact source/test are read back, implementation diff/ancestry are checked, and this claim is closed `COMPLETED` with only actually executed validation reported.