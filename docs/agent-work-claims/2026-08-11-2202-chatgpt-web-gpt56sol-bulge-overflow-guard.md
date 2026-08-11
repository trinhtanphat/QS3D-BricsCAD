# Work claim — Bulge tessellation overflow guard

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T22:02:00+07:00`
- Baseline main SHA: `f7cefccf06e5bae85e7d008d37e89fc110039862`
- Priority: continue-all remote-safe Core geometry correctness

## Reserved scope

Harden `BulgeArcTessellator` against finite-but-extreme bulge values whose squared term overflows before radius/center computation. The current formula evaluates `absBulge * absBulge`; sufficiently large finite bulges therefore become `Infinity` and are rejected even though the corresponding polyline arc has a finite limiting geometry for ordinary finite chords.

## Expected surfaces

- `src/QS3D.Core/Geometry/BulgeArcTessellator.cs`
- `tests/QS3D.Core.SmokeTests/RoomBoundaryRegressionSmoke.cs`
- this claim file

## Excluded scope

- No BricsCAD V25/native runtime or UI work.
- No polygon mesh, Curtain, Direct Draw, updater, reporting, persistence, formula, documentation-table, or release changes.
- No change to the existing 4096 tessellation segment bound or invalid/non-finite input policy.
- No GitHub Actions dispatch.

## Validation plan

- Add deterministic Core regression proving an extreme finite bulge with an ordinary finite chord does not overflow intermediate radius/center arithmetic.
- Preserve endpoints, finite tessellated points, segment bound, sagitta behavior, and existing invalid-input failures.
- Re-fetch current `main` and target files before writes; never force-push.

## Coordination

This lane is limited to numeric overflow resistance inside the existing bulge arc primitive and its existing room-boundary regression suite; it does not take neighboring active geometry/product lanes.

## Completion condition

The overflow-safe formulation and regression are pushed to current `main`, then this claim is marked `COMPLETED` with the resulting SHAs and validation actually performed.
