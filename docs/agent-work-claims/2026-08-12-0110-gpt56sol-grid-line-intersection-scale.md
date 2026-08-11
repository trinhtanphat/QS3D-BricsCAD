# Work claim — Grid LINE intersection scale

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-grid-line-intersection-scale-20260812-0110`
- Registered: `2026-08-12T01:10:00+07:00`
- Baseline main SHA: `388de3818354b7e0849fc82bca896ea92cb7b49b`
- Priority: evidence-driven Core numeric hardening during owner-requested `continue all`

## Reserved scope

Make `GridIntersectionPlanner` LINE/LINE determinant and cross-tolerance evaluation accept representable finite results even when raw component or length-product intermediates overflow.

## Expected surfaces

- `src/QS3D.Core/Geometry/GridIntersectionPlanner.cs`
- isolated focused Core smoke regression
- this claim file for close-out

## Concrete defect

For two finite LINE references around `1e160`, the true cross product can remain finite through cancellation and an explicitly small tolerance can make `tolerance * |r| * |s|` finite. The current code first evaluates raw component products in `Cross` and materializes `rLength * sLength`; either intermediate can overflow near `1e320` before cancellation or tolerance scaling, causing a false `OverflowException` even though the requested intersection calculation and result remain representable.

## Explicit exclusions

- No LINE/ARC or ARC/ARC quadratic/circle math, ambiguity policy, default tolerance, curve validation/cardinality, identity/ownership, native V25 inspection, UI, Actions, release, or LOCAL_PASS behavior changes.

## Validation plan

- Make the shared determinant helper scale-safe while still throwing when the final determinant is non-finite.
- Compute LINE/LINE cross tolerance by multiplying the smallest magnitude factors first so a finite final tolerance does not require materializing `|r|*|s|` first; still fail closed if the final tolerance is non-finite.
- Add focused public coverage for two `1e160` near-parallel LINEs crossing near their midpoints with explicit `1e-15` tolerance; raw length product/component products overflow, while determinant, tolerance and intersection remain finite.
- Re-fetch target source before implementation and do not overwrite concurrent edits.
- No GitHub Actions will be dispatched and no BricsCAD runtime PASS will be claimed from this web session.

## Completion condition

Representable LINE/LINE intersections no longer fail solely on avoidable intermediate scale overflow, while truly non-finite determinant/tolerance results remain fail-closed, focused regression is integrated on current `main`, and this claim is marked `COMPLETED`.
