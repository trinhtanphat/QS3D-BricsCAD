# Curtain opening pieces read-only result parity plan

## Goal

Make frame and panel opening planners return structurally read-only `Pieces` collections that match their `IReadOnlyList<...>` API boundary.

## Evidence

Both planners currently sort their output then call `.ToArray()` before assigning `Pieces`. The plan DTOs expose `Pieces` as `IReadOnlyList<...>`, but the runtime object remains a concrete mutable array.

## Implementation

1. Re-fetch moving `main` and both exact source blobs after claim registration.
2. Change only final ordered `Pieces` materialization from raw arrays to read-only collection wrappers, preserving ordering and element instances.
3. Add focused module-initializer Core smoke coverage for frame and panel plans: verify non-empty representative results, preserve expected source indices/counts, reject index replacement, and confirm results are not arrays.
4. Inspect each exact source commit diff, refresh moving `main`, verify source/test ancestry and zero reserved-path overlap, then close the claim.

## Non-goals

No change to piece DTO mutability, subtraction geometry, numeric tolerances, input/output limits, area calculations, opening clearance semantics, native CAD integration, Actions, or release behavior.
