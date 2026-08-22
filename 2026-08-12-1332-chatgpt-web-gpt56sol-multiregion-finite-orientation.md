# Work claim — Multi-region finite orientation cancellation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-multiregion-finite-orientation-20260812-1332`
- Registered: `2026-08-12T13:32:00+07:00`
- Baseline main SHA: `728de533a5df9e15a31d725cc6cfe2f9d60fadf2`
- Priority: evidence-driven Core topology numeric correctness during owner-requested `continue all`

## Confirmed defect

`PolygonRegionSetTopology.CrossFinite(...)` always independently normalizes the two orientation vectors. As with the completed polygon scanline follow-up, this can round a finite non-zero determinant to zero when the vectors have very different magnitudes.

Use `A=(1e46, 2.1485982218963585e45)` and `B=(0.01, 0.0021485982218963583)`. The raw binary64 determinant `A.X*B.Y - A.Y*B.X` is `-2.4758800785707605e27`, while the current normalized expression is exactly zero. A large triangle `(0,0), A, (0,1e46)` lies above edge `(0,0)->A`; a small triangle beginning at `B` and extending downward lies strictly below that edge. Current multi-region orientation misclassifies `B` as collinear/on-segment and rejects these disjoint islands as intersecting/touching.

## Reserved scope

- `src/QS3D.Core/Geometry/PolygonRegionSetTopology.cs` — `CrossFinite(...)` finite-product compatibility path only.
- `tests/QS3D.Core.SmokeTests/PolygonRegionOrientationOverflowSmoke.cs` — extend the existing ModuleInitializer orientation regression with disjoint asymmetric-scale islands.
- this claim file.

## Intended contract

- Prefer the direct determinant when raw component products and their subtraction are finite.
- Retain current normalized fallback only for non-finite raw intermediates.
- Preserve `Epsilon`, island overlap/nesting policy, point-location logic, bounds pruning, region ordering and scanline behavior.

## Coordination

The prior polygon region orientation-overflow claim is `COMPLETED`; it fixed overflowing products, not finite-product cancellation. Recent claim search shows no newer live `PolygonRegionSetTopology.cs` lane. Polygonal slab preflight owns script/preflight scope, not this topology helper. Current Quantity Rule, Bulk/Family, Beam Stirrup and release lanes are disjoint.

## Validation boundary

Extend existing auto-registered orientation smoke with two public `PolygonRegionSeed2` islands that are geometrically disjoint but currently fail the intersect/touch policy solely because normalized orientation collapses the finite determinant. Source/readback and deterministic numeric validation only; no GitHub Actions and no BricsCAD runtime PASS claim.
