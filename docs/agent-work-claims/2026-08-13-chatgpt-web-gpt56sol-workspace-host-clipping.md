# Work claim — Workspace host clipping boundary

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-workspace-host-clipping-20260813`
- Registered: `2026-08-13T14:37:30+07:00`
- Completed: `2026-08-13T14:42:00+07:00`
- Baseline main SHA: `575f86deaec7c47632846f5a2b1cb93ea85553f6`
- Priority: User-supplied BricsCAD V25 screenshot shows the right-column `Chọn phòng` action visibly painting into the gray CAD area outside the dark docked Workspace palette. Current XAML intentionally keeps a 560-DIP overflow content surface for compact hosts but neither the Workspace host boundary nor `WorkspaceOverflow` was explicitly clipped, leaving BricsCAD-hosted WPF behavior dependent on implicit host/template clipping.

## Reserved scope

Make the Workspace overflow boundary explicitly clip descendant rendering to the palette/scroll viewport while preserving horizontal scrolling, current handlers, responsive compact shell, dark-selection coverage, and native CAD viewport ownership. Implement the guard in a small presentation-only partial so the canonical XAML and concurrent UI resources remain untouched.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.HostClipping.cs`
- `scripts/preflight-workspace-host-clipping.py`
- read-only references: `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml`, `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.CompactShell.cs`, `src/QS3D.BricsCAD.V25/PaletteCoordinator.cs`

## Excluded scope

- edits to `WorkspacePanel.xaml`, `WorkspacePanel.CompactShell.cs`, or `WorkspacePanel.DarkHostTheme.cs`
- per-user splitter persistence logic already completed in the preceding lane
- RightPanel / QuantityInsight clipping
- commands, semantic/domain behavior, PaletteSet docking policy or release/installer work
- native BricsCAD visual PASS claims without licensed runtime evidence

## Result

- Implementation: `9e4bce9bb57f410f17b0fa32e3c7bdd5fd986dc3` (`fix(ui): clip Workspace rendering to palette host`).
  - Adds a presentation-only `WorkspacePanel.HostClipping.cs` partial.
  - On Workspace `Loaded`, sets `ClipToBounds = true` on the panel host and `WorkspaceOverflow.ClipToBounds = true` on the horizontal overflow viewport.
  - Does not touch commands, semantic state, the canonical XAML, compact-shell persistence, or dark-selection resources.
- Regression: `35598032dfec3f40b7657327903db760fce0742a` (`test(ui): guard Workspace palette clipping boundary`).
  - Requires both clipping assignments and preserves the existing 560-DIP horizontal-overflow / `Chọn phòng` action contract.

## Validation actually executed

- Re-fetched the exact pushed `WorkspacePanel.HostClipping.cs` and focused regression from current `main`; both required clipping assignments and presentation-only boundaries are present.
- Parsed the exact focused Python regression with Python `ast.parse` — syntax PASS.
- Verified refined claim commit `2080ff56f8c2ec8777a554774fa1671293070ce3` is an ancestor of current `main` via repository compare (`behind_by=0`).
- No GitHub Actions were dispatched by this lane. Native BricsCAD V25 clipping/pixel qualification was not executed and is not claimed as PASS.

## Coordination

The separate V25 Workspace selection-coverage lane completed while this lane was in progress and remained non-overlapping (`WorkspacePanel.DarkHostTheme.cs` / selection resources only). Measurement and LOCAL/Curtain lanes are unrelated.

## Completion condition

Satisfied for repository source/regression: focused clipping fix + source regression are pushed to current `main`, remote ancestry/source are verified, and remaining native BricsCAD visual qualification is explicitly unclaimed pending a licensed local runtime smoke.
