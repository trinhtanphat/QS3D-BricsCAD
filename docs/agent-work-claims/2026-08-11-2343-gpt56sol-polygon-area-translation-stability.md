# Work claim — polygon area translation stability

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-polygon-area-translation-stability-20260811-2343`
- Registered: `2026-08-11T23:43:00+07:00`
- Completed: `2026-08-11T23:46:00+07:00`
- Baseline main SHA: `6b2b09d56235fcf36f7422945df2f413ef0d3310`
- Priority: evidence-driven Core geometry numeric stability during owner-requested `continue all`

## Completed scope

`PolygonScanlineClipper.NormalizeAndValidate` now evaluates polygon area from local coordinate deltas instead of absolute-coordinate products so translation alone cannot overflow/corrupt the area validity check while the local geometry remains representable.

## Changed surfaces

- `src/QS3D.Core/Geometry/PolygonScanlineClipper.cs`
- `tests/QS3D.Core.SmokeTests/PolygonTranslatedAreaSmoke.cs`
- this claim file

## Concrete defect fixed

The old area validity check accumulated `a.X * b.Y - b.X * a.Y` on absolute coordinates. A simple polygon with finite local extents and representable area could therefore be rejected after a large translation because those absolute products overflowed even though all edge/orientation deltas stayed finite.

## Validation performed

- Re-read current remote source after implementation: area accumulation triangulates against the first vertex, validates every local delta/cross/sum as finite, and uses compensated accumulation while retaining the exact existing `Math.Abs(twiceArea) <= Epsilon` validity threshold.
- Added isolated `ModuleInitializer` regression coverage with a rectangle translated near `1e155`; its absolute coordinate square is infinite while its actual representable span is about `9.53e139` and its local twice-area is finite (~`1.82e280`).
- Regression validates both normalization and a horizontal scanline, including one finite positive segment and exact translated endpoints.
- Re-read source and regression blobs from remote `main` after writes; intended changes remain present.
- No scanline parity/tolerance/self-intersection/segment-limit semantics were intentionally changed.
- No GitHub Actions were run or dispatched. No local .NET/BricsCAD runtime PASS is claimed from this environment.

## Implementation commits

- `b0bec89cccb5d0cece58d187ea6c28aa60e761ae` — `fix(geometry): stabilize translated polygon area`
- `5ca5220c9a2dbf40284805a43d7cf03fa089cbd1` — `test(geometry): guard translated polygon area`

## Result

Polygon validity is now translation-stable for representable local area while genuinely non-representable local deltas/cross products/sums still fail closed.
