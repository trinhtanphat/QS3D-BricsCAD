# QS3D Grid intersection Core contract

Updated: 2026-08-10 (UTC+7)

`GridIntersectionPlanner` is the CAD-independent geometry layer for deterministic intersections between already-resolved Grid reference curves. It does not read BricsCAD entities and does not create Grid bubbles, constraints, dimensions, host links or native CAD geometry.

## Input model

A caller supplies an explicit bounded list of `GridReferenceCurve` values with unique semantic Grid element IDs:

- `Line(elementId, start, end)` — finite non-zero segment;
- `Arc(elementId, center, radius, startAngleRad, sweepAngleRad)` — finite positive radius and positive counter-clockwise sweep in `(0, 2π]`.

Semantic Grid IDs are trimmed before use, bounded to 128 characters and compared case-insensitively for duplicate detection. Whitespace variants such as `" G-A "` and `"G-A"` therefore cannot become separate intersection owners.

The explicit CCW sweep avoids hiding BricsCAD/native angle conventions inside Core. A future V25 adapter must convert the native LINE/ARC geometry into this contract deliberately and prove that conversion on real V25.

## Supported intersections

- LINE × LINE;
- LINE × ARC;
- ARC × ARC.

Segments/arcs are finite references, not infinite construction lines. Endpoint/tangent intersections are accepted when they resolve to one finite point.

Results are deterministic in input-pair order. A pair may return zero, one or two points depending on finite geometry.

## Fail-closed ambiguity and numeric safety

Core intentionally rejects geometry that cannot define one unambiguous pairwise intersection contract:

- collinear LINE references with a non-zero overlap;
- coincident ARC support circles, even when their stored sweeps appear disjoint;
- duplicate semantic Grid IDs after trim/case normalization;
- non-finite/degenerate geometry;
- derived coordinate/distance/cross-product/quadratic overflow that would otherwise create `NaN`/`Infinity` or a false no-intersection result;
- invalid tolerance, radius or sweep;
- curve/intersection counts beyond bounded limits.

Coincident-circle rejection is conservative by design. Numeric overflow is likewise fail-closed: the planner is not allowed to reinterpret an unrepresentable derived value as parallel, disjoint or a valid point. Do not weaken either guard to guess geometry.

## What remains local/native

This source slice advances issue #79 but does **not** complete Grid constraints or Grid-system behavior. Local/native agents still need to implement and qualify, as separate reviewed slices:

1. V25 LINE/ARC → `GridReferenceCurve` extraction with drawing units and angle/sweep semantics proven on exact SHA;
2. reviewed CAD spatial ordering for renumbering (Core naming already requires explicit order);
3. native Grid bubble/label placement and generated ownership/replacement;
4. optional Grid intersection markers/dimensions/constraints tied to stable semantic IDs;
5. structure-to-grid hosting/snapping policy;
6. rectangular/radial Grid-system authoring and Direct Draw/repeat UX;
7. save/reopen, UNDO, multi-DWG, Unicode/HiDPI and host-theme qualification.

Do not use this planner to auto-move structural source CAD or to infer engineering constraints. It only reports finite geometric intersections for explicitly supplied Grid references.

Canonical runtime handoff remains `docs/LOCAL-V25-QUALIFICATION.md` and `docs/REMAINING-LOCAL-ISSUES-2026-08-10.md`.
