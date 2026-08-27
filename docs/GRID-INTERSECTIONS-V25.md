# QS3D Grid intersections — BricsCAD V25 read-only adapter

Updated: 2026-08-27 (UTC+7)

## Command

`QS3DGRIDINTERSECTIONSINSPECT`

This command is the V25 extraction/inspection layer over the existing Core `GridIntersectionPlanner`. It is intentionally **read-only**: it does not rename Grid elements, create CAD markers, write constraints, modify semantic state or move source geometry.

The mutating pair-owned marker lifecycle is now a separate command surface: `QS3DGRIDINTERSECTIONS` materializes the reviewed pair-owned markers, while `QS3DGRIDINTERSECTIONSINSPECT` remains the bounded diagnostic/read-only path documented here. Keeping those command names separate prevents the inspection preflight from accidentally validating the mutating route.

## Selection and ownership contract

Select 2..2000 already-captured Grid source entities. Every selected CAD entity must:

- resolve to exactly one semantic `ElementCategory.Grid`;
- be the Grid's single authoritative source Handle;
- still resolve to a live native `LINE` or `ARC` entity.

Missing semantic ownership, ambiguous ownership, duplicate semantic Grid selection or extra authoritative source handles fail closed before Core planning.

## Remote-safe geometry boundary

The Core planner is 2D, so the remote V25 adapter deliberately accepts only one reviewed WCS plan:

- LINE endpoints must share one Z elevation;
- all selected LINE/ARC sources must share the **same plan elevation**;
- ARC center/start/end must share that elevation;
- ARC normal must normalize to approximately **normal +Z**, which means the ARC is on the accepted WCS-XY plane;
- tilted ARC and negative-normal ARC fail closed rather than being silently projected or reoriented.

For accepted ARC sources, the adapter uses native `Arc.StartAngle` and positive `Arc.TotalAngle` to build the Core positive-CCW sweep contract. It does not derive a sweep by subtracting EndAngle from StartAngle, which could hide wrap-around/convention errors.

Exact behavior of `Arc.StartAngle`, `Arc.TotalAngle`, `Arc.Normal` and transformed/tilted native entities still requires licensed BricsCAD V25 runtime qualification. The strict +Z/WCS-XY gate is intentional until that matrix is proven.

## Core planning

After extraction, `GridIntersectionPlanner.FindIntersections` owns finite geometry behavior:

- LINE × LINE;
- LINE × ARC;
- ARC × ARC;
- finite endpoint/tangent acceptance;
- bounded curve/intersection counts;
- fail-closed collinear LINE overlap;
- fail-closed coincident ARC support circles;
- deterministic semantic pair identity.

The command prints the count and WCS Z elevation, then prints at most the first 100 intersections using `G17` invariant coordinates and semantic Grid label/id pairs. The output cap prevents very large models from flooding the BricsCAD command line while Core still maintains its own bounded intersection limit.

## Why this inspect command does not create markers

`QS3DGRIDINTERSECTIONSINSPECT` **does not create markers** or constraints by design.

A physical intersection belongs to a **pair of Grid semantic IDs**. Marker creation therefore follows the reviewed deterministic **pair ownership** contract in the separate marker lifecycle rather than assigning a marker arbitrarily to only one Grid element. The read-only inspect command does not participate in replacement, health, delete or XData mutation.

## Mutation contract

There is no project snapshot because there is no semantic mutation. CAD entities are opened `ForRead` only. The command does not use `OpenMode.ForWrite`, `Erase`, `AppendEntity`, `AddNewlyCreatedDBObject`, `project.Touch()` or audit mutation.

Failure therefore leaves both `.qsdb` and the DWG unchanged.

## Runtime qualification

Source implementation is `REMOTE_DONE`; native behavior remains `LOCAL_ONLY` until an exact-SHA licensed BricsCAD V25 matrix executes.

Minimum matrix:

1. LINE × LINE crossing and endpoint touch;
2. LINE × ARC with one/two/no finite intersections;
3. ARC × ARC with tangent/two/no finite intersections;
4. collinear overlapping LINE rejection;
5. coincident ARC support-circle rejection;
6. mixed plan elevation rejection;
7. sloped LINE rejection;
8. tilted ARC rejection;
9. negative-normal WCS-XY ARC rejection;
10. verify native `Arc.StartAngle` + `Arc.TotalAngle` conversion across 0/2π wrap cases;
11. large/far-origin coordinates within supported numeric range;
12. command output cap above 100 intersections;
13. multi-DWG isolation;
14. Unicode Grid labels and HiDPI command/palette display.

Do not describe the V25 intersection adapter as runtime-certified until this matrix has actually executed on licensed BricsCAD V25.
