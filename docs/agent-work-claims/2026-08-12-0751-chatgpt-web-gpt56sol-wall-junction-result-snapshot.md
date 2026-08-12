# Work claim — Wall junction result snapshot

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T07:51:00+07:00`
- Completed: `2026-08-12T07:55:00+07:00`
- Baseline main SHA: `a6782d5321bf8a431099aaafeeb1a9f362984d1c`
- Priority: evidence-driven remote-safe geometry result integrity

## Reason

`WallJunction` was a public result type whose constructor accepted `IReadOnlyList<string> segmentIds` but stored that reference directly. A caller could pass a mutable `List<string>`, construct a completed junction result, and then mutate or clear the source list; `WallJunction.SegmentIds` changed after construction even though the result exposes only read-only properties.

## Changed scope

`WallJunction` now materializes an owned `List<string>.AsReadOnly()` snapshot of `segmentIds`. Wall-junction planning math, candidate indexing, classification, ordering, ray counts, limits, ownership/native behavior and public property types remain unchanged.

## Changed surfaces

- `src/QS3D.Core/Geometry/WallJunctionPlanner.cs` (`WallJunction` constructor only for intended behavior)
- `tests/QS3D.Core.SmokeTests/WallJunctionResultSnapshotSmoke.cs`
- this claim file

## Completion

- Claim commit: `4eecf1dc909c163cd3e396a3b8e1ab4e89f04fe4`.
- Initial implementation commit: `f76bff33fa7cf7546715b1b368fba20cd1a5c561` — materialize the segment-id snapshot.
- Immediate correction commit: `44e1dab92eddf2aa99acafb1cb8631ba688a3526` — during self-review, a whole-file replacement typo in the unchanged `CrossFinite` helper was detected (`MultiplyFinite` had an accidental extra argument) and corrected before regression/claim completion. Current source re-fetch confirms the original three-argument helper call is restored.
- Regression commit: `d32d16dd90576ed92d06d8f52fb2151ddf4e31fa` — mutate/clear caller ids and verify snapshot stability; preserve a normal planner T-junction and read-only collection boundary.
- Validation actually performed:
  - re-fetched the current `WallJunction` constructor and confirmed owned read-only snapshot semantics;
  - re-fetched `CrossFinite` after the correction and confirmed its original finite arithmetic call shape is restored;
  - re-fetched the dedicated smoke source and checked the direct-alias and normal-planner cases;
  - no repository `dotnet` tests were executed in this hosted session;
  - no GitHub Actions were dispatched or rerun;
  - no BricsCAD runtime PASS is claimed.

## Coordination

The prior wall-junction adjustment-result snapshot lane was completed and explicitly deferred this direct constructor. No newer overlapping wall-junction claim appeared before this lane was taken.

## Completion condition

Satisfied: current `main` exposes a stable `WallJunction.SegmentIds` snapshot independent of caller list mutation, focused regression coverage is present, and this claim is released as `COMPLETED`.
