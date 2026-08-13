# Work claim — V25 dark selection and scope controls

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-v25-dark-selection-20260813`
- Registered: `2026-08-13T14:11:12+07:00`
- Baseline main SHA: `eb842ad2237b0fea5c95e5dcc6f6a28e93253ffa`
- Priority: User-visible BricsCAD V25 palette regression reproduced in the supplied screenshot: selected Model tree rows and Zone/Floor ComboBox chrome can fall back to bright host/system rendering inside the dark QS3D palette.

## Reserved scope

Make the existing QS3D V25 dark theme host-independent for the Workspace scope controls and Model tree selection state only. Pin Zone/Floor ComboBoxes to the repository-owned dark ComboBox style and replace host-dependent TreeViewItem selection chrome with a repository-owned template while preserving hierarchy, expansion, selection semantics, commands and product behavior.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/Theme.xaml`
- `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml`
- focused offline source regression under `scripts/` (new file preferred to avoid unrelated smoke churn)

## Excluded scope

- Workspace responsive layout, ScrollViewer/compact-shell behavior and sizing
- `WorkspacePanel.CompactShell.cs`, Quantity Insight, Right Panel, V26 adapter behavior
- model/domain selection semantics, Zone/Floor business logic or project filtering
- native BricsCAD runtime qualification and release/installer work

## Validation plan

- Add a deterministic source regression that verifies explicit Workspace style attachment and a custom TreeViewItem template/selection chrome contract.
- Parse the touched XAML as XML and run the focused regression locally where available.
- Do not report native BricsCAD V25 visual PASS unless it is actually exercised in a licensed BricsCAD runtime.

## Coordination

The earlier `chatgpt-sol-ui-polish-20260813` palette claim is now closed on `main`; this lane intentionally excludes its compact-shell/ScrollViewer work and addresses the separate bright host-selection/control-chrome defect visible in the new screenshots.

## Completion condition

The focused fix and regression are pushed to current `main`, remote ancestry is verified, this claim is marked `COMPLETED` with exact implementation/final SHAs and validation actually executed, and no native BricsCAD PASS is claimed without runtime evidence.
