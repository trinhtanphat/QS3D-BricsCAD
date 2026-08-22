# Agent work claim — rectangular slab single-face cover hardening

- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-11T21:27:00+07:00
- Completed: 2026-08-11T21:29:00+07:00
- Status: `COMPLETED`
- Baseline main SHA: `710e692d20aacfd294db41660b6ee128019025cd`
- Priority: source-safe Rebar geometry hardening; reject rectangular slab mesh layouts whose second stacked bar layer crosses the opposite concrete-cover boundary when only one slab face is enabled.

## Confirmed defect

`RectangularSlabMeshPlanner.Plan(...)` computed stacked X/Y elevations for each slab face, but its single-face branch checked only the near cover boundary: bottom-only validated the lower occupied edge and top-only validated the upper occupied edge. A thin slab could therefore pass while the farther stacked layer crossed the opposite usable cover plane.

## Implemented

- `9c840b471bd37714b85ae18f6fe34809ac107527` — `fix(rebar): enforce rectangular slab single-face cover`
  - defines the usable slab depth between both concrete-cover planes;
  - bottom-only and top-only branches now validate the complete occupied low/high envelope of both stacked bar directions;
  - leaves the existing dual-face separation branch and all distribution/bar-cap behavior unchanged.
- `ef67a548f05cf3b00b3271ea957886d4b5bd062f` — `test(core): cover rectangular slab mesh depth`
  - adds deterministic bottom-only/top-only thin-slab rejection;
  - keeps valid single-face and dual-face success coverage;
  - uses the established net8 Core smoke `[ModuleInitializer]` registration pattern.

## Validation evidence

- Re-fetched `src/QS3D.Core/Rebar/RectangularSlabMeshPlanner.cs` from newer current `main`; the full single-face low/high cover-envelope guards remain present.
- Re-fetched `tests/QS3D.Core.SmokeTests/RectangularSlabMeshCoverRegressionSmoke.cs` from newer current `main`; regression coverage remains intact.
- The Core smoke project is SDK-style `net8.0`, and the repository already uses `[ModuleInitializer]` registration for Rebar smoke files.
- No GitHub Actions workflow was dispatched and no smoke-executable run is claimed from this connector-only lane.
- No BricsCAD V25/native runtime claim is required for this pure Core planner invariant.

## Reserved scope honored

- Changed only `RectangularSlabMeshPlanner.cs`, the focused rectangular slab Core smoke file, and this claim close-out.
- Did not change polygonal slab, wall mesh, CAD/native generation, UI, persistence, quantities, updater, Ribbon, Direct Draw, or other concurrent lanes.

## Completion

Completed. The rectangular single-face full-depth cover invariant is enforced and regression-locked in source on `main`; exact implementation and test SHAs are recorded above.
