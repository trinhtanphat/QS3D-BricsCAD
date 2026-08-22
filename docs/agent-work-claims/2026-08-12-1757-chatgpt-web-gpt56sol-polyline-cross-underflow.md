# Work claim — Polyline SignedArea cross-product underflow

- Status: `COMPLETED`
- Agent: `chatgpt-web/gpt56sol-polyline-cross-underflow`
- Registered: `2026-08-12T17:57:00+07:00`
- Completed: `2026-08-12T18:12:00+07:00`
- Baseline main SHA: `eb09884921da90d1ca1d02f80643d4d6334a3516`
- Claim commit: `043930820eddc286908c5f85e560b91d07336ec8`
- Source branch commit: `5b88aaeb22aa334585d6611ddfecec1bd4505239`
- Regression branch commit: `ce9835d026c8393edcd054de787d89bad5256f90`
- Integration PR: `#943`
- Integration commit: `37bb1973d4ac14b7fbce4cf5b6319f5d83079117`
- Priority: P0 — finite polygons with a representable non-zero subnormal signed area must not be silently collapsed to zero by intermediate multiplication underflow.

## Reserved scope

Fixed `PolylineMetrics.SignedArea(...)` only for the case where `CrossFinite(...)` sees finite non-zero multiplicands whose direct products underflow to exact zero. The direct fast path now rejects those false-zero intermediates and falls through to the existing scaled cross-product restoration path.

## Changed surfaces

- `src/QS3D.Core/Geometry/PolylineMetrics.cs`
- `tests/QS3D.Core.SmokeTests/PolylineSignedAreaCrossUnderflowSmoke.cs`
- this claim file

## Excluded scope

- coordinate-delta overflow already completed by `CORE-POLYLINE-SIGNED-AREA-DELTA-OVERFLOW`
- existing cross-product overflow/cancellation hardening
- `Point2`, polygon topology/scanline planners, callers, persistence, UI, BricsCAD runtime code
- GitHub Actions or licensed V25 runtime qualification

## Validation actually executed

- Confirmed on the pre-fix source that `CrossFinite(...)` accepted finite `0d` products immediately even when both multiplicands were non-zero.
- Deterministic IEEE-754 counterexample: `SmallAxis = 1e-200`, `LargeAxis = 2.4e-124`; the raw product underflows to `0d`, while scaled restoration preserves a `double.Epsilon` cross contribution.
- Independently evaluated the patched arithmetic for positive orientation (`double.Epsilon` signed area), reversed orientation (`-double.Epsilon`), legitimate collinear zero (`0d`), and an ordinary 2x3 square (`6d`).
- Read back `src/QS3D.Core/Geometry/PolylineMetrics.cs` from current `main`; blob `250b8b43af6f634ca54f7d67786c0e15bbeece1c` contains the underflow guards.
- Read back `tests/QS3D.Core.SmokeTests/PolylineSignedAreaCrossUnderflowSmoke.cs` from current `main`; blob `2ed54516191a20ae5fd168feb3af94172e301a83` contains the focused regression.
- PR #943 was squash-merged successfully as `37bb1973d4ac14b7fbce4cf5b6319f5d83079117`.

## Remaining gates

- GitHub Actions were not dispatched, per repository manual-only CI policy.
- The available execution environment did not provide a .NET SDK, so no `dotnet test` PASS is claimed here.
- No licensed BricsCAD V25 runtime qualification was executed or claimed; that remains LOCAL_ONLY where applicable.

## Coordination

The prior Polyline coordinate-delta overflow lane remains separate and completed at `f28794f88c24dd2275da48804e4ed6549d0ab174`. Concurrent numeric-underflow work targets other Core domains and does not overlap `PolylineMetrics.CrossFinite`.

## Completion condition

Satisfied: the source fix and focused smoke are present on current `main`, their blobs were read back, the integration SHA is recorded, and this claim is closed without overstating CI or runtime evidence.
