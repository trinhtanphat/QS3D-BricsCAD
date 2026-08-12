# Work claim — Rectangular Grid extent overflow

- Status: `COMPLETED`
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

`ValidateExtent` tested only `max - min <= tolerance`. With finite values such as `min = -1e308` and `max = +1e308`, the subtraction overflowed to positive infinity, so the comparison was false and the extent was accepted. `PlanRectangular` could then emit a LINE with finite endpoints whose endpoint delta/length was not representable, deferring failure into later Grid consumers.

## Implementation

- `b772a34c2d1fbf24c4665f386369252cfd62ef88` — materialize the rectangular extent span once, reject non-finite span with `OverflowException`, then apply the existing positive-span tolerance rule.
- `181cbbcf3b1c22c9b4af812758853f0391837c1c` — add focused smoke coverage for a finite `[-1e308, +1e308]` U extent that previously passed validation and could emit an unsupported V-family line.

## Validation performed

- Re-fetched target source after claim registration and confirmed `ValidateExtent` still used an unchecked `max - min` comparison before editing.
- Re-fetched committed source and confirmed finite-span validation occurs before the tolerance comparison.
- Re-fetched the smoke fixture and confirmed the overflow extent is rejected during rectangular Grid planning.
- Source/static validation only; no GitHub Actions dispatched and no BricsCAD V25 runtime/build/NETLOAD PASS claimed.

## Explicit exclusions retained

- No Grid naming, station ordering, radial Grid, intersection, annotation, native V25, `GridReferenceCurve` factory-wide contract, UI, Actions, release, or LOCAL_PASS behavior changes.

## Completion

Rectangular Grid planning now rejects non-finite extent spans at validation rather than emitting lines with unsupported endpoint deltas, focused regression is integrated on `main`, and this claim is closed.
