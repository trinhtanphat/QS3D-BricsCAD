# QS3D — local Grid / polygon mesh native gates

Updated: 2026-08-10 (UTC+7)

This is a focused execution handoff for agents with interactive Windows + licensed BricsCAD V25. It covers source that is already useful remotely but must not be described as native/runtime complete until the gates below pass on an exact SHA.

Read first:

- `docs/LOCAL-V25-QUALIFICATION.md`
- `docs/REMAINING-LOCAL-ISSUES-2026-08-10.md`
- `docs/GRID-WORKFLOW.md`
- `docs/GRID-INTERSECTIONS.md`
- `docs/POLYGONAL-SLAB-MESH.md`
- `docs/LEVEL-REFERENCES.md`
- `CI_POLICY.md`

GitHub Actions remain manual-only. Do not dispatch CI merely because this handoff exists or because a local test is ready.

## A. Grid native extraction and intersection gate — issue #79

### Source already present

Core/source now provides:

- `QS3DGRID` semantic capture for finite LINE/ARC sources;
- `GridNamingService` with explicit-order numeric/alphabetic semantic labels;
- `GridNamingHealthService` in comprehensive health;
- `GridIntersectionPlanner` for bounded finite LINE×LINE, LINE×ARC and ARC×ARC intersection planning;
- fail-closed overlap/coincident-circle handling;
- `QS3DSYNCSOURCE` for authoritative source reconciliation.

### Local implementation target

Add one reviewed V25 adapter slice that converts **tracked Grid source entities only** into the Core `GridReferenceCurve` contract. Do not read arbitrary untracked LINE/ARC geometry and pretend it is Grid semantics.

For each tracked Grid:

1. resolve the canonical semantic Grid owner from the selected source/generated context;
2. open its source CAD entity read-only under the active locked document context;
3. accept only native LINE or ARC sources consistent with the existing `QS3DGRID` source contract;
4. convert drawing coordinates into `Point2` values without silently changing drawing units;
5. convert ARC geometry into explicit center/radius/start-angle/**positive CCW sweep** expected by Core;
6. reject non-finite, degenerate, full-overlap/ambiguous native geometry before any semantic/native mutation;
7. pass only stable semantic Grid IDs into `GridIntersectionPlanner`;
8. keep intersection analysis read-only until a separate reviewed ownership contract exists for generated markers/dimensions/constraints.

### Runtime acceptance matrix

Use a private/sanitized DWG containing:

- perpendicular LINE Grids;
- skew LINE Grids;
- LINE endpoint touch;
- collinear non-overlap, endpoint-touch and overlapping LINE pairs;
- LINE × ARC with 0/1/2 finite intersections;
- ARC × ARC with 0/1/2 finite intersections;
- tangent ARC cases;
- same support circle / overlapping ARC ambiguity;
- negative/large world coordinates;
- millimetre and metre drawings;
- moved/edited tracked sources followed by `QS3DSYNCSOURCE`;
- save/reopen and multi-DWG switching.

Record whether native ARC start/end/sweep conversion produces the same Core point(s) before and after save/reopen. A source-static pass is not enough.

### Do not implement yet without a separate reviewed contract

- auto-moving columns/walls to Grid intersections;
- generated Grid bubbles/markers/dimensions without canonical owner slots;
- automatic rectangular/radial Grid systems that create a second Grid store;
- automatic spatial renumbering without a visible/reviewable ordering rule;
- constraints that silently modify source CAD.

## B. Grid labels / bubbles native gate — issue #79/#77

`GridLabel` and `GridSequenceIndex` are semantic properties only. A local/native bubble layer must:

- use the existing semantic Grid ID as owner identity;
- create its own generated annotation ownership slot instead of overloading `GeneratedSolidHandle`;
- update/replace only annotations it owns;
- survive save/reopen;
- fail closed on foreign/ambiguous annotations;
- support Unicode Vietnamese;
- define Model Space vs Paper Space explicitly;
- define stale/update behavior when Grid source or semantic label changes;
- leave semantic Grid capture/naming usable even when no native bubble exists.

Do not mark Grid naming native-complete merely because `GridNamingService`/health pass in Core.

## C. Polygonal Slab rebar native gate — issue #83

### Source already present

Current source contains:

- `PolygonScanlineClipper`;
- `PolygonalSlabMeshPlanner`;
- guarded V25 Slab polygon path for closed straight simple POLYLINE Slabs;
- legacy rotated-rectangle `RectangleLocalXY` path retained;
- non-rect simple polygon `PolygonGlobalXY` path;
- bounded count, cover + bar-radius clearance, generated ownership, stale/audit and rollback contracts.

### Local acceptance

Test exact-SHA `QS3DSLABREBAR3D` on:

- axis-aligned rectangle;
- rotated rectangle (must remain legacy/local-axis compatible);
- convex non-rectangle polygon;
- concave L/U-shaped simple polygons that create split scanline segments;
- near-collinear edges;
- clockwise and counter-clockwise source vertex order if the adapter permits both;
- very small valid polygon near cover/diameter limits;
- invalid self-intersecting polygon;
- source edit → stale/rebuild;
- rerun replacement ownership;
- ESC/failure before commit;
- save/reopen/health.

For every generated bar segment verify it remains inside the supported host footprint after required concrete cover + bar radius. Preserve the explicit source-mode metadata used by health/replacement.

### Explicitly unsupported until separately designed

- bulged/curved POLYLINE boundary;
- holes/islands/multiple outer loops;
- arbitrary inferred local reinforcement axes for freeform polygons;
- automatic structural design.

## D. Foundation polygon mesh gate — issue #83

Foundation remains intentionally behind Slab. Before changing the native Foundation builder:

1. reuse the existing bounded Core polygon clipping/planning contract rather than inventing a second clipper;
2. preserve the current rectangle-backed Foundation behavior as a regression path;
3. define which native source entity supplies the supported simple polygon footprint;
4. preserve Foundation-specific faces/cover/Z placement, independent X/Y notation, owner slot, fingerprint/mode, stale and health semantics;
5. validate the whole polygon/rebar plan before deleting/replacing existing generated bars;
6. keep project snapshot rollback coupled to the native transaction failure boundary;
7. reject unsupported curved boundaries/holes before mutation.

Only after exact-SHA V25 proof should the Foundation adapter advertise polygon support.

## E. Level-reference coupling

Do not complete Grid/Foundation/Slab native work by adding a duplicate Level model. Current Core reuses `FloorDefinition` as Level and already owns Bottom/Top Level reference semantics.

When a native mesh/host starts consuming Level references, use the shared vertical resolver and test:

- legacy elements without Level references remain unchanged;
- bottom-only, top-only and bottom+top reference cases;
- offset changes;
- Floor elevation change invalidation;
- missing/deleted reference guards;
- save/reopen;
- generated rebar/host alignment after reference changes.

## Evidence format

Store raw evidence under ignored `artifacts/`. A sanitized handoff may contain:

```text
Exact SHA: <40-char SHA>
BricsCAD V25 edition/build: <value>
DWG fixture class: <sanitized description>
Grid LINE extraction: PASS/FAIL/NOT RUN
Grid ARC sweep conversion: PASS/FAIL/NOT RUN
Grid intersections: PASS/FAIL/NOT RUN
Grid save/reopen: PASS/FAIL/NOT RUN
Slab polygon mesh: PASS/FAIL/NOT RUN
Foundation polygon mesh: PASS/FAIL/NOT RUN
Level-reference coupling: PASS/FAIL/NOT RUN
Ownership/replacement: PASS/FAIL/NOT RUN
Health/stale: PASS/FAIL/NOT RUN
Known blockers: <sanitized list>
```

Only write `LOCAL_PASS` for gates actually executed on that exact SHA/package. Missing evidence is `NOT QUALIFIED`, not a remote pass.
