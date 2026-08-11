# Work claim — Premium dark luxury theme v2

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-premium-theme-v2-20260811-2057`
- Registered: `2026-08-11T20:57:00+07:00`
- Scope expanded: `2026-08-11T21:14:00+07:00`
- Baseline main SHA: `3e48854288d0f2d02148d06fce93f0854b75d1ba`
- Expansion baseline main SHA: `aa8b4a067b18f0752a6c097993c0771c1b356516`
- Priority: owner supplied real BricsCAD runtime screenshots and requested a premium / professional / luxury UI upgrade, explicit fixes for white/light ComboBox hover/selected states, and a follow-up fix for Workspace elements visually overlapping at the current palette width.

## Reserved scope

Upgrade the shared BricsCAD-hosted WPF design system: richer graphite/navy surface hierarchy, restrained champagne luxury accent, stronger action hierarchy, fully dark host-independent ComboBox/TextBox/CheckBox/ScrollBar chrome, more polished list/table/card/title states, and source guards/documentation for those contracts. Keep the design CAD-first, dense, high-contrast and suitable for Vietnamese labels at narrow palette widths.

The owner then supplied a second screenshot showing a concrete Workspace collision/overlap defect. The previously-active `Workspace compact BLT-style shell polish` lane is now `COMPLETED` on `main` at integrated commit `46a2f6562f230bad39f95dc1ad37abf629ebb607`, so this claim is expanded to own only the newly demonstrated collision fix on the existing Workspace header/action layout. The fix must preserve every existing handler/binding and the completed compact-shell presentation layer.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/Theme.xaml`
- `scripts/preflight-wpf-theme.py`
- `docs/UI-UX-PREMIUM-PLAN.md`
- `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml` — only the exact collision/overlap layout shown by the owner screenshot
- `scripts/preflight-workspace-compact-shell.py` — only if required to guard the collision fix without weakening the existing compact-shell contract
- this claim file for close-out

## Excluded scope

- No edits to Workspace handlers, semantic/property mutation logic, `WorkspacePanel.CompactShell.cs` behavior unless a narrowly-proven presentation-only compatibility adjustment is unavoidable.
- No edits to `RightPanel*`, `PaletteCoordinator.cs`, Ribbon, Project Tools, BQ/quantity windows, Schedule windows, Start Center, updater/release, Core semantics, CAD commands or business logic.
- Do not change command routing, project state, selection behavior, CAD viewport behavior or plugin hosting boundaries.
- No GitHub Actions dispatch or release publication.

## Validation plan

- Re-fetch current `main` immediately before implementation and before the final push.
- Keep `Theme.xaml` well-formed XAML and preserve all existing public resource keys used by current UI.
- Strengthen `scripts/preflight-wpf-theme.py` to require the premium palette/brush set and host-independent dark templates for ComboBox/TextBox/CheckBox/ScrollBar while retaining the explicit `PanelTitle` foreground guard.
- For the Workspace collision, preserve all existing `x:Name`, handlers, bindings and compact-shell source contracts; use normal WPF layout (`Grid`/`DockPanel`, explicit spacing/min-width/trim/wrap) rather than negative margins or overlay tricks.
- Source-review the final theme for no `DropShadowEffect`, blur, animated gradients, CAD commands, project mutation or heavy per-row effects.
- Do not claim BricsCAD V25 runtime/HiDPI visual PASS remotely; existing `LOCAL-012` remains the local visual/selection qualification boundary.

## Coordination

The completed Workspace compact-shell claim no longer reserves Workspace layout. Its implementation and preflight remain authoritative and must be preserved. Any currently active neighboring feature/window claims keep ownership of their files and behavior. This lane owns the shared design-system layer plus only the owner-demonstrated Workspace collision fix; it does not take over unrelated Workspace functionality.

## Completion condition

The shared premium theme v2, detailed plan refresh, focused static guard and exact Workspace collision fix are pushed on current `main`; existing resource keys, handlers, semantic behavior and completed compact-shell behavior remain compatible; the claim is marked `COMPLETED` with exact implementation SHA and actual source/static validation performed, while real BricsCAD visual qualification remains truthfully local-only.
