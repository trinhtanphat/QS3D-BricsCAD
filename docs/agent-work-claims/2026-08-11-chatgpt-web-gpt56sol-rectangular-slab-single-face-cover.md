# Agent work claim — rectangular slab single-face cover hardening

- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-11T21:27:00+07:00
- Status: `ACTIVE`
- Baseline main SHA: `710e692d20aacfd294db41660b6ee128019025cd`
- Priority: source-safe Rebar geometry hardening; reject rectangular slab mesh layouts whose second stacked bar layer crosses the opposite concrete-cover boundary when only one slab face is enabled.

## Confirmed defect

`RectangularSlabMeshPlanner.Plan(...)` computes stacked X/Y elevations for each slab face. Its single-face branch checks only the near cover boundary: bottom-only validates the lower occupied edge and top-only validates the upper occupied edge. A thin slab can therefore pass while the farther stacked layer crosses the opposite usable cover plane.

## Reserved scope

- `src/QS3D.Core/Rebar/RectangularSlabMeshPlanner.cs`
- `tests/QS3D.Core.SmokeTests/RectangularSlabMeshCoverRegressionSmoke.cs`
- this claim file for close-out

## Functional contract

- bottom-only and top-only meshes must keep the entire occupied X/Y bar envelope within `[-halfThickness + cover, halfThickness - cover]`;
- preserve existing dual-face separation, X/Y closest-to-face ordering, spacing/count/bar-cap behavior, and finite-input guards;
- do not change polygonal slab code, wall mesh, CAD/native generation, UI, persistence, quantities, updater, Ribbon, or other concurrent lanes.

## Validation target

- deterministic bottom-only thin-slab rejection;
- symmetric top-only rejection;
- valid single-face layout still succeeds;
- valid dual-face layout still contains both faces;
- use the existing net8 Core smoke `[ModuleInitializer]` registration pattern;
- no GitHub Actions dispatch and no claim of remote BricsCAD V25 runtime PASS.

## Completion condition

The rectangular single-face full-depth cover invariant is enforced and regression-locked on current `main`, source evidence is re-fetched after concurrent updates, and this claim is marked `COMPLETED` with exact implementation/test SHAs.
