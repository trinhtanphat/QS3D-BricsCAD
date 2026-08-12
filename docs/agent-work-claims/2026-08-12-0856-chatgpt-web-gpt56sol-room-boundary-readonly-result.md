# Work claim — Room boundary structural read-only result

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-room-boundary-readonly-result-20260812-0856`
- Registered: `2026-08-12T08:56:00+07:00`
- Baseline main SHA: `407b715081b2d1937e49eedab90b959c094e7a27`
- Priority: evidence-driven public result ownership during owner-requested full review/fix continuation

## Confirmed defect

`RoomBoundaryEngine.Discover(...)` declares `IReadOnlyList<RoomBoundary>` but its successful final path returns `result.OrderBy(...).ToList()` directly. Callers can cast the returned value to `ICollection<RoomBoundary>` and structurally add/remove/clear discovered boundaries after the engine has published the result.

## Reserved scope

- `src/QS3D.Core/Geometry/RoomBoundaryEngine.cs` — final successful result boundary only.
- `tests/QS3D.Core.SmokeTests/RoomBoundaryReadOnlyResultSmoke.cs` — focused CAD-independent regression.
- this claim file.

## Contract

Return a structural read-only wrapper for the sorted discovered-boundary list while preserving the existing 5,000 input-segment bound, 20,000 subdivided-edge bound, intersection/topology math, boundary ordering/key generation, vertices/source-id snapshots, tolerance/minimum-area validation and empty-result behavior.

## Coordination

Previous Room Boundary bounded-enumeration, intersection-arithmetic and snap-cell-range claims are `COMPLETED`. This lane does not edit Auto Room lifecycle, command/native discovery, room persistence, boundary key semantics or existing Room Boundary smoke/preflight files.

## Validation plan

Discover one ordinary rectangular face, preserve boundary count/key/area/perimeter and read-only child snapshots, require the returned `ICollection<RoomBoundary>` to be read-only, and prove structural `Add` throws `NotSupportedException`. Re-fetch the exact source before write; never force-push. No GitHub Actions dispatch, executable test PASS or BricsCAD runtime qualification claim.