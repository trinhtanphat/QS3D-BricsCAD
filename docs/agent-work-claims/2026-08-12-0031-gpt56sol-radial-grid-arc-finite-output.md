# Work claim — Radial Grid ARC finite output

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-radial-grid-arc-finite-output-20260812-0031`
- Registered: `2026-08-12T00:31:00+07:00`
- Baseline main SHA: `fe34c95c26bac556e649721618faceab400599c8`
- Priority: evidence-driven Core geometry hardening during owner-requested `continue all`

## Reserved scope

Make `GridSystemPlanner.PlanRadial` fail closed before returning a ring `GridReferenceCurve` whose computed ARC endpoints are non-finite despite finite center/radius inputs.

## Expected surfaces

- `src/QS3D.Core/Geometry/GridSystemPlanner.cs`
- isolated focused Core smoke regression
- this claim file for close-out

## Concrete defect

`PlanRadial` validates a finite center and finite positive ring radius, then delegates to `GridReferenceCurve.Arc`, whose endpoint construction performs `center + radius * cos/sin` without validating the computed points. Large same-sign finite center/radius values can therefore overflow an ARC endpoint to infinity, and `PlanRadial` returns an invalid curve object that only fails later in unrelated consumers.

## Explicit exclusions

- No Grid naming, station ordering, intersection, annotation, native V25, factory-wide `GridReferenceCurve` contract, UI, Actions, release, or LOCAL_PASS behavior changes.

## Validation plan

- Keep normal radial Grid generation unchanged.
- Validate each generated ring ARC start/end immediately in `PlanRadial` before adding it to output.
- Add focused smoke coverage with finite center/radius values that make the generated full-circle endpoint overflow; assert the planner rejects instead of returning a non-finite Grid curve.
- Re-fetch target source before implementation and do not overwrite concurrent edits.
- No GitHub Actions will be dispatched and no BricsCAD runtime PASS will be claimed from this web session.

## Completion condition

`PlanRadial` cannot return non-finite ring endpoints from finite but unrepresentable center/radius combinations, regression is integrated on current `main`, and this claim is marked `COMPLETED`.
