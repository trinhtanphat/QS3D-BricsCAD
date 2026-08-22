# QS3D Grid spatial ordering Core contract

Updated: 2026-08-10 (UTC+7)

`GridSpatialOrderingPlanner` provides a CAD-independent ordering rule for one **parallel LINE Grid family** after a caller has already resolved tracked semantic Grid geometry into `GridReferenceCurve` values.

It exists to remove arbitrary pick-order dependence from future automatic Grid renumbering without pretending that Core can infer every architectural Grid-system convention.

## Supported contract

The caller supplies:

- a bounded set of unique semantic Grid IDs represented as `GridReferenceCurveKind.Line`;
- one explicit non-zero 2D ordering axis;
- an alignment tolerance;
- a projected-coordinate tolerance;
- optional ascending/descending direction.

For a common rectangular Grid family:

- vertical Grid LINEs can be ordered left-to-right with axis `(1, 0)`;
- horizontal Grid LINEs can be ordered bottom-to-top with axis `(0, 1)`.

Each input LINE must be perpendicular to the ordering axis within the supplied alignment tolerance. The planner projects both endpoints onto the normalized axis and uses the average projected coordinate. Different LINE extents therefore do not change the ordering coordinate for a valid parallel family.

The result is a deterministic ordered list of semantic Grid IDs plus their resolved scalar coordinate.

## Fail-closed behavior

The planner rejects instead of guessing when:

- the ordering axis is zero, non-finite or cannot be normalized safely;
- the input is empty or exceeds the bounded curve count;
- semantic Grid IDs are duplicate case-insensitively;
- an input curve is not a LINE;
- a LINE is degenerate or has non-finite coordinates;
- a LINE is not perpendicular to the explicit ordering axis within tolerance;
- two LINEs project to the same coordinate within tolerance;
- derived projection/delta arithmetic leaves the supported finite numeric range.

The projected-coordinate tie is intentionally an error rather than an element-ID tie-break. Near-duplicate/overlapping Grid lines need review because silently choosing one label order would hide model ambiguity.

## What this does not solve

This Core slice does **not** claim:

- native BricsCAD LINE/ARC extraction;
- choosing the ordering axis automatically from CAD;
- mixed LINE + ARC ordering;
- radial Grid ordering;
- rectangular/radial Grid-system grouping;
- native bubble placement;
- automatic renumber command/UI;
- structure movement/snapping/constraints;
- exact V25 runtime qualification.

ARC/radial ordering needs a separate reviewed policy. A radial system may reasonably order by angle or radius depending on user intent, so Core must not invent that convention from geometry alone.

## Intended native integration

A future V25 automatic renumber command should:

1. resolve only tracked `ElementCategory.Grid` source entities;
2. convert native geometry into the existing `GridReferenceCurve` contract;
3. let the user choose or visibly confirm the ordering axis/family;
4. call `GridSpatialOrderingPlanner`;
5. present/review the ordered semantic IDs;
6. call the existing atomic `GridNamingService.Renumber(...)` path;
7. update owned Grid annotations only through the canonical annotation ownership/replacement layer;
8. preserve project/CAD rollback and post-commit UI boundaries.

Do not combine automatic spatial ordering with structural relocation or engineering constraints.

## Source checks

```text
python scripts/preflight-grid-spatial-ordering.py
```

`GridSpatialOrderingSmoke` covers ascending/descending order plus fail-closed non-parallel, ARC, duplicate-ID, ambiguous-coordinate and invalid-axis cases. The repository-wide smoke-registration preflight must continue to discover and register this smoke.

## Local/runtime boundary

The Core planner is source-safe. Real automatic renumbering still needs exact-SHA licensed BricsCAD V25 proof for native geometry extraction, UCS/drawing coordinates, user axis selection, annotation refresh, UNDO, save/reopen and multi-DWG behavior.

Until that adapter/runtime work exists and passes, describe this capability as **Core spatial-order planning**, not completed automatic CAD Grid renumbering.
