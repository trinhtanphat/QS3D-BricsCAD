# Auto Room repeated stale-marking no-op

## Problem

`AutoRoomLifecycle.MarkStaleForSelection(...)` currently includes every matched inactive Auto Room in the mutation set, even when the room is already canonically stale for the same `TopologyChanged` reason. Repeating the same selection therefore calls `project.Touch()`, refreshes `BoundaryStaleUtc`, and marks the room dirty without a semantic state transition.

## Contract

1. Preserve existing input enumeration freshness, UTC validation, source-signature matching, floor/zone scoping, and deterministic room ordering.
2. A matched room is a no-op only when all stale metadata is already canonical:
   - `BoundaryState == Stale` using canonical ordinal text,
   - `BoundaryStaleReason == TopologyChanged` using canonical ordinal text,
   - `BoundaryStaleUtc` parses exactly as a round-trip UTC timestamp and is already in canonical `O` form.
3. Missing, malformed, non-UTC, or non-canonical stale metadata remains repairable and therefore counts as a mutation.
4. Return only rooms whose stale metadata is written by this call, in the existing deterministic ID order.
5. A fully no-op call must preserve `ProjectState.ChangeVersion`, element dirty flags/timestamp, and the original stale timestamp.

## Verification

Add focused CAD-independent smoke coverage for first transition, repeated canonical no-op, and malformed metadata repair. Add a static preflight that pins the no-op filter before `project.Touch()` and the canonical metadata requirements. Do not claim GitHub Actions, full .NET build, or BricsCAD runtime execution unless actually run.
