# QS3D authoritative source edit / reconcile workflow

Updated: 2026-08-20 (UTC+7)

## Status

`QS3DSYNCSOURCE` is source-implemented as the deterministic P0 bridge for **native BricsCAD source edits**.

Exact licensed BricsCAD V25.2.10 evidence at SHA `2a6aa84a41daa68f35160bfc78c4330b78bc0f97` now qualifies the deterministic LINE command/batch path for native `MOVE`, `ROTATE`, and crossing-window endpoint `STRETCH`, including production reconcile, generated invalidation/rebuild, save and cold reopen. This does not claim custom grip/jig/reactor parity, manual ESC behavior, POLYLINE topology editing or the remaining category/dependent matrix. Those broader interactive paths still require licensed qualification. The source-level contract remains intentionally simple and safe:

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

Still required before claiming the entire native-edit surface:

1. manual grip/jig and ESC/cancel behavior;
2. closed POLYLINE vertex edit changing area/perimeter;
3. open/closed POLYLINE state changes where the semantic category allows them;
4. Door/Opening movement with linked host and generated Curtain/opening state;
5. Beam/Column/Slab/Foundation edits with generated rebar present;
6. interactive failure injection beyond the existing guarded LOCAL-004 rollback matrix;
7. the remaining multi-DWG/user-interaction variants;
8. `QS3DHEALTHALL` / `QS3DRELEASECHECK` after each remaining reconcile/rebuild scenario.

The broader precise status remains **source-implemented / statically guarded; licensed V25 interactive qualification pending** beyond the exact automated LINE command slice above.
