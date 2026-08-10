# QS3D authoritative source edit / reconcile workflow

Updated: 2026-08-10 (UTC+7)

## Status

`QS3DSYNCSOURCE` is source-implemented as the deterministic P0 bridge for **native BricsCAD source edits**.

This does not claim custom grip/jig/reactor parity. Interactive BricsCAD-native MOVE/ROTATE/STRETCH/grip behavior still requires licensed V25 runtime qualification. The source-level contract is intentionally simpler and safer:

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

## Runtime qualification still required

Before describing this as production-qualified, test on the exact release SHA in licensed BricsCAD V25 x64:

1. LINE MOVE preserving length;
2. LINE ROTATE preserving length;
3. LINE/STRETCH changing length;
4. closed POLYLINE vertex edit changing area/perimeter;
5. open/closed POLYLINE state changes where the semantic category allows them;
6. Door/Opening movement with linked host and generated Curtain/opening state;
7. Beam/Column/Slab/Foundation edits with generated rebar present;
8. failure injection proving generated CAD erase abort + project snapshot restore;
9. save/reopen and multi-DWG behavior;
10. `QS3DHEALTHALL` / `QS3DRELEASECHECK` after reconcile and after explicit rebuild.

The precise status remains **source-implemented / statically guarded; licensed V25 interactive qualification pending**.
