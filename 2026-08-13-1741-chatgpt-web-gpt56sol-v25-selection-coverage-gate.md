# Work claim — V25 dark selection coverage gate

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-v25-selection-coverage-gate-20260813`
- Registered: `2026-08-13T17:41:00+07:00`
- Completed: `2026-08-13T17:44:00+07:00`
- Baseline main SHA: `d015a14102c7014966df1202b8e949fed7764649`
- Priority: Final regression gate for the user-reported BricsCAD/WPF bright-selection family. The current V25 UI now has targeted host-theme guards across every audited XAML surface with TreeView/ListBox/ListView/DataGrid selection containers, and this lane adds a repository-wide source gate preventing a future XAML collection surface from being added without equivalent active/inactive host-selection protection.

## Reserved scope

Add one dynamic offline preflight that scans `src/QS3D.BricsCAD.V25/UI/*.xaml` for real TreeView/ListBox/ListView/DataGrid controls (excluding `Theme.xaml`) and requires each owning XAML class to have a companion `<Class>.DarkHostTheme.cs` containing all four active/inactive WPF `SystemColors` selection resource keys plus a root resource pin. This lane is regression-only: no production UI behavior changes.

## Expected surfaces

- `scripts/preflight-v25-dark-selection-coverage.py`

## Excluded scope

- any production XAML/C# changes
- ComboBox/button/text visual redesign, layout/responsiveness
- business/domain/CAD/project behavior, V26
- GitHub Actions dispatch and native BricsCAD runtime claims

## Result

- Regression gate: `3f972497a8a83034d1331271b73448eaa82b7059` (`test(ui): gate V25 dark selection coverage`).
- The gate dynamically scans every V25 UI XAML file for `TreeView`, `ListBox`, `ListView`, or `DataGrid` controls, derives the owning `x:Class`, and requires a companion `<Class>.DarkHostTheme.cs`.
- Every discovered companion must contain all four active/inactive WPF `SystemColors` selection background/text keys, QS3D `BgSelectedBrush` / `TextBrush` lookups, and a root `Resources[...]` write.
- The gate reports all uncovered XAML surfaces in one run rather than stopping at the first miss.
- No production XAML/C# source was changed under this regression-only lane.

## Validation actually executed

- Re-fetched the exact pushed gate from current `main`; the dynamic XAML discovery, companion-guard derivation, four-key checks, QS3D brush checks, root resource-boundary check, and aggregated failure reporting are present.
- `python -m py_compile scripts/preflight-v25-dark-selection-coverage.py` — PASS on an isolated connector-derived fixture containing the exact pushed script.
- Dynamic focused fixture run — PASS and discovered 18 current V25 XAML selection-surface files: `AuditLogWindow.xaml`, `DoorOpeningScheduleWindow.xaml`, `FamilyManagerWindow.xaml`, `FloorLevelWindow.xaml`, `MaterialCatalogWindow.xaml`, `ModelHealthWindow.xaml`, `QuantityInsightPanel.xaml`, `QuantitySettingsWindow.xaml`, `QuantitySummaryWindow.xaml`, `RebarScheduleWindow.xaml`, `RecognitionWindow.xaml`, `RevisionWindow.xaml`, `RightPanel.xaml`, `RoomFinishScheduleWindow.xaml`, `StartCenterWindow.xaml`, `WallQuantityWindow.xaml`, `WorkspacePanel.xaml`, and `ZoneManagerWindow.xaml`; each had a corresponding audited dark-host guard in the fixture.
- `compare_commits(3f972497a8a83034d1331271b73448eaa82b7059, main)` returned `status=ahead`, `behind_by=0`, with merge-base equal to the regression commit. The newer changed-file set at validation time was unrelated Curtain/Source Reconcile/Reference Search/Auto Room work and did not modify this coverage script or the audited dark-selection guard surfaces.
- No GitHub Actions were dispatched. A fresh full repository build/native BricsCAD V25 visual smoke was not executed by this lane, so no native runtime PASS is claimed.

## Coordination

All bounded V25 dark-selection implementation lanes from Workspace through Start Center/review/schedule/diagnostic/settings/material/scope managers are completed. Concurrent Geometry Extensions/Curtain/cost/runtime work is non-overlapping. This lane changed only the dynamic regression script and this claim record.

## Completion condition

Satisfied for repository regression coverage: the dynamic gate is pushed to `main`, exact script/ancestry were verified, the current audited V25 selection-surface set is covered, and no production source was changed under this lane.
