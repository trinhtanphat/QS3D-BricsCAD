# Work claim — Room boundary snap cell range

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-room-boundary-snap-cell-range-20260812-0727`
- Registered: `2026-08-12T07:27:00+07:00`
- Baseline main SHA: `2965418b77b790cecc158ff75f5a71f6ee71f80b`
- Priority: evidence-driven Core topology hardening during owner-requested `continue all`

## Reserved scope

Remove the `Int64` range dependency from `RoomBoundaryEngine.PointSnapper` spatial-cell indexing while preserving tolerance-based nearest-point snapping and deterministic lowest-index tie breaking.

## Expected surfaces

- `src/QS3D.Core/Geometry/RoomBoundaryEngine.cs`
- isolated focused Core smoke regression
- this claim file for close-out

## Concrete defect

`PointSnapper.Cell` currently performs `checked((long)Math.Floor(value / _tolerance))`. Finite inputs such as coordinate `1e10` with tolerance `1e-9` produce a finite cell coordinate `1e19` outside `Int64`, so snapping throws solely because the implementation stores an unbounded spatial index in a bounded integer. Neighbor loops also rely on `cell +/- 1` in the same bounded integer domain.

## Explicit exclusions

- No Room intersection arithmetic, graph/bridge/face traversal, quantized boundary-key policy, source provenance, Room Auto command lifecycle, native BricsCAD runtime, Actions, release, or LOCAL_PASS changes.

## Validation plan

- Represent spatial-cell tokens without `Int64` conversion and enumerate a fixed at-most-three neighbor token set per axis, avoiding arithmetic loop wrap.
- Preserve the existing distance check and deterministic tie break as the final authority for snapping.
- For a non-finite quotient caused only by finite coordinate divided by tiny positive tolerance, use an exact-coordinate fallback token; in that regime distinct representable doubles are farther apart than tolerance, so only identical coordinate values can require snapping.
- Add focused reflection smoke coverage at `1e10 / 1e-9 = 1e19`, asserting repeated identical points snap to one vertex and a distinct representable point remains distinct without overflow.
- Re-fetch target source after claim before implementation; never overwrite concurrent edits.
- No GitHub Actions will be dispatched and no BricsCAD runtime PASS will be claimed from this web session.

## Completion condition

Room-boundary point snapping no longer throws solely because a finite coordinate/tolerance cell index exceeds `Int64`, focused regression is committed on current `main`, and this claim is marked `COMPLETED`.
