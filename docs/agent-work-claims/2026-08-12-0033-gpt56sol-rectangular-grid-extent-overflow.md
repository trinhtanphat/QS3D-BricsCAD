# Work claim — Rectangular Grid extent overflow

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-rectangular-grid-extent-overflow-20260812-0033`
- Registered: `2026-08-12T00:33:00+07:00`
- Baseline main SHA: `935bab2c0e2224429909a2838a83006cf215d29a`
- Priority: evidence-driven Core geometry hardening during owner-requested `continue all`

## Reserved scope

Make `GridSystemPlanner.ValidateExtent` reject rectangular Grid extents whose finite endpoints produce a non-finite span before the planner emits lines that downstream finite-length geometry cannot consume.

## Expected surfaces

- `src/QS3D.Core/Geometry/GridSystemPlanner.cs`
- isolated focused Core smoke regression
- this claim file for close-out

## Concrete defect

`ValidateExtent` currently tests only `max - min <= tolerance`. With finite values such as `min = -1e308` and `max = +1e308`, the subtraction overflows to positive infinity, so the comparison is false and the extent is accepted. `PlanRectangular` can then emit a LINE with finite endpoints whose endpoint delta/length is not representable, deferring failure into later Grid consumers.

## Explicit exclusions

- No Grid naming, station ordering, radial Grid, intersection, annotation, native V25, `GridReferenceCurve` factory-wide contract, UI, Actions, release, or LOCAL_PASS behavior changes.

## Validation plan

- Materialize the extent span once, require it to be finite and greater than tolerance.
- Preserve ordinary finite extent behavior.
- Add focused smoke coverage with a finite `[-1e308, +1e308]` U extent that previously produced an unrepresentable V-family Grid line.
- Re-fetch target source before implementation and do not overwrite concurrent edits.
- No GitHub Actions will be dispatched and no BricsCAD runtime PASS will be claimed from this web session.

## Completion condition

Rectangular Grid planning rejects non-finite extent spans at validation rather than emitting lines with unsupported endpoint deltas, focused regression is integrated on current `main`, and this claim is marked `COMPLETED`.
