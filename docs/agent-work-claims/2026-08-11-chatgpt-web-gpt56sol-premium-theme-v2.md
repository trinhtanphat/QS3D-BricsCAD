# Work claim — Premium dark luxury theme v2

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-premium-theme-v2-20260811-2057`
- Registered: `2026-08-11T20:57:00+07:00`
- Baseline main SHA: `3e48854288d0f2d02148d06fce93f0854b75d1ba`
- Priority: owner supplied a real BricsCAD runtime screenshot and requested a premium / professional / luxury UI upgrade; the shared theme can improve the visible palette/window chrome without overlapping the already-active Workspace compact-shell or Right Panel feature lanes.

## Reserved scope

Upgrade the shared BricsCAD-hosted WPF design system only: richer graphite/navy surface hierarchy, restrained champagne luxury accent, stronger action hierarchy, fully dark host-independent ComboBox/TextBox/CheckBox/ScrollBar chrome, more polished list/table/card/title states, and source guards/documentation for those contracts. Keep the design CAD-first, dense, high-contrast and suitable for Vietnamese labels at narrow palette widths.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/Theme.xaml`
- `scripts/preflight-wpf-theme.py`
- `docs/UI-UX-PREMIUM-PLAN.md`
- this claim file for close-out

## Excluded scope

- No edits to `WorkspacePanel.xaml`, `WorkspacePanel.*.cs`, `RightPanel*`, `PaletteCoordinator.cs`, Ribbon, Project Tools, BQ/quantity windows, Schedule windows, Start Center, updater/release, Core semantics, CAD commands or business logic.
- Do not duplicate or take over the active `Workspace compact BLT-style shell polish` claim; that lane owns Workspace density/layout and its dedicated partial.
- Do not change handlers, bindings, command routing, project state, selection behavior, CAD viewport behavior or plugin hosting boundaries.
- No GitHub Actions dispatch or release publication.

## Validation plan

- Re-fetch current `main` immediately before implementation and before the final push.
- Keep `Theme.xaml` well-formed XAML and preserve all existing public resource keys used by current UI.
- Strengthen `scripts/preflight-wpf-theme.py` to require the premium palette/brush set and host-independent dark templates for ComboBox/TextBox/CheckBox/ScrollBar while retaining the explicit `PanelTitle` foreground guard.
- Source-review the final theme for no `DropShadowEffect`, blur, animated gradients, CAD commands, project mutation or heavy per-row effects.
- Do not claim BricsCAD V25 runtime/HiDPI visual PASS remotely; existing `LOCAL-012` remains the local visual/selection qualification boundary.

## Coordination

The active Workspace compact-shell claim explicitly excludes `Theme.xaml`; this claim owns only the shared design-system layer. Any currently active feature/window claims keep ownership of their XAML and behavior. The theme must remain backward-compatible with their existing keyed styles and implicit control styles.

## Completion condition

The shared premium theme v2, detailed plan refresh and focused static guard are pushed on current `main`; existing resource keys and plugin behavior remain compatible; the claim is marked `COMPLETED` with exact implementation SHA and actual source/static validation performed, while real BricsCAD visual qualification remains truthfully local-only.
