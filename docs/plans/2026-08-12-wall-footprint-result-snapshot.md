# Wall footprint result defensive snapshot plan

## Goal

Make the public `WallFootprintResult` constructor own an immutable snapshot of its polygon input instead of retaining caller-owned mutable storage.

## Evidence

The constructor currently assigns `Polygon = polygon`, while `Polygon` is a get-only `IReadOnlyList<Point2>`. Passing a mutable `Point2[]` therefore allows both post-construction source mutation and runtime down-cast mutation through the result.

## Implementation

1. Re-fetch moving `main` and the exact `WallFootprintEngine.cs` blob after claim registration.
2. Change only `WallFootprintResult` constructor assignment to a defensive `List<Point2>` snapshot wrapped with `AsReadOnly()`.
3. Add focused module-initializer Core smoke coverage proving post-construction source-array mutation does not alter the result and result index replacement throws `NotSupportedException`.
4. Inspect exact source diff, refresh moving `main`, verify source/test ancestry and no reserved-path overlap, then close the claim.

## Non-goals

No validation policy or cardinality change for the public constructor; no changes to footprint engine calculations, intersection logic, miter/bevel behavior, point numerics, area/perimeter, native CAD integration, Actions, or release behavior.
