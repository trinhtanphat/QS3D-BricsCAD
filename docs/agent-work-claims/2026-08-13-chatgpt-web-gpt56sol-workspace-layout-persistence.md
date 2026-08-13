# Work claim — Workspace compact-shell layout persistence

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-workspace-layout-persistence-20260813`
- Registered: `2026-08-13T14:30:00+07:00`
- Completed: `2026-08-13T14:34:00+07:00`
- Baseline main SHA: `62e174679df73cfaaf54d6278fb7e6caf826cd7b`
- Priority: User-visible V25 Workspace follow-up. Source inspection confirmed the compact presentation layer runs on `Loaded` after `AttachLayoutPersistence()` and rewrote the persisted model/family pane widths and family/room split heights with hard-coded presentation defaults, so splitter settings could be lost every time the palette loaded.

## Reserved scope

Preserve per-user Workspace splitter dimensions across palette load while keeping the compact presentation-only header/footer density and splitter preview behavior. Remove only the hard-coded pane width/height rewrites from the compact-shell layer; do not change the persistence store format, project/QSDB state, semantic behavior, or native viewport boundary.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.CompactShell.cs`
- `scripts/preflight-ui-layout-persistence.py`
- read-only references: `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.LayoutPersistence.cs`, `src/QS3D.BricsCAD.V25/Services/UserUiLayoutStore.cs`, `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml`

## Excluded scope

- `WorkspacePanel.DarkHostTheme.cs` and the separate dark host-selection/selection-coverage lanes
- PaletteSet outer window size persistence
- RightPanel / QuantityInsight layout behavior
- model/domain mutations, commands, QSDB persistence
- native BricsCAD runtime qualification or release/installer work

## Result

- Implementation: `3752d589634d5a909fb58691150f6fac2905c225` (`fix(ui): preserve persisted Workspace pane sizes`).
  - `TuneWorkspaceGrid()` now limits itself to non-persisted header/footer chrome density and splitter preview/focus behavior.
  - It no longer rewrites `ModelColumnWidth`, `FamilyColumnWidth`, `FamilyTopHeight`, or `RoomTopHeight` after `WorkspacePanel.LayoutPersistence.cs` restores them.
  - It also no longer tightens those persisted pane minimums from the compact presentation layer.
- Regression: `dc240c94832d57bde770eadaeb9c79658fe5731e` (`test(ui): guard compact shell layout persistence`).
  - The canonical layout-persistence preflight now includes `WorkspacePanel.CompactShell.cs` and fails if the compact shell assigns any persisted pane width/height/minimum again.

## Validation actually executed

- Re-fetched current `main` implementation and verified `TuneWorkspaceGrid()` contains only the two non-persisted outer chrome row adjustments plus splitter presentation behavior; the previous four persisted pane dimension rewrites are absent.
- Re-fetched the updated canonical layout-persistence preflight and verified the compact-shell regression checks are present on `main`.
- Verified claim commit `4171f456ce676f2af96f8caf64bd4b8015cad3f2` is an ancestor of current `main` via repository compare (`behind_by=0`).
- No GitHub Actions were dispatched by this lane. The focused Python gate and native BricsCAD V25 visual/runtime qualification were not executed in this connector-only pass, so no runtime PASS is claimed.

## Coordination

The earlier `chatgpt-sol-ui-polish-20260813` and dark host-selection claim are completed. A later separate Workspace selection-coverage claim appeared on `main`; it owns `WorkspacePanel.DarkHostTheme.cs` / selection-resource coverage and does not overlap this compact-shell persistence fix. LOCAL-003/Curtain and measurement/research lanes remain unrelated.

## Completion condition

Satisfied for repository source/regression: focused fix + regression are pushed to current `main`, remote ancestry/source are verified, and remaining native BricsCAD qualification is explicitly unclaimed pending licensed local runtime evidence.
