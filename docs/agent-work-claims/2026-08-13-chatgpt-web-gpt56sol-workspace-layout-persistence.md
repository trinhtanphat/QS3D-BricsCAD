# Work claim — Workspace compact-shell layout persistence

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-workspace-layout-persistence-20260813`
- Registered: `2026-08-13T14:30:00+07:00`
- Baseline main SHA: `62e174679df73cfaaf54d6278fb7e6caf826cd7b`
- Priority: User-visible V25 Workspace follow-up. Source inspection confirms the compact presentation layer runs on `Loaded` after `AttachLayoutPersistence()` and currently rewrites the persisted model/family pane widths and family/room split heights with hard-coded presentation defaults, so splitter settings can be lost every time the palette is loaded.

## Reserved scope

Preserve per-user Workspace splitter dimensions across palette load while keeping the compact presentation-only header/footer density and splitter preview behavior. Remove only the hard-coded pane width/height rewrites from the compact-shell layer; do not change the persistence store format, project/QSDB state, semantic behavior, or native viewport boundary.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.CompactShell.cs`
- `scripts/preflight-ui-layout-persistence.py`
- read-only references: `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.LayoutPersistence.cs`, `src/QS3D.BricsCAD.V25/Services/UserUiLayoutStore.cs`, `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml`

## Excluded scope

- `WorkspacePanel.DarkHostTheme.cs` and the completed dark host-selection lane
- PaletteSet outer window size persistence
- RightPanel / QuantityInsight layout behavior
- model/domain mutations, commands, QSDB persistence
- native BricsCAD runtime qualification or release/installer work

## Validation plan

- Extend the existing source preflight to assert the compact-shell layer does not assign the four persisted pane dimensions or tighten their persisted minimums after `AttachLayoutPersistence()`.
- Re-fetch current `main` before implementation and verify no new overlapping ACTIVE/BLOCKED claim appeared.
- Re-fetch exact pushed source/test after implementation. Native BricsCAD V25 visual/runtime PASS will not be claimed without licensed runtime evidence.

## Coordination

The earlier `chatgpt-sol-ui-polish-20260813` and `chatgpt-web-gpt56sol-v25-dark-selection-20260813` UI claims are completed. Current LOCAL-003/Curtain and measurement/research lanes are unrelated to this bounded Workspace persistence fix.

## Completion condition

Focused fix + regression are pushed to current `main`, remote ancestry/source are verified, and this claim is marked `COMPLETED` with exact SHAs and validation actually executed.
