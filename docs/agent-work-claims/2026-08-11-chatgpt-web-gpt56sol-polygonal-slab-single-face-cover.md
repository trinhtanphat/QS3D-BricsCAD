# Agent work claim — polygonal slab single-face cover hardening

- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-11T20:37:00+07:00
- Status: `ACTIVE`
- Baseline main SHA: `4e23ff281857173eb6e642923a7f1cda8b22e007`
- Priority: source-safe Rebar numerical/geometry hardening; reject polygonal slab mesh layouts whose second stacked bar layer crosses the opposite concrete-cover boundary when only one slab face is enabled.

## Confirmed defect

`PolygonalSlabMeshPlanner.ResolveElevations(...)` validates a bottom-only mesh only against the bottom cover plane (`low`) and a top-only mesh only against the top cover plane (`high`). Because X/Y bars on one face are vertically stacked by `xRadius + yRadius`, a thin slab can pass the current single-face check even though the farther layer extends through the opposite face's cover zone. The dual-face branch already checks the full occupied depth, so the defect is isolated to single-face validation.

## Reserved scope

- `src/QS3D.Core/Rebar/PolygonalSlabMeshPlanner.cs`
- focused deterministic Core smoke/preflight coverage for this exact single-face cover invariant, using an existing rebar smoke surface when available or one narrowly scoped new auto-discovered preflight/smoke file if needed
- this claim file for close-out

## Functional contract

- bottom-only mesh must keep the entire occupied X/Y bar envelope between `-halfThickness + cover` and `halfThickness - cover`;
- top-only mesh must satisfy the same full slab usable-depth envelope;
- preserve existing dual-face separation behavior and X/Y closest-to-face ordering;
- fail closed for non-finite/degenerate inputs and do not relax existing spacing/count/topology limits;
- do not change CAD/native generation, UI, quantity formulas, persistence, or ownership models.

## Explicit exclusions / coordination

- no wall quantity takeoff window, Core reporting identity, Create Similar, Workspace/UI, command-boundary, Start Center, quantity-description viewport, or generated-source recognition surfaces owned by other active claims;
- no broad slab-mesh redesign or new reinforcement model;
- no GitHub Actions dispatch/re-run/release;
- no remote claim of BricsCAD V25 runtime PASS.

## Validation target

- deterministic regression proving a bottom-only thin slab that previously passed now fails before bar output;
- symmetric top-only regression;
- a valid single-face mesh still succeeds;
- existing dual-face behavior remains unchanged;
- re-fetch current `main` before implementation and inspect final diff/status after push.

## Completion condition

The single-face full-depth cover invariant is enforced and regression-locked on current `main`, source-safe validation evidence is recorded, runtime-only qualification remains local, and this claim is marked `COMPLETED` with the exact implementation SHA.