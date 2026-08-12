# Work claim — Bulge Arc read-only result parity

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T08:43:00+07:00`
- Baseline main SHA observed: `f746a372a7782d38436deb86e55ebe72e381e125`
- Priority: P2 — Core API result immutability consistency

## Confirmed defect

`BulgeArcTessellator.Tessellate(...)` advertises `IReadOnlyList<Point2>`, and the curved-arc path returns `List<Point2>.AsReadOnly()`, but the straight/near-zero-bulge fast path returns a raw `Point2[]`. Callers can therefore cast only the straight-path result back to an array and mutate it, making the public read-only contract depend on input shape.

## Reserved scope

- `src/QS3D.Core/Geometry/BulgeArcTessellator.cs` straight fast-path result wrapper only
- focused Core smoke regression under `tests/QS3D.Core.SmokeTests/`
- `docs/plans/2026-08-12-bulge-arc-readonly-result.md`
- this claim file

## Contract

1. Straight and curved tessellation results expose consistent non-mutable collection semantics.
2. Straight path still returns exactly `[start, end]` in the same order.
3. Curved path values, sagitta segmentation, segment limits, overflow guards and geometric calculations are unchanged.
4. No BricsCAD/native CAD behavior or release workflow changes.

## Validation boundary

Add focused deterministic Core smoke coverage for straight-path value parity and mutation rejection, plus curved-path non-regression. No GitHub Actions dispatch and no licensed BricsCAD runtime PASS claim.
