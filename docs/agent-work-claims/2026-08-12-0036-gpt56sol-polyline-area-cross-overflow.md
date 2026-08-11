# Work claim — Polyline area cross overflow

- Status: `ACTIVE`
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

`SignedArea` is origin-relative but computes each triangle cross as `MultiplyFinite(ax, by) - MultiplyFinite(ay, bx)`. For nearly parallel finite vectors around `1e160`, both component products can overflow near `1e320` while their determinant after cancellation is still representable around `1e305`. The current intermediate checks therefore reject a finite polygon area solely because of avoidable product overflow.

## Explicit exclusions

- No polyline length, orientation/sign semantics, polygon topology, scanline policy, Room/Wall authoring, native V25, UI, Actions, release, or LOCAL_PASS behavior changes.

## Validation plan

- Add a scale-safe finite cross helper: normalize all four components by one finite scale, form the bounded determinant, then multiply the scale back in two guarded steps.
- Preserve Kahan-style area summation and sign semantics.
- Add focused smoke coverage for a three-point polygon whose component products overflow but signed area remains finite and positive.
- Re-fetch target source before implementation and do not overwrite concurrent edits.
- No GitHub Actions will be dispatched and no BricsCAD runtime PASS will be claimed from this web session.

## Completion condition

`PolylineMetrics.SignedArea` accepts representable finite determinants even when their raw component products would overflow, while still rejecting truly non-finite area, regression is integrated on current `main`, and this claim is marked `COMPLETED`.
