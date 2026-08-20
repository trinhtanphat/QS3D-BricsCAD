# QS3D authoritative source edit / reconcile workflow

Updated: 2026-08-20 (UTC+7)

## Status

`QS3DSYNCSOURCE` is source-implemented as the deterministic P0 bridge for **native BricsCAD source edits**.

Exact licensed BricsCAD V25.2.10 evidence now qualifies three bounded deterministic command paths: LINE `MOVE`/`ROTATE`/endpoint `STRETCH` at SHA `2a6aa84a41daa68f35160bfc78c4330b78bc0f97`, one closed Slab POLYLINE vertex `STRETCH` at SHA `d389fc11a6d9599735180adb34a40a04089e5494`, and one Beam LINE `MOVE` with host/longitudinal/stirrup dependents at SHA `a49342145020b154479eaa780ef3a1af597a2b3f`. All use production reconcile, generated invalidation/rebuild, save and cold reopen. The POLYLINE runner removes the last-created overlapping generated solid from the native crossing selection before displacement, then proves the old solid remained unchanged until reconcile. The Beam runner likewise proves the old host, four longitudinal bars and six stirrups remain unchanged until reconcile, then verifies complete three-family invalidation and replacement. This does not claim custom grip/jig/reactor parity, manual ESC behavior, closed/open topology transitions, Beam STRETCH/count redistribution or the remaining category/dependent matrix. Those broader interactive paths still require licensed qualification. The source-level contract remains intentionally simple and safe:

```text
QS3D semantic element
-> edit its authoritative CAD source with BricsCAD
-> select the edited source
-> QS3DSYNCSOURCE
-> ownership-safe generated invalidation/removal
-> refresh source-derived semantic metrics/CAD metadata
-> deterministic semantic regeneration
-> explicit rebuild only when wanted
```

## Why this command exists

QS3D treats source DWG geometry as authoritative. A native CAD edit can change placement/orientation while preserving the same measured length or area. Therefore comparing only `LengthM`/`AreaM2` is insufficient: a moved or rotated source can make generated host/rebar/curtain geometry wrong even though its scalar dimensions did not change.

`QS3DSYNCSOURCE` treats every explicit reconcile as a geometry change and invalidates dependent generated output accordingly.

## Selection contract

The command fails closed when:

- selection contains QS3D-generated output instead of authoritative source CAD;
- a selected source is not already tracked by QS3D;
- one source handle belongs to multiple semantic elements;
- a semantic element has more than one authoritative source handle in the current P0 reconcile contract;
- multiple selected CAD objects resolve to the same semantic element;
- a Door/WallOpening references a missing host;
- source-derived measurements required for the current source type cannot be refreshed deterministically;
- the active DWG changes during the operation.

It does not silently capture unknown CAD or reassign Family/Floor/Zone/category.

## Transaction / ownership contract

For the selected semantic elements, plus the linked host of a selected Door/WallOpening where applicable:

1. capture a deep `ProjectStateSnapshot`;
2. open one BricsCAD write transaction;
3. use `GeneratedDependentGeometryInvalidator.Prepare(...)` so generated host/rebar/tie/stirrup/mesh/curtain-frame output is erased only after canonical ownership checks;
4. refresh source-derived semantic state from `EntitySnapshotReader` / `CadUnitService`;
5. mark the element `ElementDirtyFlags.All` even when Length/Area did not numerically change;
6. run deterministic `RegenerationEngine`;
7. commit generated invalidation metadata and project revision while CAD is still rollback-capable;
8. commit the CAD transaction.

If any pre-commit phase fails, CAD erase operations abort and the project snapshot is restored. The user's already-edited authoritative source CAD is intentionally **not** rolled back by QS3D.

Post-commit Workspace refresh and viewport regen belong to `SourceReconcileCommands` and are best-effort; UI failure cannot turn a valid reconcile commit into a false operation failure.

## Rebuild boundary

`QS3DSYNCSOURCE` does **not** silently regenerate destructive/native downstream geometry. After a successful reconcile, run the workflow appropriate to the object when you want physical output again, for example:

- `QS3DBUILD3D` for supported host/structural native solids;
- `QS3DCURTAINFRAMES3D` / `QS3DCURTAIN3D` for Curtain frames;
- the relevant rebar 3D command for generated reinforcement;
- explicit opening physical-cut commands when host mutation is intended.

This keeps native editing, semantic reconciliation and physical rebuild as reviewable boundaries.

## Runtime qualification status

The exact `LOCAL_004_P01_LINE_ONLY` run completed these first three cases through the real native command processor on a disposable repository sample:

1. LINE `MOVE` preserving 5 m length, followed by reconcile, stale-solid removal and rebuild;
2. LINE `ROTATE` preserving 5 m length, followed by reconcile and stale-solid removal;
3. LINE endpoint `STRETCH` changing source and semantic length to 8 m, followed by reconcile, rebuild, save and cold reopen.

The exact `LOCAL_004_P02_CLOSED_POLYLINE_VERTEX` run then completed one production Direct Draw Slab cell:

1. native top-level crossing-window `STRETCH` changed only the top-right source vertex from `(4,3)` to `(5,3)` m while the explicitly removed overlapping generated solid stayed at its old geometry;
2. production reconcile refreshed area `12 -> 13.5 m2`, perimeter `14 -> 12 + sqrt(10) m`, gross/net volume `1.44 -> 1.62 m3` and formwork, then erased the stale owned solid;
3. explicit rebuild created distinct owned native output with expected `0..5 x 0..3 x 0..0.12 m` bounds, scoped Core/runtime Health remained clear, and save plus fresh-process cold reopen preserved the final state.

The exact `LOCAL_004_P03_BEAM_DEPENDENT_MOVE` run then completed one dependent-output Beam cell:

1. production `QS3DDRAWBEAM` plus bounded fixture notation built one 5 m Beam host, four `4D16` longitudinal bars and six `D8@1000` stirrups;
2. native top-level `MOVE` translated only the source LINE by `+1 m` WCS Y while every old host/rebar/stirrup handle, bound and volume remained unchanged before reconcile;
3. production reconcile removed all three stale ownership/metadata families, explicit host/rebar/stirrup commands created distinct complete translated replacements, scoped Core/runtime Health stayed clear, and save plus fresh-process cold reopen preserved the final state;
4. the first licensed candidate exposed centered `CreateFrustum` bars translated to the covered source start; the bounded production correction places the bar center at `covered start + axis * usableLength/2` while preserving count, cover, layout, ownership, transaction and semantic metadata contracts.

Still required before claiming the entire native-edit surface:

1. manual grip/jig and ESC/cancel behavior;
2. open/closed POLYLINE state changes where the semantic category allows them;
3. Door/Opening movement with linked host and generated Curtain/opening state;
4. Beam STRETCH/count redistribution and remaining Column/Slab/Foundation edits with generated rebar present;
5. interactive failure injection beyond the existing guarded LOCAL-004 rollback matrix;
6. the remaining multi-DWG/user-interaction variants;
7. `QS3DHEALTHALL` / `QS3DRELEASECHECK` after each remaining reconcile/rebuild scenario.

The broader precise status remains **source-implemented / statically guarded; licensed V25 interactive qualification pending** beyond the exact automated LINE, closed-POLYLINE and Beam-dependent cells above.
