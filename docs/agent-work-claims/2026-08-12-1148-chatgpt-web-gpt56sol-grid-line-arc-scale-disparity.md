# Work claim — Grid LINE/ARC overflow fallback scale disparity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-grid-line-arc-scale-disparity-20260812-1148`
- Registered: `2026-08-12T11:48:00+07:00`
- Completed: `2026-08-12T11:53:00+07:00`
- Baseline main SHA: `5552fae484d48eac4d9af446abd660b88687dbd9`
- Claim SHA: `9a1b6d914dff68c3546743cf4205f29c8ea14491`
- Product SHA: `aeee22ed205215307041cf1001ed3cd17bcf0580`
- Regression SHA: `ee17eb960a54900c387c3b990beaddf37d700045`
- Priority: P1 — large finite LINE/ARC intersections must not fail because a common normalization scale underflows the finite LINE direction.

## Confirmed defect

The completed large-finite LINE/ARC fix preserved the raw quadratic whenever it was finite and fell back to common-scale normalization only after raw coefficient/discriminant overflow. That fallback still had a scale-disparity hole: for a finite LINE around `1e-8` against a finite tangent ARC whose center/radius are around `1e200`, raw `c` becomes `Infinity - Infinity`, forcing fallback; dividing the LINE direction by the world/circle scale produced about `1e-208`, whose square underflowed to zero. The fallback then threw `Grid LINE/ARC quadratic direction is outside the supported numeric range` although the segment has a well-defined endpoint intersection.

## Reserved scope

- `src/QS3D.Core/Geometry/GridIntersectionPlanner.cs` — `IntersectLineArc(...)` overflow fallback only.
- `tests/QS3D.Core.SmokeTests/GridIntersectionLineArcLargeFiniteSmoke.cs` — extend the already-registered focused regression only.
- this claim file.

No smoke-registration edit was required because `GridIntersectionLineArcLargeFiniteSmoke.Run()` was already registered on `main`.

## Implemented contract

- The raw finite quadratic fast-path remains unchanged.
- Overflow fallback now normalizes the LINE direction by its own finite component scale and the line-origin/circle geometry by a separate finite geometry scale.
- Fallback roots are solved in scaled-distance coordinates. Segment filtering/clamping is performed against the LINE component scale, and accepted points are reconstructed from the scaled displacement instead of converting through a potentially underflowing/overflowing original `t` parameter.
- Root ordering/filtering, arc sweep filtering, deduplication, input validation, and normal finite behavior remain intact.
- The focused smoke now includes a `2e-8` LINE tangent at its start point to a circle centered/radius `1e200`; raw arithmetic is non-finite, while the two-scale fallback produces roots `0` and `2`, accepts the endpoint root and rejects the far root.

## Coordination

The prior Grid LINE/ARC claim is completed. Recent commit search found no newer `GridIntersectionPlanner` ownership before this claim was published. Concurrent changes observed while implementing were on unrelated ProjectElement, documentation, revision, health, and dependency-impact lanes.

## Validation

- Product commit readback shows only the reserved `IntersectLineArc(...)` fallback changed.
- Source readback on later `main` confirms the two-scale fallback is retained.
- Regression readback on later `main` confirms `ScaleDisparityFallbackPreservesEndpointIntersection()` is present in the already-registered smoke.
- Independent numeric rereview of the regression input confirms raw `c`/discriminant are non-finite; fallback coefficients are `a=1`, `b=-2`, `c=0`, discriminant `4`, roots `0` and `2`, with only the zero-distance root inside the short LINE segment.
- No GitHub Actions were dispatched and no BricsCAD/Windows runtime/build PASS is claimed.
