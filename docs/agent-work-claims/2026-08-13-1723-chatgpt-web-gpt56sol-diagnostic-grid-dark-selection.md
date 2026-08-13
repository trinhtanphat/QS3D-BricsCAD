# Work claim — V25 diagnostic grid dark selection

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-diagnostic-grid-dark-selection-20260813`
- Registered: `2026-08-13T17:23:00+07:00`
- Baseline main SHA: `83d2e25fbebf06b30b7729152961267f08feda63`
- Priority: Continue the user-requested V25 host-theme audit on read-only diagnostic surfaces. `ModelHealthWindow.xaml` contains `IssueGrid` and `AuditLogWindow.xaml` contains `Grid`, both stock-template DataGrids. Shared dark row/cell styles do not own the WPF container templates, so active/inactive `SystemColors` selection resources can still be supplied by the BricsCAD host.

## Reserved scope

Keep the Model Health and Audit Log DataGrid selections on QS3D-owned dark active/inactive resources. Add one presentation-only guard partial per window with root + named DataGrid resource pins. Preserve filtering, locate/double-click, audit rendering and all project/CAD semantics.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/ModelHealthWindow.DarkHostTheme.cs` (new)
- `src/QS3D.BricsCAD.V25/UI/AuditLogWindow.DarkHostTheme.cs` (new)
- `scripts/preflight-diagnostic-grid-dark-selection.py` (new)
- read-only ModelHealth/AuditLog XAML and shared Theme contracts

## Excluded scope

- health calculation/locate behavior, audit content/filtering/business logic
- shared Theme redesign, other windows, V26, release/installer work
- GitHub Actions dispatch and native BricsCAD PASS claims without licensed runtime evidence

## Validation plan

- Require all four active/inactive WPF selection background/text keys in each guard.
- Require root plus `IssueGrid` / `Grid` local resource pins.
- Preserve `OnGridDoubleClick` and audit grid/search contracts; forbid project/CAD/command mutation paths in the presentation partials.
- Re-fetch after registration, then re-fetch exact pushed source/test and verify ancestry.

## Coordination

Prior dark-host lanes through Quantity Settings and Material Catalog are completed. Current drawing/Curtain/runtime/mapping lanes are unrelated. No recent Model Health or Audit Log dark-selection claim was found.

## Completion condition

Both diagnostic grid guards + focused regression are pushed to current `main`, exact source/ancestry are verified, and this claim is marked `COMPLETED` with exact SHAs and validation actually executed.
