# Work claim — V25 Workspace dark selection coverage

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-v25-workspace-selection-coverage-20260813`
- Registered: `2026-08-13T14:27:30+07:00`
- Baseline main SHA: `0413a7951f1946b54211f04ea3de2b901b0576a0`
- Priority: Follow-up to the screenshot-visible white-selection fix. Source inspection shows the same Workspace also uses Family `ListBox`, property/inspection `ListView`, a second room-finish `TreeView`, and additional ComboBoxes. Their container styles still rely on stock WPF templates, so host/system active or inactive selection resources can leak bright chrome through the same mechanism already confirmed on `ModelTree`.

## Reserved scope

Extend the presentation-only V25 Workspace host-theme guard from the screenshot-visible Zone/Floor + ModelTree controls to the rest of the Workspace selection/control surface. Publish the canonical ComboBox style as a direct Workspace resource for all descendant ComboBoxes, and shadow active/inactive WPF selection background/text resources at the Workspace resource boundary so TreeView/ListBox/ListView containers use QS3D dark selection colors without changing selection semantics or business logic.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.DarkHostTheme.cs`
- `scripts/preflight-workspace-dark-selection.py`
- read-only verification of `src/QS3D.BricsCAD.V25/UI/Theme.xaml` and `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml`

## Excluded scope

- Level/native geometry and current `LOCAL-003` runtime qualification
- Workspace responsive layout, ScrollViewer/compact-shell sizing, commands, model/domain selection behavior
- shared `Theme.xaml` redesign, V26 behavior, Quantity Insight/Right Panel, installer/release work
- native BricsCAD visual PASS claims without an actual licensed runtime smoke

## Validation plan

- Regression must require Workspace-level `SystemColors.Highlight*` and `InactiveSelectionHighlight*` shadowing, not only `ModelTree.Resources`.
- Regression must require the repository-owned implicit ComboBox style to be re-published at `WorkspacePanel.Resources` while preserving explicit Zone/Floor pinning.
- Re-fetch exact pushed source from current `main` and verify ancestry; do not dispatch GitHub Actions.

## Coordination

`LOCAL-003` is active but owns Level/native geometry qualification and is non-overlapping with this presentation-only Workspace resource lane. The previous dark-selection claim is `COMPLETED`; this is a new bounded coverage lane rather than reopening that reservation.

## Completion condition

The broader Workspace selection/resource guard and focused regression are pushed to current `main`, remote ancestry is verified, this claim is marked `COMPLETED` with exact SHAs and only actually executed validation is reported.
