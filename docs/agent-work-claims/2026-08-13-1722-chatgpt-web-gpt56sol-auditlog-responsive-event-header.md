# Work claim — V25 AuditLog responsive event header

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-auditlog-responsive-event-header-20260813`
- Registered: `2026-08-13T17:22:00+07:00`
- Baseline main SHA: `736113bbc4bb90c75e84a706b8a6b5109419b325`
- Priority: user-visible V25 UI hardening after the RightPanel/Quantity Insight responsive pass. Current `AuditLogWindow.xaml` still implements the event-list title/status row as a `DockPanel` whose final child is marked `DockPanel.Dock="Right"`. With default `LastChildFill=True`, that last status text fills the remaining width instead of honoring its right dock, making header alignment host/width dependent.

## Reserved scope

Replace only the `DÒNG SỰ KIỆN` / `UTC • PROJECT AUDIT` title row with a deterministic two-column responsive grid (`*` + `Auto`). Keep the title shrinkable and ellipsized, keep the audit status in the auto/right column, and preserve all search/DataGrid/read-only/footer behavior.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/AuditLogWindow.xaml`
- new `scripts/preflight-auditlog-responsive-event-header.py`
- this claim file

## Excluded scope

- Audit storage/business semantics, filtering/code-behind, project/QSDB state
- shared `Theme.xaml`, RightPanel, Workspace, Quantity Insight or other window redesigns
- MTR/MAP/Core lanes, release/runtime qualification, V26, GitHub Actions
- no native BricsCAD visual PASS claim without licensed runtime evidence

## Validation plan

- Require a named `AuditEventHeaderGrid` with exactly `*` + `Auto` columns.
- Require the event title in column 0 to remain shrinkable/no-wrap/ellipsis and `UTC • PROJECT AUDIT` in the right-aligned auto column.
- Preserve `SearchBox`, `OnSearchChanged`, read-only `DataGrid`, all existing audit columns, `Summary`, and `MỚI NHẤT HIỂN THỊ TRƯỚC`.
- Reject the stale event-header `DockPanel.Dock="Right"` pattern.
- Re-fetch current `main` before source write and exact pushed source/test after implementation; verify intervening commits for overlap.

## Coordination

Current claim/commit search returned no AuditLog responsive/header claim or commit. Recent active lanes are concentrated in Measurement Trace, mapping, release/local runtime and other UI surfaces. This lane intentionally touches only AuditLog XAML plus a new focused preflight.

## Completion condition

The narrow responsive-header redesign and focused source regression are pushed to current `main`, current source is re-fetched, this claim is closed `COMPLETED` with exact SHAs, and only validation actually executed is reported.