# Work claim — Workspace model-section header overlap

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-model-section-header-overlap-20260811`
- Registered: `2026-08-11T21:40:00+07:00`
- Baseline: current `main` after premium theme v2 and responsive top-header integration.
- Owner evidence: the supplied BricsCAD runtime screenshot shows the compact left `MÔ HÌNH` section and its `Làm mới` action competing for the same narrow horizontal space; the owner explicitly asked to remove component/element overlap.

## Reserved scope

Fix only the narrow **model-section** header inside the existing Workspace palette (`MÔ HÌNH` + `Làm mới`). Preserve the already-completed premium theme v2 and responsive top-header breakpoint work. The fix must be presentation-only, keep all existing XAML handlers/tooltips/bindings, reserve independent layout space for the title/caption and refresh action, and trim text instead of allowing visual collision.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.CompactShell.cs`
- `scripts/preflight-workspace-compact-shell.py`
- this claim file for close-out

## Excluded scope

- No changes to `Theme.xaml` / premium theme v2.
- No changes to `WorkspacePanel.xaml`, handlers, Core semantics, project state, CAD commands, selection behavior or viewport behavior.
- No Right Panel / Ribbon / BQ / updater / release work.
- No GitHub Actions dispatch.

## Validation

- Re-fetch latest `main` and preserve the existing `TuneResponsiveHeader()` implementation.
- Require a focused `MÔ HÌNH` / `Làm mới` collision guard: disable DockPanel last-child fill for that exact header, dock the title stack left and refresh action right, constrain title width from actual measured header/button width, and apply ellipsis/no-wrap to the header labels.
- Extend the existing compact-shell preflight without weakening prior top-header, handler or CAD-boundary checks.
- Real BricsCAD V25 visual/DPI verification remains `LOCAL-012`; remote source/static work must not claim runtime PASS.
