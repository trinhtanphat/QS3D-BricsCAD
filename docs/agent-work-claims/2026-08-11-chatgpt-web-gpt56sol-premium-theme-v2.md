# Work claim — Premium dark luxury theme v2

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-premium-theme-v2-20260811-2057`
- Registered: `2026-08-11T20:57:00+07:00`
- Scope expanded: `2026-08-11T21:14:00+07:00`
- Completed: `2026-08-11T21:36:00+07:00`
- Baseline main SHA: `3e48854288d0f2d02148d06fce93f0854b75d1ba`
- Expansion baseline main SHA: `aa8b4a067b18f0752a6c097993c0771c1b356516`
- Priority: owner supplied real BricsCAD runtime screenshots and requested a premium / professional / luxury UI upgrade, explicit fixes for white/light ComboBox hover/selected states, and a follow-up fix for Workspace elements visually overlapping at the current palette width.

## Delivered scope

The shared BricsCAD-hosted WPF design system is upgraded to premium/luxury v2 while remaining CAD-first and dense:

- deep graphite/navy surface hierarchy;
- restrained champagne hierarchy accent, not a decorative gold skin;
- stronger primary / secondary / destructive command hierarchy;
- explicit high-contrast text hierarchy;
- fully dark host-independent `ComboBox`, `TextBox`, `CheckBox`, `RadioButton`, `ScrollBar` and `ToolTip` templates;
- polished `TreeView`, `ListView`, `ListBox`, `GridViewColumnHeader`, `DataGrid` and card/status primitives;
- explicit `PanelTitle` foreground guard retained;
- no `DropShadowEffect`, blur/acrylic, animated gradient or per-row effect.

The owner-demonstrated Workspace header collision is also fixed in the completed compact-shell presentation layer without changing its XAML handlers/bindings or business behavior:

- header reacts to its actual width through presentation-only `SizeChanged` handling;
- at the compact breakpoint the decorative `SEMANTIC MODEL` badge collapses;
- at the narrow breakpoint the additional `BIM WORKSPACE` badge collapses and the visible `Xoay 3D` / `Zoom chọn` labels shorten to `Xoay` / `Zoom` while their existing handlers and tooltips remain intact;
- the center status text has `MinWidth = 0`, centered ellipsis-safe layout and tighter margins;
- no negative margins, overlays or command-routing changes were introduced.

## Implemented surfaces

- `src/QS3D.BricsCAD.V25/UI/Theme.xaml`
- `scripts/preflight-wpf-theme.py`
- `docs/UI-UX-PREMIUM-PLAN.md`
- `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.CompactShell.cs`
- `scripts/preflight-workspace-compact-shell.py`
- this claim file

`WorkspacePanel.xaml` itself was intentionally left unchanged for the collision follow-up so every completed compact-shell `x:Name`, handler, binding and original full display label remains canonical. The responsive adjustment is presentation-only and applied by the existing `WorkspacePanel.CompactShell.cs` layer.

## Commits / integration

### Shared premium theme v2

- source commit: `110c00128bdcd99680c870b472c2d403e04cc2b6`
- integrated into `main` through PR `#465`
- main merge commit: `7224baa13b03e5599419bdd6f025ca3dbb2040f3`

### Workspace collision follow-up

- responsive presentation commit: `d29603eace7ee367616c0e32e1c1ba619e5aec2c`
- regression guard commit: `bb9b10729561290c851a5f8d177a31fac269d1e0`
- integrated into `main` through PR `#469`
- main merge commit: `7c96951a0b75cb105f6b333b8d20fe92871aaf08`

## Validation evidence

- The shared theme static preflight was executed against the exact prepared premium v2 blob set before integration and passed: required premium brushes, explicit `PanelTitle` contrast, dark host-independent core-control chrome and the no-heavy-effects contract were all present.
- Final `main` uses the exact validated `Theme.xaml` blob `d3f0f91e28833140a1ec977b5ac84a8787bbc3f3`.
- Final source review confirms the responsive Workspace header uses only WPF presentation properties (`Visibility`, margins, padding, display labels, `MinWidth`, `TextAlignment`) and preserves the existing XAML click handlers/tooltips and BricsCAD viewport boundary.
- `scripts/preflight-workspace-compact-shell.py` now locks the `570` / `700` breakpoints, badge-collapse contract, narrow display labels, status minimum-width behavior and rejects negative-margin collision hacks while retaining all previous action/semantic-boundary guards.
- No GitHub Actions were dispatched.

## Excluded / unchanged

- No edits to Workspace handlers, semantic/property mutation logic, project state, selection behavior, CAD commands, CAD viewport behavior or plugin hosting boundaries.
- No edits to `RightPanel*`, `PaletteCoordinator.cs`, Ribbon, Project Tools, BQ/quantity windows, Schedule windows, Start Center, updater/release or Core semantics in this lane.
- No release publication.

## LOCAL_ONLY boundary

Real BricsCAD V25 runtime visual qualification, Vietnamese text clipping, palette widths and HiDPI 100% / 125% / 150% / 200% remain under existing `LOCAL-012`. Remote/source work does not claim a licensed BricsCAD runtime PASS.

## Completion

Reservation released. Shared premium theme v2 and the exact Workspace header collision follow-up are integrated into `main`; source/static contracts are guarded; future agents may edit these files only after re-checking current `main` and active claims.
