# Work claim — Curved opening projection overflow

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-curved-opening-projection-overflow-20260812-0022`
- Registered: `2026-08-12T00:22:00+07:00`
- Baseline main SHA: `f249785e44482df20c4fa7324d96fc1a7df24c1d`
- Priority: evidence-driven Core numeric hardening during owner-requested `continue all`

## Reserved scope

Make `CurvedOpeningFootprintPlanner.Project` compute projection for finite long segments without squaring `segment.Length` into overflow.

## Expected surfaces

- `src/QS3D.Core/Geometry/CurvedOpeningFootprintPlanner.cs`
- isolated focused Core smoke regression
- this claim file for close-out

## Concrete defect

`Project` currently computes `denominator = segment.Length * segment.Length` and then uses an unscaled dot product. A valid finite segment can have length above `sqrt(double.MaxValue)` while its endpoints, normalized direction and intended projection remain finite. The squared length then becomes infinity and the planner rejects otherwise representable geometry.

## Explicit exclusions

- No opening width/clearance policy, ambiguity policy, centerline slicing, wall-footprint construction, native V25 cut/materialization, Opening Boolean lifecycle, UI, Actions, release, or LOCAL_PASS behavior changes.

## Validation plan

- Preserve projection/station behavior for ordinary inputs.
- Re-express projection using the already finite segment length and normalized direction so no length-square is required.
- Add focused large-coordinate/long-segment smoke coverage where `Length` is finite but `Length * Length` overflows; assert the curved opening plan remains finite and deterministic.
- Re-fetch target source before implementation and do not overwrite concurrent edits.
- No GitHub Actions will be dispatched and no BricsCAD runtime PASS will be claimed from this web session.

## Completion condition

Finite representable curved-host projection no longer fails solely because of an intermediate length-square overflow, regression is integrated on current `main`, and this claim is marked `COMPLETED` with exact implementation SHA(s) and validation performed.
