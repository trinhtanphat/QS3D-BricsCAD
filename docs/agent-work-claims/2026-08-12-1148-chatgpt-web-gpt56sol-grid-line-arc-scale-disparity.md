# Work claim — Grid LINE/ARC overflow fallback scale disparity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-grid-line-arc-scale-disparity-20260812-1148`
- Registered: `2026-08-12T11:48:00+07:00`
- Baseline main SHA: `5552fae484d48eac4d9af446abd660b88687dbd9`
- Priority: P1 — large finite LINE/ARC intersections must not fail because a common normalization scale underflows the finite LINE direction.

## Confirmed defect

The completed large-finite LINE/ARC fix preserves the raw quadratic whenever it is finite and falls back to common-scale normalization only after raw coefficient/discriminant overflow. That fallback still has a scale-disparity hole: for a finite LINE around `1e-8` against a finite tangent ARC whose center/radius are around `1e200`, raw `c` becomes `Infinity - Infinity`, forcing fallback; dividing the LINE direction by the world/circle scale produces about `1e-208`, whose square underflows to zero. The fallback then throws `Grid LINE/ARC quadratic direction is outside the supported numeric range` although the segment has a well-defined endpoint intersection.

## Reserved scope

- `src/QS3D.Core/Geometry/GridIntersectionPlanner.cs` — `IntersectLineArc(...)` overflow fallback only.
- `tests/QS3D.Core.SmokeTests/GridIntersectionLineArcLargeFiniteSmoke.cs` — extend the already-registered focused regression only.
- this claim file.

No smoke-registration edit is required because `GridIntersectionLineArcLargeFiniteSmoke.Run()` is already registered on `main`.

## Intended contract

- Keep the raw finite quadratic fast-path unchanged.
- When raw arithmetic is non-finite, normalize the LINE direction by its own finite component scale and the line-origin/circle geometry by a separate finite geometry scale.
- Solve the fallback in the scaled distance parameter and convert roots back to the original segment parameter without overflow/underflow where a representable in-segment root exists.
- Preserve root ordering/filtering, arc sweep filtering, deduplication, input validation, and normal finite behavior.
- Add a regression for a short finite LINE tangent at its endpoint to a circle around `1e200`, which currently enters fallback and underflows the common-scaled direction.

## Coordination

The prior Grid LINE/ARC claim is completed. Recent commit search found no newer `GridIntersectionPlanner` ownership, and current active lanes visible on `main` concern ProjectElement quantity dirty propagation and unrelated revision/health work.

## Validation

Read back exact source/test diffs after each write. Do not dispatch GitHub Actions and do not claim BricsCAD/Windows runtime PASS.
