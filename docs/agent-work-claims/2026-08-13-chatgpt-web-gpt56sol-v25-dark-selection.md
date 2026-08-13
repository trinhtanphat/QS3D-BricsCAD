# Work claim — V25 dark selection and scope controls

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-v25-dark-selection-20260813`
- Registered: `2026-08-13T14:11:12+07:00`
- Completed: `2026-08-13T14:18:02+07:00`
- Baseline main SHA: `eb842ad2237b0fea5c95e5dcc6f6a28e93253ffa`
- Priority: User-visible BricsCAD V25 palette regression reproduced in the supplied screenshot: selected Model tree rows and Zone/Floor ComboBox chrome can fall back to bright host/system rendering inside the dark QS3D palette.

## Reserved scope

Make the existing QS3D V25 dark theme host-independent for the Workspace scope controls and Model tree selection state only. Pin Zone/Floor ComboBoxes to the repository-owned dark implicit ComboBox style as explicit local styles, and shadow WPF host/system active + inactive TreeView selection brushes at the Model tree resource boundary so the default container template cannot fall back to a bright host highlight. Preserve hierarchy, expansion, selection semantics, commands and product behavior.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.DarkHostTheme.cs`
- `scripts/preflight-workspace-dark-selection.py`
- read-only contract references: `src/QS3D.BricsCAD.V25/UI/Theme.xaml`, `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml`

## Excluded scope

- edits to shared `Theme.xaml` or `WorkspacePanel.xaml`
- Workspace responsive layout, ScrollViewer/compact-shell behavior and sizing
- `WorkspacePanel.CompactShell.cs`, Quantity Insight, Right Panel, V26 adapter behavior
- model/domain selection semantics, Zone/Floor business logic or project filtering
- native BricsCAD runtime qualification and release/installer work

## Result

- Implementation: `426c606bf0fa30ce2c384c5e52142551bcbcba63` (`fix(v25): pin dark Workspace host selection chrome`).
  - Resolves the existing implicit dark ComboBox style from `Theme.xaml` and assigns it locally to `ZoneCombo` / `FloorCombo`.
  - Pins ComboBox background/foreground/border to `BgInputBrush`, `TextBrush` and `BorderStrongBrush` through dynamic resource references.
  - Shadows active + inactive WPF `SystemColors` selection background/text brush keys in `ModelTree.Resources` with QS3D `BgSelectedBrush` / `TextBrush`, preventing the stock TreeViewItem template from asking the BricsCAD host for a bright selection surface.
- Regression: `a1ca93dcec52f4f06fd74f1cb01179652ee8b46e` (`test(ui): guard V25 dark host selection chrome`).
- Final pushed implementation/test SHA: `a1ca93dcec52f4f06fd74f1cb01179652ee8b46e`.

## Validation actually executed

- Re-fetched `WorkspacePanel.DarkHostTheme.cs`, the focused regression, the current `Theme.xaml` ComboBox/brush contract, and the current `WorkspacePanel.xaml` named controls from `main`; all required source contracts are present.
- `python3 -m py_compile scripts/preflight-workspace-dark-selection.py` — PASS on an isolated connector-derived fixture containing the exact pushed regression and bridge plus the current-main Theme/Workspace contract snippets.
- `python3 scripts/preflight-workspace-dark-selection.py` — `PASS: V25 Workspace dark host-selection contract` on that focused connector-derived fixture.
- Environment limitation: the execution container cannot resolve `github.com`, and it has no `dotnet`, C# compiler or `pwsh`, so a fresh full checkout/build could not be executed there.
- Native BricsCAD V25 visual/runtime qualification was not executed and is not claimed as PASS.

## Coordination

The earlier `chatgpt-sol-ui-polish-20260813` palette claim was already closed on `main`. This lane avoided its shared XAML/compact-shell surfaces and addressed the separate bright host-selection/control-chrome defect visible in the new screenshots through a bounded host-compatibility bridge.

## Completion condition

Satisfied for repository source/regression: the focused fix and test are pushed to `main`, remote source is re-fetched and verified, and native BricsCAD qualification remains explicitly unclaimed pending a licensed local runtime visual smoke.
