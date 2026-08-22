# Agent work claim — polygonal slab single-face cover hardening

- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-11T20:37:00+07:00
- Completed: 2026-08-11T21:17:00+07:00
- Status: `COMPLETED`
- Baseline main SHA: `4e23ff281857173eb6e642923a7f1cda8b22e007`
- Priority: source-safe Rebar numerical/geometry hardening; reject polygonal slab mesh layouts whose second stacked bar layer crosses the opposite concrete-cover boundary when only one slab face is enabled.

## Confirmed defect

`PolygonalSlabMeshPlanner.ResolveElevations(...)` validated a bottom-only mesh only against the bottom cover plane and a top-only mesh only against the top cover plane. Because X/Y bars on one face are vertically stacked by `xRadius + yRadius`, a thin slab could pass the old single-face check even though the farther layer crossed the opposite face's usable cover boundary. The dual-face branch already checked occupied-depth separation, so the defect was isolated to single-face validation.

## Implemented

- `a48702b7eeb5fd1ebb5f0186a9dfbffb891f9e68` — `fix(rebar): enforce slab single-face cover envelope`
  - computes the full occupied low/high envelope across both stacked X/Y bar layers;
  - bottom-only and top-only layouts now fail closed unless the complete bar envelope remains within `[-halfThickness + cover, halfThickness - cover]`;
  - preserves the existing dual-face separation branch, face ordering, spacing/count/topology guards, and native/CAD boundaries.
- `76a5485c9bfc9db3cdef52ac5a8cdf5baf7687bf` — `test(core): cover slab single-face mesh depth`
  - adds deterministic bottom-only and top-only thin-slab rejection cases;
  - retains a valid single-face success case and a valid dual-face success case;
  - uses the repository's existing `[ModuleInitializer]` smoke-registration pattern.

## Validation evidence

- Re-fetched `src/QS3D.Core/Rebar/PolygonalSlabMeshPlanner.cs` from current `main`; the committed source contains the full single-face low/high cover-envelope guard.
- Re-fetched the regression smoke from current `main`; it exercises the public planner API and checks bottom/top face behavior rather than only matching source text.
- `tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj` is SDK-style `net8.0`, so the new `.cs` smoke is included automatically.
- Existing `BeamRebarSmokeRegistration.cs` already uses the same `System.Runtime.CompilerServices.ModuleInitializer` registration pattern, confirming the registration mechanism is established in this smoke project.
- `main` advanced concurrently after the implementation/test commits; the planner fix and smoke file were re-fetched from the newer tree and remained intact.
- No GitHub Actions/release was dispatched. This connector-only lane does not claim that the smoke executable was run, and no BricsCAD V25 runtime claim is needed for this pure Core planner invariant.

## Reserved scope honored

- Changed only `src/QS3D.Core/Rebar/PolygonalSlabMeshPlanner.cs`, the focused Core smoke file, and this claim close-out.
- Did not change CAD/native generation, UI, quantity formulas, persistence, ownership models, wall takeoff, Workspace, Start Center, updater, or other concurrent lanes.

## Completion

Completed. The single-face full-depth cover invariant is enforced and regression-locked in source on `main`; exact implementation and regression SHAs are recorded above.