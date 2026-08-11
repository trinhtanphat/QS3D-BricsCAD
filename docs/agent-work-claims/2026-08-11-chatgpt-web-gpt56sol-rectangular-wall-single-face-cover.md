# Agent work claim — rectangular wall single-face cover hardening

- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-11T21:33:00+07:00
- Completed: 2026-08-11T21:36:00+07:00
- Status: `COMPLETED`
- Baseline main SHA: `0a8900d78705a4ab51aac14d9eab0e78fda6eeae`
- Priority: source-safe Rebar geometry hardening; reject single-face structural-wall meshes whose stacked second bar layer crosses the opposite concrete-cover boundary.

## Confirmed defect

`RectangularWallMeshPlanner.Plan(...)` validated two-face Near+Far separation, but Near-only and Far-only layouts had no full-depth concrete-cover-envelope validation. Because horizontal and vertical bars on a face are stacked by the two radii, a thin wall could return a valid-looking layout even when the farther stacked layer crossed the opposite usable cover plane.

## Implemented

- `66f29aeae19f891722b00fc98b6fab5380745d23` — `fix(rebar): enforce wall single-face cover envelope`
  - defines the usable wall depth between both concrete-cover planes;
  - Near-only and Far-only branches validate the complete occupied low/high envelope of both stacked bar directions;
  - preserves the existing two-face separation branch, closest-to-face ordering, spacing/count/bar-cap behavior, and finite guards.
- `650300db165f14f70fae688678cd4838bf57c5d7` — `test(core): cover wall single-face mesh depth`
  - adds deterministic Near-only/Far-only thin-wall rejection;
  - retains valid single-face and valid two-face success coverage;
  - uses the repository's established net8 Core smoke `[ModuleInitializer]` registration pattern.

## Validation evidence

- Re-fetched `src/QS3D.Core/Rebar/RectangularWallMeshPlanner.cs` from a newer current `main`; the full single-face low/high cover-envelope guards remain present.
- Re-fetched `tests/QS3D.Core.SmokeTests/RectangularWallMeshCoverRegressionSmoke.cs` from the same newer tree; the public-planner regression coverage remains intact.
- Current concurrent commits were on updater/repository-health lanes and did not overwrite the reserved Rebar surfaces.
- No GitHub Actions workflow was dispatched and no smoke executable run is claimed from this connector-only lane.
- No BricsCAD V25/native runtime claim is required for this pure Core planner invariant.

## Reserved scope honored

- Changed only `RectangularWallMeshPlanner.cs`, the focused wall Core smoke file, and this claim close-out.
- Did not change slab planners, CAD/native generation, UI, persistence, quantities/reporting, updater, Ribbon, Direct Draw, or other concurrent lanes.

## Completion

Completed. The rectangular-wall single-face full-depth cover invariant is enforced and regression-locked in source on `main`; exact implementation and test SHAs are recorded above.
