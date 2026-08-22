# Bulge Arc read-only result parity plan

## Goal

Make `BulgeArcTessellator.Tessellate(...)` honor one immutable-result contract for both its straight/near-zero-bulge fast path and its curved-arc path.

## Evidence

The public return type is `IReadOnlyList<Point2>`. The curved path builds a `List<Point2>` and returns `AsReadOnly()`, while the straight path currently returns `new[] { start, end }`, which remains a mutable `Point2[]` at runtime.

## Implementation

1. Re-fetch moving `main` and the exact `BulgeArcTessellator.cs` blob after claim registration.
2. Replace only the straight-path raw array with a read-only collection wrapper while preserving the exact two values and ordering.
3. Add focused module-initializer Core smoke coverage that verifies straight-path values, verifies both straight and curved results implement `IList<Point2>` as read-only, and proves mutation attempts throw `NotSupportedException`.
4. Inspect the exact source/test commit diffs, refresh `main`, verify both commits remain ancestors with no reserved-path overlap, then close the claim with exact evidence.

## Non-goals

No change to bulge tolerance, included-angle validation, radius/center math, sagitta selection, maximum segment count, curved point generation, polygon tessellation, native CAD integration, Actions, or release behavior.
