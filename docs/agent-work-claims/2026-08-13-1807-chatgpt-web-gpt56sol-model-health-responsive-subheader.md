# Work claim — V25 Model Health responsive issue-list subheader

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-model-health-responsive-subheader-20260813`
- Registered: `2026-08-13T18:07:00+07:00`
- Baseline main SHA: `e6cb6f438787ea4fb7130c78deb71e95d788d762`
- Priority: user-visible V25 UI hardening. In `ModelHealthWindow.xaml`, the `DANH SÁCH VẤN ĐỀ` subheader uses a left title followed by a final `TextBlock DockPanel.Dock="Right"` under default `DockPanel.LastChildFill=True`; the `DOUBLE-CLICK → CAD LOCATE` label can therefore fill remaining width rather than occupying a bounded right edge. The footer DockPanel is intentionally correct because its final explanatory TextBlock fills after a right-docked status pill and is excluded.

## Reserved scope

Replace only the issue-list subheader DockPanel with a deterministic responsive `*` + `Auto` grid. Preserve filters, named controls, locate/double-click handlers, exact read-only issue-grid schema, all health/review wording, and the intentionally-correct footer DockPanel.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/ModelHealthWindow.xaml`
- new `scripts/preflight-model-health-responsive-subheader.py`
- this claim file

## Excluded scope

- model-health calculation/triage/code-behind/Core behavior
- footer layout, dark-host selection theme, shared Theme
- other windows, V26/release/GitHub Actions/native runtime claims

## Validation plan

- Require named `ModelHealthIssueHeaderGrid` with `*` + `Auto` columns and right-aligned/no-wrap `DOUBLE-CLICK → CAD LOCATE`.
- Preserve `SummaryText`, `SearchBox`, `SeverityCombo`, `VisibleCountText`, `IssueGrid`, Locate/filter/double-click handlers, and exact current issue-grid column bindings.
- Require the existing footer `DockPanel LastChildFill="True"` with final explanatory TextBlock to remain intact.
- Reject only the stale issue-subheader final-child right docking.
- Re-fetch current `main` before source write and exact pushed XAML/regression after implementation; inspect intervening files for overlap.

## Coordination

Recent commit/code search found no Model Health responsive lane. Current Material Catalog and other UI work is on distinct surfaces.

## Completion condition

The narrow responsive subheader redesign and focused regression are on current `main`, exact source/test are read back, implementation diff/ancestry are checked, and this claim is closed `COMPLETED` with only actually executed validation reported.