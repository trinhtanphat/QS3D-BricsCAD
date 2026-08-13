# Work claim — V25 dark selection and scope controls

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-v25-dark-selection-20260813`
- Registered: `2026-08-13T14:11:12+07:00`
- Baseline main SHA: `eb842ad2237b0fea5c95e5dcc6f6a28e93253ffa`
- Priority: User-visible BricsCAD V25 palette regression reproduced in the supplied screenshot: selected Model tree rows and Zone/Floor ComboBox chrome can fall back to bright host/system rendering inside the dark QS3D palette.

## Reserved scope

Make the existing QS3D V25 dark theme host-independent for the Workspace scope controls and Model tree selection state only. Pin Zone/Floor ComboBoxes to the repository-owned dark implicit ComboBox style as explicit local styles, and shadow WPF host/system active + inactive TreeView selection brushes at the Model tree resource boundary so the default container template cannot fall back to a bright host highlight. Preserve hierarchy, expansion, selection semantics, commands and product behavior.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.DarkHostTheme.cs` (new presentation-only host compatibility partial)
- `scripts/preflight-workspace-dark-selection.py` (new focused source regression)
- read-only contract references: `src/QS3D.BricsCAD.V25/UI/Theme.xaml`, `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml`

## Excluded scope

- edits to shared `Theme.xaml` or `WorkspacePanel.xaml`
- Workspace responsive layout, ScrollViewer/compact-shell behavior and sizing
- `WorkspacePanel.CompactShell.cs`, Quantity Insight, Right Panel, V26 adapter behavior
- model/domain selection semantics, Zone/Floor business logic or project filtering
- native BricsCAD runtime qualification and release/installer work

## Validation plan

- Add a deterministic source regression that verifies the bridge explicitly applies the repository-owned ComboBox style to Zone/Floor and shadows both active/inactive WPF system selection brushes on `ModelTree`.
- Verify the existing Theme still owns host-independent ComboBox chrome and dark `BgSelectedBrush` selection contract.
- Run the focused Python preflight locally from an isolated materialization of the exact touched/current source where connector access permits; otherwise record the environment limitation and re-fetch exact source from `main` for structural verification.
- Do not report native BricsCAD V25 visual PASS unless it is actually exercised in a licensed BricsCAD runtime.

## Coordination

The earlier `chatgpt-sol-ui-polish-20260813` palette claim is now closed on `main`. This lane intentionally avoids its shared XAML/compact-shell surfaces and addresses the separate bright host-selection/control-chrome defect visible in the new screenshots through a bounded host-compatibility bridge.

## Completion condition

The focused fix and regression are pushed to current `main`, remote ancestry is verified, this claim is marked `COMPLETED` with exact implementation/final SHAs and validation actually executed, and no native BricsCAD PASS is claimed without runtime evidence.
