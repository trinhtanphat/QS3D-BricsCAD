# Work claim — Workspace model-section header overlap

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-model-section-header-overlap-20260811`
- Registered: `2026-08-11T21:40:00+07:00`
- Completed: `2026-08-11T21:44:00+07:00`
- Baseline: current `main` after premium theme v2 and responsive top-header integration.
- Owner evidence: the supplied BricsCAD runtime screenshot shows the compact left `MÔ HÌNH` section and its `Làm mới` action competing for the same narrow horizontal space; the owner explicitly asked to remove component/element overlap.

## Delivered scope

Fixed only the narrow **model-section** header inside the existing Workspace palette (`MÔ HÌNH` + `Làm mới`) while preserving the already-completed premium theme v2 and responsive top-header breakpoint work.

The presentation-only guard now:

- locates the exact `MÔ HÌNH` title stack and its sibling `Làm mới` button;
- disables `DockPanel.LastChildFill` only for that exact section so the refresh action cannot expand into the title/caption area;
- docks the title stack left and refresh action right;
- reserves a 7 px gap between text and action;
- measures actual header/button width and caps the title stack to the remaining horizontal space;
- uses `NoWrap` + `CharacterEllipsis` for the section title/caption at constrained widths;
- recomputes available title width on header/button `SizeChanged`;
- keeps the existing `TuneResponsiveHeader()` top-bar breakpoints unchanged.

No XAML handlers, tooltips, bindings, CAD commands, project mutation paths, selection behavior, viewport behavior or Core semantics were changed.

## Implemented surfaces

- `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.CompactShell.cs`
- `scripts/preflight-workspace-compact-shell.py`
- this claim file

## Commits / integration

- implementation commit: `bdbba119b62d7512e0f862573cd0b07eec37828f`
- integrated through PR `#482`
- `main` merge commit: `a9eabc97e749ef1d3ee16c5784344c4aa0c3b96a`

## Validation evidence

- Final source preserves the existing responsive top-header implementation and adds the focused model-section collision guard only.
- The compact-shell preflight now requires the exact `MÔ HÌNH`/`Làm mới` contract: `LastChildFill = false`, opposing DockPanel docks, no-wrap/ellipsis text, measured remaining-width cap and resize recomputation.
- Prior handler, viewport-boundary and no-business-side-effect checks remain in the same preflight.
- Negative-margin/Canvas overlay-style collision hacks remain rejected.
- No GitHub Actions were dispatched.

## LOCAL_ONLY boundary

Real BricsCAD V25 runtime visual qualification, Vietnamese text clipping and HiDPI/palette-width verification remain under existing `LOCAL-012`; this remote/source batch does not claim a licensed BricsCAD runtime PASS.

## Completion

Reservation released. The owner-demonstrated model-section header overlap fix is integrated into `main`; future agents must re-check current `main` and active claims before modifying these surfaces.
