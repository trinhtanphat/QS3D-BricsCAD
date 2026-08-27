# QS3D Grid spatial ordering Core contract

Updated: 2026-08-26 (UTC+7)

`GridSpatialOrderingPlanner` provides CAD-independent deterministic ordering after a caller has already resolved tracked semantic Grid geometry into `GridReferenceCurve` values. The original contract remains the one **parallel LINE Grid family** rule; the reviewed extension adds an explicit mixed/radial policy without guessing architectural intent from selection order.

## Existing parallel LINE contract

`OrderParallelLines(...)` is compatibility-preserved. The caller supplies:

- a bounded set of unique semantic Grid IDs represented as `GridReferenceCurveKind.Line`;
- one explicit non-zero 2D ordering axis;
- an alignment tolerance;
- a projected-coordinate tolerance;
- optional ascending/descending direction.

For a common rectangular Grid family, vertical Grid LINEs can be ordered left-to-right with axis `(1, 0)` and horizontal Grid LINEs bottom-to-top with axis `(0, 1)`. Each LINE must be perpendicular to the ordering axis within tolerance. The planner projects both endpoints onto the normalized axis and uses their average projected coordinate, so different LINE extents do not change the coordinate for a valid parallel family.

Two LINEs that project to the same coordinate within tolerance are ambiguous and fail closed rather than being silently ordered by semantic ID.

## reviewed mixed LINE + ARC ordering

`OrderReviewedSet(...)` exists for a set that has already been reviewed as one ordering operation. The caller must provide all policy that geometry alone cannot safely infer:

- the explicit LINE ordering axis;
- the explicit reviewed radial center for every ARC in the set;
- an explicit group precedence of `LinesThenArcs` or `ArcsThenLines`;
- explicit ascending/descending choices for the LINE and ARC groups;
- alignment and coordinate tolerances.

The method first validates semantic IDs across the complete mixed set case-insensitively. LINEs are then delegated back through the canonical `OrderParallelLines(...)` implementation. ARCs must contain finite geometry, a positive radius above tolerance, a sweep in `(0, 2π]`, and a center matching the explicit reviewed radial center within tolerance. ARC ordering is by radius. Equal/near-equal radii within tolerance are ambiguous and fail closed.

Group precedence is never inferred from CAD pick order. For supported inputs, selection order does not define output order: permutations of the same reviewed set produce the same semantic-ID sequence for the same explicit policy.

`GridReviewedOrderingEntry` reports semantic ID, curve kind, explicit group index, and the spatial scalar used inside that group. This is planning evidence only; it is not a second Grid store or numbering engine.

## Fail-closed behavior

The planner rejects instead of guessing when:

- an ordering axis, reviewed ARC center, tolerance, endpoint or ARC parameter is non-finite/invalid;
- the input is empty or exceeds the 2,000-curve bound;
- semantic Grid IDs are duplicate case-insensitively, including across LINE/ARC groups;
- a LINE is degenerate, not perpendicular to the explicit axis, or collides with another projected coordinate within tolerance;
- an ARC has zero/near-zero radius, invalid sweep, a center inconsistent with the reviewed radial center, or a radius tie within tolerance;
- the requested mixed group precedence is unsupported;
- derived geometry/projection arithmetic exceeds the supported finite numeric range.

The planner intentionally refuses arbitrary spatial tie-breaks. Duplicate/overlapping Grid sources or unclear radial families require review rather than silent renumbering.

## Architecture boundary

This capability reuses `GridReferenceCurve` and `GridSpatialOrderingPlanner`; it does not create a second Grid catalog, semantic store, numbering engine, intersection engine or native annotation ownership layer. Pair-owned native intersection-marker lifecycle remains outside this lane (#3771).

The Core contract does **not** claim that an arbitrary CAD drawing can be auto-classified into architectural Grid families. A native caller must first resolve tracked semantic Grid sources and obtain/confirm the explicit axis, radial center and mixed group precedence. Any later renumber mutation must continue through the existing atomic Grid naming/annotation ownership path.

## Source checks

```text
python scripts/preflight-grid-spatial-ordering.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

`GridSpatialOrderingSmoke` preserves the previous parallel-LINE cases and adds reviewed mixed-set permutation invariance, explicit group precedence, radial-center mismatch, radius ambiguity, cross-kind duplicate ID and invalid-ARC regressions.

## Native/runtime handoff

Hosted evidence can establish the Core deterministic ordering contract but cannot establish licensed BricsCAD behavior. A future reviewed native command/UI route must, on one exact integrated source SHA:

1. resolve only tracked `ElementCategory.Grid` authoritative LINE/ARC sources;
2. show or explicitly obtain the LINE axis, radial center and group precedence before applying any order;
3. prove cancel/no-confirmation performs no semantic or CAD mutation;
4. prove permutations of native selection produce the same reviewed plan;
5. reject duplicate/ambiguous/non-finite sources without partial renumbering;
6. route any approved renumber through the canonical atomic Grid naming + annotation ownership path;
7. qualify UNDO/REDO, save/reopen and multi-DWG isolation in licensed V25/V26 as applicable.

Until that host qualification is produced from the exact integrated SHA, runtime status is **PENDING_LOCAL**. No hosted build, smoke test or source guard may be promoted to `LOCAL_PASS`.

Describe the merged source capability as **Core spatial-order planning** with an explicit reviewed mixed/radial ordering policy, not as autonomous CAD Grid renumbering.