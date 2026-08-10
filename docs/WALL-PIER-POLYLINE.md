# WallPier open-POLYLINE profile contract

Status: **source-implemented; BricsCAD V25 runtime verification still required**.

This document defines the specialized `WallPier` path used by `QS3DBUILD3D` when the semantic source is an **open plan-view POLYLINE**. It is deliberately conservative: QS3D reuses the existing guarded wall-footprint engine and does not invent a second offset/junction model.

## Geometry contract

Core entry point: `WallPierPathProfilePlanner`.

Inputs:

- open centerline with at least two distinct finite points;
- `ThicknessM > 0`;
- `HeightM > 0`;
- `WallPierProfileMode = Rectangular | Chamfered`;
- `WallPierChamferM > 0` only for `Chamfered`;
- project `WallMiterLimit`;
- finite positive geometry tolerance.

The planner first delegates the path to `WallFootprintEngine`. Therefore the same centerline cleanup, self-intersection rejection, miter limit, bevel fallback and footprint self-intersection guards used by Tường KT remain authoritative.

### Rectangular

`Rectangular` returns the guarded `WallFootprintEngine` polygon unchanged. A two-point path is numerically equivalent to the existing straight `WallPierProfilePlanner` rectangular profile.

### Chamfered

`Chamfered` modifies **only the four terminal footprint corners**: left/right at the beginning of the open path and left/right at the end. Every terminal corner is replaced by two points located `WallPierChamferM` along its adjacent edges.

Internal bends are **not** independently chamfered. Their join remains the miter/bevel result produced by `WallFootprintEngine`. This avoids creating an undocumented structural/fabrication rule at every path bend.

The chamfer fails closed when:

- a terminal corner cannot be mapped uniquely to the guarded footprint;
- four distinct terminal corners are not available;
- either adjacent terminal edge is not longer than twice the requested chamfer;
- the source centerline/footprint is degenerate or self-intersecting;
- any input/derived value is non-finite.

For a straight two-point path, the result is numerically equivalent to the legacy 8-vertex chamfered `WallPierProfilePlanner` profile.

## BricsCAD V25 adapter

`PolylineWallSolidBuilder` keeps the existing source-reading/native Solid3d pipeline:

1. require open `Polyline` source and unique semantic ownership;
2. tessellate bulged segments using `WallArcSagittaM`;
3. resolve `ThicknessM`, `HeightM`, `BottomOffsetM`, profile mode/chamfer and `WallMiterLimit`;
4. run `WallPierPathProfilePlanner` for `WallPier` only;
5. create one closed planar profile from the returned polygon;
6. create one `Region` and extrude it vertically;
7. replace only the correctly owned prior generated solid;
8. commit semantic/generated metadata only after the CAD transaction succeeds.

ArchitecturalWall and GlassWall keep the existing generic `WallFootprintEngine` path; this batch does not change their geometry behavior.

## Exact path snapshot and quantity parity

After a successful open-POLYLINE WallPier build, the adapter stores:

- `WallPierPathProfileKind = OpenPolyline`;
- `WallPierPathProfileMode`;
- `WallPierPathProfileChamferM`;
- `WallPierPathProfileCenterlineLengthM`;
- `WallPierPathProfileThicknessM`;
- `WallPierPathProfileHeightM`;
- `WallPierPathProfileAreaM2`;
- `WallPierPathProfilePerimeterM`;
- `WallPierPathProfileGrossVolumeM3`;
- `WallPierPathProfileLateralAreaM2`.

`WallRegenerator` may use these exact metrics only when all of the following remain true:

- `GeneratedSolidHandle` exists;
- generated-host stale state is clear;
- snapshot kind is `OpenPolyline`;
- stored mode equals current effective instance/Family mode;
- stored chamfer equals current effective chamfer;
- stored centerline length, thickness and height equal current semantic dimensions within numeric tolerance;
- area/perimeter/volume/lateral values are finite and positive;
- volume equals area × height and lateral area equals perimeter × height within tolerance.

Otherwise the regenerator falls back to the established straight-profile semantic calculation. Release/model-health stale gates remain responsible for preventing stale generated geometry from being treated as current production output.

Building a straight LINE WallPier clears all `WallPierPathProfile*` snapshot keys before quantity regeneration so a previous polyline snapshot cannot leak into a LINE workflow.

## Deterministic regression source

`WallPierProfileSmoke` covers:

- legacy rectangular profile;
- legacy chamfered profile;
- straight rectangular path parity;
- straight chamfered path parity;
- bent rectangular reuse of `WallFootprintEngine`;
- exactly four terminal chamfers on a bent path;
- oversized terminal-chamfer rejection;
- self-intersecting centerline rejection;
- non-finite/degenerate legacy profile rejection.

`scripts/preflight-wall-pier.py` statically locks Core → V25 → quantity → command hint → smoke/docs wiring. `scripts/preflight-all.py` discovers it automatically. No automatic workflow is added.

## Runtime gate

Before describing this exact source SHA as V25 runtime-verified, test on licensed BricsCAD V25 with the exact managed assemblies:

1. Release/x64 compile;
2. DemandLoad/NETLOAD;
3. straight LINE Rectangular/Chamfered regression;
4. open straight/bent POLYLINE Rectangular/Chamfered generation;
5. bulged open POLYLINE tessellation and profile generation;
6. large coordinates and rotated paths;
7. miter and bevel-fallback bends;
8. oversized chamfer and self-intersection fail-closed behavior;
9. rebuild after Family/instance mode, chamfer, thickness and height changes;
10. BQ/quantity parity after rebuild and stale-state behavior before rebuild;
11. save/reopen/private-DWG and UNDO regression.

Until that gate passes, the precise status is **source-implemented / deterministic-Core-covered / static-regression-source-present**, not V25 runtime-verified.
