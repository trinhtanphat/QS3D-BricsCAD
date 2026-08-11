# Work claim — Polyline area cross overflow

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-polyline-area-cross-overflow-20260812-0036`
- Registered: `2026-08-12T00:36:00+07:00`
- Baseline main SHA: `857613da7e1b805538864a49013b73ce0a8e8571`
- Priority: evidence-driven Core numeric hardening during owner-requested `continue all`

## Reserved scope

Make `PolylineMetrics.SignedArea` evaluate finite cross products without requiring each large component product to be finite independently.

## Expected surfaces

- `src/QS3D.Core/Geometry/PolylineMetrics.cs`
- isolated focused Core smoke regression
- this claim file for close-out

## Concrete defect

`SignedArea` was origin-relative but computed each triangle cross as `MultiplyFinite(ax, by) - MultiplyFinite(ay, bx)`. For nearly parallel finite vectors around `1e160`, both component products can overflow near `1e320` while their determinant after cancellation is still representable around `1e305`. The intermediate checks therefore rejected a finite polygon area solely because of avoidable product overflow.

## Implementation

- `ad9d05a349f3e8527bd43edfc04396571db78079` — replace independent raw products with `CrossFinite`, normalizing each vector by its own finite scale and restoring the determinant through guarded smaller-scale then larger-scale multiplication; preserve the existing compensated summation and area sign.
- `951a049295e274164a56b9f5b0d13020b1e8230f` — add focused smoke coverage where the raw component products are infinite but the signed determinant/area remains finite and positive.

## Concurrency handling

- The first source update attempt received HTTP 409 while `main` advanced concurrently.
- Re-fetched current `main` and the exact `PolylineMetrics.cs` blob, confirmed the target area implementation was unchanged, then retried without force; the retry committed successfully.

## Validation performed

- Re-fetched committed source and confirmed `SignedArea` now calls `CrossFinite` while retaining compensated summation.
- Re-fetched the smoke fixture and confirmed it uses finite vectors around `1e160` whose raw products overflow but whose determinant remains representable.
- Source/static validation only; no GitHub Actions dispatched and no BricsCAD V25 runtime/build/NETLOAD PASS claimed.

## Explicit exclusions retained

- No polyline length, orientation/sign semantics, polygon topology, scanline policy, Room/Wall authoring, native V25, UI, Actions, release, or LOCAL_PASS behavior changes.

## Completion

`PolylineMetrics.SignedArea` now accepts representable finite determinants even when their raw component products would overflow, while still rejecting truly non-finite area, focused regression is integrated on `main`, and this claim is closed.
