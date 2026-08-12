# Work claim — Curved opening projection overflow

- Status: `COMPLETED`
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

`Project` computed `denominator = segment.Length * segment.Length` and then used an unscaled dot product. A valid finite segment can have length above `sqrt(double.MaxValue)` while its endpoints, normalized direction and intended projection remain finite. The squared length then becomes infinity and the planner rejects otherwise representable geometry.

## Implementation

- `b5d1b4ba705768b93aa0f087a48c46f973a61b3a` — replace length-squared projection with finite normalized direction plus scale-safe dot product; preserve projection clamp, station, slicing and footprint semantics.
- `8fafe8e14df04901517d47db7a4a1567c421f073` — add focused smoke coverage using a finite `1e200` host segment whose squared length would overflow, asserting finite deterministic station, centerline and cutter footprint.

## Validation performed

- Re-fetched the target source after claim registration and confirmed the length-square projection was still present before editing.
- Re-fetched the committed source and confirmed `Project` now computes `ux/uy`, finite offsets and `DotFinite(...)` without forming `segment.Length * segment.Length`.
- Re-fetched the smoke fixture and confirmed it exercises a `1e200` segment with finite output assertions.
- Source/static validation only from this web session; no GitHub Actions dispatched and no BricsCAD V25 runtime/build/NETLOAD PASS claimed.

## Explicit exclusions retained

- No opening width/clearance policy, ambiguity policy, centerline slicing, wall-footprint construction, native V25 cut/materialization, Opening Boolean lifecycle, UI, Actions, release, or LOCAL_PASS behavior changes.

## Completion

Finite representable curved-host projection no longer fails solely because of an intermediate length-square overflow, focused regression is integrated on `main`, and this claim is closed.
