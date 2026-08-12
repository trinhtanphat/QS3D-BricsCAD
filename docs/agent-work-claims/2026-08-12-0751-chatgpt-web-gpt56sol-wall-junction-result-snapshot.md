# Work claim — Wall junction result snapshot

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T07:51:00+07:00`
- Baseline main SHA: `a6782d5321bf8a431099aaafeeb1a9f362984d1c`
- Priority: evidence-driven remote-safe geometry result integrity

## Reason

`WallJunction` is a public result type whose constructor accepts `IReadOnlyList<string> segmentIds` but stores that reference directly. A caller can pass a mutable `List<string>`, construct a completed junction result, and then mutate or clear the source list; `WallJunction.SegmentIds` changes after construction even though the result exposes only read-only properties. The planner itself already passes a read-only list, but the public constructor does not enforce snapshot ownership.

## Reserved scope

Materialize an owned read-only snapshot of `segmentIds` in the `WallJunction` constructor only. Preserve wall-junction planning math, candidate indexing, classification, ordering, ray counts, limits, ownership/native behavior and all public property types. Add focused CAD-independent regression coverage.

## Expected surfaces

- `src/QS3D.Core/Geometry/WallJunctionPlanner.cs` (`WallJunction` constructor only)
- `tests/QS3D.Core.SmokeTests/WallJunctionResultSnapshotSmoke.cs`
- this claim file

## Excluded scope

- No wall-junction geometry/tolerance/intersection/classification changes.
- No validation-policy expansion for ids, enum values, points or ray counts beyond removing collection aliasing.
- No native/UI changes, GitHub Actions dispatch or BricsCAD runtime claim.

## Validation plan

- Construct a `WallJunction` from a mutable segment-id list, mutate and clear the source, and assert the result retains original ids/order.
- Confirm a normal planner-created L/T/X-style junction remains stable and read-only at the collection boundary.
- Re-fetch the current full source blob immediately before write; never force-push.
- Record source/static verification only; do not claim an executed repository `dotnet` run in this hosted session.

## Coordination

The prior wall-junction adjustment-result snapshot lane is completed and explicitly deferred this direct `WallJunction` constructor. Current/recent wall-junction commit history shows no newer overlapping claim, and the planner blob remains unchanged since that defer decision.

## Completion condition

Current `main` exposes a stable `WallJunction.SegmentIds` snapshot independent of caller list mutation, focused regression coverage is present, and this claim is marked `COMPLETED`.
