# Work claim — Workspace host clipping boundary

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-workspace-host-clipping-20260813`
- Registered: `2026-08-13T14:37:30+07:00`
- Baseline main SHA: `575f86deaec7c47632846f5a2b1cb93ea85553f6`
- Priority: User-supplied BricsCAD V25 screenshot shows the right-column `Chọn phòng` action visibly painting into the gray CAD area outside the dark docked Workspace palette. Current XAML intentionally keeps a 560-DIP overflow content surface for compact hosts but neither the `UserControl` host boundary nor `WorkspaceOverflow` explicitly sets `ClipToBounds`, leaving the BricsCAD-hosted WPF boundary dependent on implicit host/template clipping.

## Reserved scope

Make the Workspace overflow boundary explicitly clip descendant rendering to the palette/scroll viewport while preserving horizontal scrolling, current handlers, responsive compact shell, dark-selection coverage, and native CAD viewport ownership.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml`
- `scripts/preflight-ui-layout-persistence.py`
- read-only references: `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.CompactShell.cs`, `src/QS3D.BricsCAD.V25/PaletteCoordinator.cs`

## Excluded scope

- `WorkspacePanel.DarkHostTheme.cs` and active selection-resource coverage
- per-user splitter persistence logic already completed in the preceding lane
- RightPanel / QuantityInsight clipping
- commands, semantic/domain behavior, PaletteSet docking policy or release/installer work
- native BricsCAD visual PASS claims without licensed runtime evidence

## Validation plan

- Require explicit `ClipToBounds="True"` at both the Workspace `UserControl` host surface and the `WorkspaceOverflow` viewport.
- Preserve the existing named ScrollViewer, 560-DIP content floor, horizontal overflow and vertical-disabled contract.
- Re-fetch exact pushed XAML/preflight and verify claim ancestry on current `main`; do not dispatch GitHub Actions.

## Coordination

The active V25 Workspace selection-coverage claim owns only `WorkspacePanel.DarkHostTheme.cs` / dark selection resources and explicitly excludes responsive/ScrollViewer sizing. Measurement and LOCAL/Curtain lanes are non-overlapping.

## Completion condition

Focused XAML clipping fix + source regression are pushed to current `main`, remote ancestry/source are verified, and this claim is marked `COMPLETED` with exact SHAs and only actually executed validation.
