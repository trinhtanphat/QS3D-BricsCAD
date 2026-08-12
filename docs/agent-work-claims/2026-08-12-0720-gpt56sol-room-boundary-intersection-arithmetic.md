# Work claim — Room boundary intersection arithmetic

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-room-boundary-intersection-arithmetic-20260812-0720`
- Registered: `2026-08-12T07:20:00+07:00`
- Baseline main SHA: `3dc86e27db785071930110dbf710fe91554d8603`
- Priority: evidence-driven Core topology hardening during owner-requested `continue all`

## Reserved scope

Harden finite large-coordinate intersection arithmetic in `RoomBoundaryEngine` without changing room face tracing, snapping policy, bridge detection, minimum-area policy, or authoring lifecycle.

## Expected surfaces

- `src/QS3D.Core/Geometry/RoomBoundaryEngine.cs`
- isolated focused Core smoke regression
- this claim file for close-out

## Concrete defects

`RoomBoundaryEngine.Cross` still evaluates `ax * by - ay * bx` directly. Large finite near-parallel vectors can overflow both component products while their determinant remains finite, turning a representable intersection decision into `NaN`/overflow behavior.

`AddEndpointCut` independently computes `dx * dx + dy * dy` and an absolute dot product. A long finite collinear segment can therefore produce `Infinity / Infinity => NaN` for a point whose finite projection parameter is representable, silently dropping an endpoint cut needed for subdivision.

## Explicit exclusions

- No `PointSnapper` cell-index contract, graph topology/face traversal, bridge detection, source provenance, Room Auto command lifecycle, UI, native BricsCAD V25/V26, Actions, release, or LOCAL_PASS changes.

## Validation plan

- Replace raw determinant arithmetic with a scale-safe finite cross helper while preserving sign and existing epsilon comparisons.
- Replace `AddEndpointCut` length-squared projection with scale-safe unit-direction/ratio comparisons so out-of-range points are rejected before reconstructing a bounded parameter.
- Add focused smoke coverage that directly exercises the private numeric helpers with finite large-coordinate values, avoiding unrelated snap-cell limits.
- Re-fetch target source before implementation and never overwrite concurrent edits.
- No GitHub Actions will be dispatched and no BricsCAD runtime PASS will be claimed from this web session.

## Completion condition

Room-boundary pair-cut arithmetic no longer fails solely on avoidable determinant or projection intermediate overflow, focused regression is committed on current `main`, and this claim is marked `COMPLETED`.
