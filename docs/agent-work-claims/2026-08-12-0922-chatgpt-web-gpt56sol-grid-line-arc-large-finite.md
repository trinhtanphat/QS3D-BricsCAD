# Work claim — Grid LINE/ARC large finite intersection arithmetic

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-grid-line-arc-large-finite-20260812-0922`
- Registered: `2026-08-12T09:22:00+07:00`
- Baseline main SHA: `ed2448e545ffaf43422afe57bf02ba007cc2da64`
- Priority: evidence-driven Core geometry correctness during owner-requested `continue all`

## Confirmed defect

`GridIntersectionPlanner.IntersectLineArc(...)` validates that LINE/ARC deltas are finite, but then forms the quadratic with raw squared/multiplied world-space values. Large finite geometry (for example coordinates/radius around `1e200`) can therefore overflow `a`, `b`, `c`, or the discriminant to infinity even though the mathematical intersection and the already-validated deltas are finite.

Rereview of the first normalization fix found a compatibility edge: always normalizing by world-space magnitude can underflow a very small but still representable LINE direction, changing ordinary finite behavior. The follow-up therefore keeps the raw quadratic as the compatibility fast-path and only retries with common-scale normalization when raw coefficients/discriminant become non-finite from avoidable overflow.

## Reserved scope

- `src/QS3D.Core/Geometry/GridIntersectionPlanner.cs` — `IntersectLineArc(...)` large-finite quadratic arithmetic only.
- `tests/QS3D.Core.SmokeTests/GridIntersectionLineArcLargeFiniteSmoke.cs` — focused CAD-independent regression.
- `tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs` — register only the dedicated Grid regression above.
- this claim file.

## Contract

Keep existing input validation, finite-derived fail-closed behavior, root ordering/filtering, arc sweep filtering, deduplication, tolerance semantics and result ordering. Preserve the legacy/raw quadratic whenever its derived values remain finite. If raw squared products or discriminant overflow, retry the same quadratic after a finite common geometry scale before squared products, with the discriminant tolerance transformed consistently. Do not make always-normalized arithmetic the ordinary path.

## Coordination

The active Grid renumber read-only-result lane owns `GridNamingService.cs` and `GridRenumberReadOnlyResultSmoke.cs`, not this planner or smoke path. Current claim/commit inspection found no live ownership of `GridIntersectionPlanner.cs`, the dedicated regression path, or `SmokeTestRegistration.cs`; exact commit search for `SmokeTestRegistration` only surfaced a historical 2026-08-10 merge conflict.

## Integration

- Claim expansion: `2d340a93ec16b41bcbb32555162c7e7699ce7075`
- Product follow-up: `2d989fb24b465c77a2803dca77b00575f2047eb4`
- Focused regression: `591e1a8916cc79e61c882371b4b3415b5449a214`
- Smoke registration: `85d94c21a5b1edcc09209ccdfdf66ea57f5ed5db`
- Product diff readback confirmed only `IntersectLineArc(...)` changed in `GridIntersectionPlanner.cs`.
- Numeric rereview confirmed the large-finite fallback roots are `0.25` and `0.75`, while the raw finite near-endpoint tangent preserves root `0.0` and discards the far root outside the segment.
- No GitHub Actions dispatched; no BricsCAD/Windows runtime PASS claimed.

## Validation plan

The dedicated registered smoke covers finite LINE/ARC geometry around `1e200` whose expected intersections are finite but whose unscaled squared terms overflow, plus a raw-finite compatibility case with highly asymmetric geometry. Runtime execution remains intentionally unclaimed because GitHub Actions/BricsCAD validation was not dispatched in this lane.
