# Work claim — Grid ARC sweep-boundary roundoff

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-grid-arc-sweep-roundoff-20260812-1206`
- Registered: `2026-08-12T12:06:00+07:00`
- Completed: `2026-08-12T12:09:00+07:00`
- Baseline main SHA: `e6f56ebe33a331ff4abaa1588566551752432296`
- Claim SHA: `6f815d617dfc9b686176f32400931bf1ee49046d`
- Product SHA: `5621beab54a935ef46a49d618959c632977f54c3`
- Regression SHA: `57445e364e3c8cd5bdf5322e87c5d8409e8bcbf7`
- Priority: P1 — valid ARC endpoint intersections must not be rejected by sub-ulp angular reconstruction error at large finite radius.

## Confirmed defect

`GridIntersectionPlanner.IsOnArc(...)` derived `angularTolerance = tolerance / max(radius, tolerance)`. At very large radius this became effectively zero compared with the rounding error of `Atan2` and normalized-angle arithmetic. A support-circle intersection mathematically on an ARC sweep endpoint could therefore be rejected because the reconstructed angle was one or a few binary64 ulps above the stored sweep endpoint.

Concrete counterexample: first radius `1e200`, second radius `5e199`, center separation `6e199` directed at `0.05` rad. The lower support-circle intersection is the endpoint of a first ARC starting at `0` with sweep `5.943424574382111`. Reconstructed `Atan2` is `5.943424574382112`, about `8.88e-16` rad above the endpoint, while the previous angular tolerance was about `1e-208`.

## Reserved scope

- `src/QS3D.Core/Geometry/GridIntersectionPlanner.cs` — `IsOnArc(...)` angular tolerance only, reusing the existing private numeric-precision constant.
- `tests/QS3D.Core.SmokeTests/GridIntersectionPlannerSmoke.cs` — focused large-finite sweep-endpoint regression only.
- this claim file.

No smoke registration edit was required.

## Implemented contract

- Existing world-space angular tolerance conversion remains the minimum semantic tolerance.
- Angular comparisons now also use the established `3.5527136788005009e-15` machine-precision floor.
- The same computed angular tolerance is used both for the near-full-circle shortcut and sweep-endpoint comparison.
- Radial membership, intersection construction, ordering, deduplication and public API remain unchanged.

## Regression

`LargeFiniteArcSweepEndpointAllowsAngularRoundoff()` covers the concrete large-finite pair. The first ARC starts at zero and ends at sweep `5.943424574382111`; both the interior support-circle intersection and the endpoint intersection must survive sweep filtering. Expected points are approximately `(9.04853550665272e199, 4.257229754763658e199)` and `(9.428344310654162e199, -3.3326151232561086e199)`.

## Validation

- Product diff readback confirms only the angular-tolerance calculation/order inside `IsOnArc(...)` changed.
- The focused regression was added to the already-registered Grid smoke; no registration file was modified.
- Independent numeric reproduction measured the endpoint reconstruction difference at about `8.88e-16` rad, below the 16-epsilon precision floor and vastly above the previous `~1e-208` angular tolerance.
- No GitHub Actions were dispatched and no BricsCAD/Windows runtime/build PASS is claimed.
