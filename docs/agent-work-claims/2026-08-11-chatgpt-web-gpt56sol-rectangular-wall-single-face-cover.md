# Agent work claim — rectangular wall single-face cover hardening

- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-11T21:33:00+07:00
- Status: `ACTIVE`
- Baseline main SHA: `0a8900d78705a4ab51aac14d9eab0e78fda6eeae`
- Priority: source-safe Rebar geometry hardening; reject single-face structural-wall meshes whose stacked second bar layer crosses the opposite concrete-cover boundary.

## Confirmed defect

`RectangularWallMeshPlanner.Plan(...)` validates two-face Near+Far separation, but Near-only and Far-only layouts have no full-depth concrete-cover-envelope validation. Because horizontal and vertical bars on a face are stacked by the two radii, a thin wall can return a valid-looking layout even when the farther stacked layer crosses the opposite usable cover plane.

## Reserved scope

- `src/QS3D.Core/Rebar/RectangularWallMeshPlanner.cs`
- `tests/QS3D.Core.SmokeTests/RectangularWallMeshCoverRegressionSmoke.cs`
- this claim file for close-out

## Functional contract

- Near-only and Far-only meshes must keep the full occupied horizontal/vertical bar envelope inside `[-halfThickness + cover, halfThickness - cover]`;
- preserve existing two-face separation, closest-to-face ordering, spacing/count/bar-cap behavior, and finite-input guards;
- do not change slab planners, CAD/native generation, UI, persistence, quantities/reporting, updater, Ribbon, Direct Draw, or other active claims.

## Validation target

- deterministic Near-only thin-wall rejection;
- symmetric Far-only rejection;
- a valid single-face mesh still succeeds;
- a valid two-face mesh still contains both faces;
- use the established net8 Core smoke `[ModuleInitializer]` registration pattern;
- no GitHub Actions dispatch and no remote BricsCAD V25 runtime PASS claim.

## Completion condition

The rectangular-wall single-face full-depth cover invariant is enforced and regression-locked on current `main`, source evidence is re-fetched after concurrent updates, and this claim is marked `COMPLETED` with exact implementation/test SHAs.
