# Work claim — Workspace host clipping boundary

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-workspace-host-clipping-20260813`
- Registered: `2026-08-13T14:37:30+07:00`
- Baseline main SHA: `575f86deaec7c47632846f5a2b1cb93ea85553f6`
- Priority: User-supplied BricsCAD V25 screenshot shows the right-column `Chọn phòng` action visibly painting into the gray CAD area outside the dark docked Workspace palette. Current XAML intentionally keeps a 560-DIP overflow content surface for compact hosts but neither the Workspace host boundary nor `WorkspaceOverflow` is explicitly clipped, leaving BricsCAD-hosted WPF behavior dependent on implicit host/template clipping.

## Reserved scope

Make the Workspace overflow boundary explicitly clip descendant rendering to the palette/scroll viewport while preserving horizontal scrolling, current handlers, responsive compact shell, dark-selection coverage, and native CAD viewport ownership. Implement the guard in a small presentation-only partial so the canonical XAML and active/concurrent UI resources remain untouched.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.HostClipping.cs` (new presentation-only partial)
- `scripts/preflight-workspace-host-clipping.py` (new focused source regression)
- read-only references: `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml`, `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.CompactShell.cs`, `src/QS3D.BricsCAD.V25/PaletteCoordinator.cs`

## Excluded scope

- edits to `WorkspacePanel.xaml`, `WorkspacePanel.CompactShell.cs`, or `WorkspacePanel.DarkHostTheme.cs`
- per-user splitter persistence logic already completed in the preceding lane
- RightPanel / QuantityInsight clipping
- commands, semantic/domain behavior, PaletteSet docking policy or release/installer work
- native BricsCAD visual PASS claims without licensed runtime evidence

## Validation plan

- Focused regression must require `ClipToBounds = true` on the `WorkspacePanel` host and `WorkspaceOverflow.ClipToBounds = true` after the panel is loaded.
- Preserve the existing named ScrollViewer, 560-DIP content floor, horizontal overflow and vertical-disabled XAML contract.
- Re-fetch exact pushed partial/preflight and verify claim ancestry on current `main`; do not dispatch GitHub Actions.

## Coordination

The V25 Workspace selection-coverage lane owns only `WorkspacePanel.DarkHostTheme.cs` / dark selection resources and excludes responsive/ScrollViewer sizing. Measurement and LOCAL/Curtain lanes are non-overlapping. This scope update is claim-only and precedes the new partial/test writes.

## Completion condition

Focused clipping fix + source regression are pushed to current `main`, remote ancestry/source are verified, and this claim is marked `COMPLETED` with exact SHAs and only actually executed validation.
