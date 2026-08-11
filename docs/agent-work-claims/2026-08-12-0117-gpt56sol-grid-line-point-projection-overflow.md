# Work claim — Grid LINE point projection overflow

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-grid-line-point-projection-overflow-20260812-0117`
- Registered: `2026-08-12T01:17:00+07:00`
- Baseline main SHA: `df8ee6865e9fcd3e1b80ba6abc535098a960af03`
- Priority: evidence-driven Core numeric hardening during owner-requested `continue all`

## Reserved scope

Make `GridIntersectionPlanner.IsOnLineSegment` avoid `length^2` and raw dot-product overflow when validating representable points on very long LINE references.

## Expected surfaces

- `src/QS3D.Core/Geometry/GridIntersectionPlanner.cs`
- isolated focused Core smoke regression
- this claim file for close-out

## Concrete defect

The collinear/shared-endpoint LINE path eventually calls `IsOnLineSegment`, which currently evaluates `px*dx + py*dy` and `dx*dx + dy*dy`. For a valid LINE around `1e160`, `length` remains finite while `length^2` overflows near `1e320`. A representable shared endpoint can therefore fail with `OverflowException` even though membership is exactly decidable from projection onto the already finite unit direction.

## Explicit exclusions

- No LINE/LINE determinant/cross-tolerance logic, LINE/ARC or ARC/ARC math, ambiguity policy, default tolerance, curve validation/cardinality, identity/ownership, native V25 inspection, UI, Actions, release, or LOCAL_PASS behavior changes.

## Validation plan

- Preserve the existing perpendicular-distance cross check.
- Replace squared-length/dot parameter bounds with a scale-safe projection onto the unit LINE direction and equivalent distance-domain bounds `[-tolerance, length+tolerance]`, avoiding overflow in the upper bound by comparing any beyond-end residual separately.
- Add public coverage for two collinear `1e160` LINEs sharing exactly one endpoint with explicit `1e-15` tolerance; require one finite shared-endpoint intersection instead of numeric overflow.
- Re-fetch target source before implementation and do not overwrite concurrent edits.
- No GitHub Actions will be dispatched and no BricsCAD runtime PASS will be claimed from this web session.

## Completion condition

Representable points on long Grid LINEs no longer fail solely on dot/length-square overflow while off-segment tolerance semantics remain equivalent, focused regression is integrated on current `main`, and this claim is marked `COMPLETED`.
