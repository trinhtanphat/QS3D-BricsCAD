# Work claim — Room boundary intersection arithmetic

- Status: `COMPLETED`
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

`RoomBoundaryEngine.Cross` evaluated `ax * by - ay * bx` directly. Large finite near-parallel vectors could overflow both component products while their determinant remained finite, turning a representable intersection decision into `NaN`/overflow behavior.

`AddEndpointCut` independently computed `dx * dx + dy * dy` and an absolute dot product. A long finite collinear segment could therefore produce `Infinity / Infinity => NaN` for a point whose finite projection parameter was representable, silently dropping an endpoint cut needed for subdivision.

## Implementation

- `859971674ec4f52a244dd036fdc6b8c029bb6a85` — replaces raw determinant arithmetic with a scale-safe finite `Cross` and replaces endpoint length-squared projection with unit-direction/scaled projection arithmetic that clamps/rejects before reconstructing a bounded parameter.
- `41af365003b2d19de8d182e8bd11ca44520eddaf` — adds focused Core smoke coverage for determinant cancellation around `1e160` and a long finite collinear midpoint projection that previously produced `Infinity / Infinity`.

## Validation

- Re-read `RoomBoundaryEngine.cs` from current `main`; source blob `df7c66fd8a060e94795cb479e49cd7da7d37ffab` contains the scale-safe `Cross` and `AddEndpointCut` implementation.
- Re-read `RoomBoundaryIntersectionArithmeticSmoke.cs` from current `main`; test blob `ed75cb1c7d8765f63f1a7dc4952d57db28378975` contains both focused regressions.
- No GitHub Actions were dispatched.
- No local .NET compile/test runner or BricsCAD V25/V26 runtime PASS is claimed from this web session.

## Explicit exclusions

- No `PointSnapper` cell-index contract, graph topology/face traversal, bridge detection, source provenance, Room Auto command lifecycle, UI, native BricsCAD V25/V26, Actions, release, or LOCAL_PASS changes.

## Completion

The claimed Room-boundary pair-cut arithmetic no longer fails solely on avoidable determinant or projection intermediate overflow, focused regression is committed on `main`, and this source-only claim is complete.
