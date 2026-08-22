# Work claim — V25 diagnostic grid dark selection

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-diagnostic-grid-dark-selection-20260813`
- Registered: `2026-08-13T17:23:00+07:00`
- Completed: `2026-08-13T17:26:00+07:00`
- Baseline main SHA: `83d2e25fbebf06b30b7729152961267f08feda63`
- Priority: Continue the user-requested V25 host-theme audit on read-only diagnostic surfaces. `ModelHealthWindow.xaml` contains `IssueGrid` and `AuditLogWindow.xaml` contains `Grid`, both stock-template DataGrids. Shared dark row/cell styles do not own the WPF container templates, so active/inactive `SystemColors` selection resources can still be supplied by the BricsCAD host.

## Reserved scope

Keep the Model Health and Audit Log DataGrid selections on QS3D-owned dark active/inactive resources. Add one presentation-only guard partial per window with root + named DataGrid resource pins. Preserve filtering, locate/double-click, audit rendering and all project/CAD semantics.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/ModelHealthWindow.DarkHostTheme.cs`
- `src/QS3D.BricsCAD.V25/UI/AuditLogWindow.DarkHostTheme.cs`
- `scripts/preflight-diagnostic-grid-dark-selection.py`
- read-only ModelHealth/AuditLog XAML and shared Theme contracts

## Excluded scope

- health calculation/locate behavior, audit content/filtering/business logic
- shared Theme redesign, other windows, V26, release/installer work
- GitHub Actions dispatch and native BricsCAD PASS claims without licensed runtime evidence

## Result

- Model Health implementation: `ef6e85e9d16604dfea88e895fac5c55946892bd2` (`fix(v25): keep Model Health selection dark`).
- Audit Log implementation: `3a56150f8b571662c4eb8bf4ec71273124d4d399` (`fix(v25): keep Audit Log selection dark`).
- Regression: `51c81d5a3447c909baa73623ac1cb11cb3d6c35a` (`test(ui): guard diagnostic grid dark selection`).
- Each guard shadows all four active/inactive WPF selection background/text keys using `BgSelectedBrush` / `TextBrush` at its window boundary and directly on `IssueGrid` or `Grid`; no diagnostic/audit behavior path is changed.

## Validation actually executed

- Re-fetched exact current-main Model Health and Audit Log guard source; all four selection keys and local DataGrid resource pins are present.
- Current XAML retains `IssueGrid` / `OnGridDoubleClick` and Audit `Grid` / `OnSearchChanged` contracts unchanged.
- Shared Theme retains canonical `BgSelectedBrush`, `DataGridRow` and `DataGridCell` contracts.
- Focused regression logic — `PASS: V25 diagnostic DataGrid dark host-selection contract` in an isolated connector-derived fixture.
- `compare_commits(51c81d5a3447c909baa73623ac1cb11cb3d6c35a, main)` returned `status=ahead`, `behind_by=0`, merge-base equal to the regression commit; the only newer changed file at that check was unrelated V26 preflight work.
- No GitHub Actions were dispatched. Native BricsCAD V25 visual/runtime qualification was not executed and is not claimed as PASS.

## Coordination

Prior dark-host lanes through Quantity Settings and Material Catalog are completed. Concurrent drawing/Curtain/runtime/mapping work did not touch this scope.

## Completion condition

Satisfied for repository source/regression: both diagnostic grid guards and focused regression are pushed to `main`, exact source/ancestry were verified, and native visual qualification remains pending a licensed runtime smoke.
