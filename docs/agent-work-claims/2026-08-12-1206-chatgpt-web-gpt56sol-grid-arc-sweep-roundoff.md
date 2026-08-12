# Work claim — Grid ARC sweep-boundary roundoff

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-grid-arc-sweep-roundoff-20260812-1206`
- Registered: `2026-08-12T12:06:00+07:00`
- Baseline main SHA: `e6f56ebe33a331ff4abaa1588566551752432296`
- Priority: P1 — valid ARC endpoint intersections must not be rejected by sub-ulp angular reconstruction error at large finite radius.

## Confirmed defect

`GridIntersectionPlanner.IsOnArc(...)` derives `angularTolerance = tolerance / max(radius, tolerance)`. At very large radius this can be effectively zero compared with the rounding error of `Atan2` and normalized-angle arithmetic. A support-circle intersection that is mathematically exactly on an ARC sweep endpoint can therefore be rejected because the reconstructed angle is one or a few binary64 ulps above the stored sweep endpoint.

Concrete counterexample: first radius `1e200`, second radius `5e199`, center separation `6e199` directed at `0.05` rad. The lower support-circle intersection is the endpoint of a first ARC starting at `0` with sweep `5.943424574382111`. Reconstructed `Atan2` is `5.943424574382112`, about `8.88e-16` rad above the endpoint, while the existing angular tolerance is about `1e-208`. The endpoint is falsely excluded.

## Reserved scope

- `src/QS3D.Core/Geometry/GridIntersectionPlanner.cs` — `IsOnArc(...)` angular tolerance only, reusing the existing private numeric-precision constant.
- `tests/QS3D.Core.SmokeTests/GridIntersectionPlannerSmoke.cs` — focused large-finite sweep-endpoint regression only.
- this claim file.

No smoke registration edit is required.

## Intended contract

- Preserve the existing world-space angular tolerance conversion as the minimum semantic tolerance.
- Add the already-established small machine-precision floor for angular comparisons only.
- Use the same angular tolerance for the near-full-circle shortcut and sweep endpoint comparison.
- Do not change radial membership, intersection construction, ordering, deduplication, or public API.

## Coordination

The radial-membership roundoff lane is completed and introduced the shared precision constant. No concurrent `IsOnArc` ownership surfaced; current interchange/selection/regeneration work owns unrelated source paths.

## Validation

Add a regression requiring both the interior support intersection and exact sweep-endpoint intersection to be returned. Read back exact diffs, close with exact SHAs, do not dispatch GitHub Actions, and do not claim BricsCAD/Windows runtime/build PASS.
