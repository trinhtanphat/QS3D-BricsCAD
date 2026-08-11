# Work claim — polygon area translation stability

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-polygon-area-translation-stability-20260811-2343`
- Registered: `2026-08-11T23:43:00+07:00`
- Baseline main SHA: `6b2b09d56235fcf36f7422945df2f413ef0d3310`
- Priority: evidence-driven Core geometry numeric stability during owner-requested `continue all`

## Reserved scope

Make `PolygonScanlineClipper.NormalizeAndValidate` evaluate polygon area from local coordinate deltas instead of absolute-coordinate products so translation alone cannot overflow/corrupt the area validity check when the actual local area remains representable.

## Expected surfaces

- `src/QS3D.Core/Geometry/PolygonScanlineClipper.cs`
- a focused isolated Core smoke file
- this claim file for close-out

## Concrete defect

The area validity check currently accumulates `a.X * b.Y - b.X * a.Y` on absolute coordinates. A simple polygon with moderate finite local extents can therefore have finite, representable area and finite edge/orientation deltas, yet be rejected after a large translation because the absolute products overflow. The same Core already uses origin-relative area accumulation in `PolylineMetrics.SignedArea`, so the current scanline validation is translation-sensitive for purely numeric reasons.

## Explicit exclusions

- No scanline parity, intersection tolerance, self-intersection, axis, segment-count, region/hole topology, native BricsCAD, UI, updater/licensing, interchange, Actions, release, or LOCAL_PASS behavior changes.
- No relaxation for truly non-representable local area/orientation arithmetic.

## Validation plan

- Preserve existing polygon scanline behavior and area tolerance semantics.
- Add a large translated rectangle whose absolute coordinate products overflow while local width/height and twice-area remain finite; normalization and clipping must succeed with finite output.
- Keep fail-closed overflow behavior for genuinely non-representable local geometry.
- Re-fetch current source immediately before implementation; no overwrite of concurrent edits.
- No GitHub Actions will be dispatched and no BricsCAD runtime PASS will be claimed from this web session.

## Completion condition

Polygon validity is translation-stable for representable local area, focused regression is present on current `main`, and this claim is marked `COMPLETED` with exact implementation SHA(s) and validation performed.
