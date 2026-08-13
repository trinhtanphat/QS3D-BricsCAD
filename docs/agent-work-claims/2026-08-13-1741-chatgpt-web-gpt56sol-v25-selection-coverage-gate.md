# Work claim — V25 dark selection coverage gate

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-v25-selection-coverage-gate-20260813`
- Registered: `2026-08-13T17:41:00+07:00`
- Baseline main SHA: `d015a14102c7014966df1202b8e949fed7764649`
- Priority: Final regression gate for the user-reported BricsCAD/WPF bright-selection family. The current V25 UI now has targeted host-theme guards across every audited XAML surface with TreeView/ListBox/ListView/DataGrid selection containers, but there is no repository-wide source gate preventing a future XAML collection surface from being added without equivalent active/inactive host-selection protection.

## Reserved scope

Add one dynamic offline preflight that scans `src/QS3D.BricsCAD.V25/UI/*.xaml` for real TreeView/ListBox/ListView/DataGrid controls (excluding `Theme.xaml`) and requires each owning XAML class to have a companion `<Class>.DarkHostTheme.cs` containing all four active/inactive WPF `SystemColors` selection resource keys plus a root resource pin. This lane is regression-only: no production UI behavior changes.

## Expected surfaces

- `scripts/preflight-v25-dark-selection-coverage.py` (new)

## Excluded scope

- any production XAML/C# changes
- ComboBox/button/text visual redesign, layout/responsiveness
- business/domain/CAD/project behavior, V26
- GitHub Actions dispatch and native BricsCAD runtime claims

## Validation plan

- The preflight dynamically discovers XAML collection surfaces rather than relying on a hard-coded window list.
- Require companion dark-host source with `HighlightBrushKey`, `InactiveSelectionHighlightBrushKey`, `HighlightTextBrushKey`, `InactiveSelectionHighlightTextBrushKey`, and a root `Resources[...]` write.
- Report every uncovered XAML file in one run so future UI additions fail visibly.
- Re-fetch exact pushed regression and verify commit ancestry. No GitHub Actions dispatch.

## Coordination

All bounded V25 dark-selection implementation lanes from Workspace through Start Center/review/schedule/diagnostic/settings/material/scope managers are completed. Current Geometry Extensions/Curtain/cost/runtime work is non-overlapping. This claim reserves only the new dynamic coverage script.

## Completion condition

Coverage regression is pushed to current `main`, exact script/ancestry are verified, this claim is marked `COMPLETED`, and no production source is changed under this lane.
