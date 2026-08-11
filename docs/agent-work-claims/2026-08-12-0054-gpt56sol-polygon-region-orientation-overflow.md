# Work claim — Polygon region orientation overflow

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-polygon-region-orientation-overflow-20260812-0054`
- Registered: `2026-08-12T00:54:00+07:00`
- Baseline main SHA: `a062ad52bf1c35f9fdd68f4b60756023ecacfa97`
- Priority: evidence-driven Core multi-region topology hardening during owner-requested `continue all`

## Reserved scope

Make `PolygonRegionSetTopology` orientation determinants scale-safe for cross-island boundary/touch/nesting validation.

## Expected surfaces

- `src/QS3D.Core/Geometry/PolygonRegionSetTopology.cs`
- isolated focused Core smoke regression
- this claim file for close-out

## Concrete defect

Individual island loops are normalized by the hardened polygon clipper, but cross-island validation has its own raw `Orientation`: `(b.X-a.X)*(c.Y-a.Y) - (b.Y-a.Y)*(c.X-a.X)`. Large finite nearly parallel vectors can overflow both products while retaining a finite determinant, so disjoint/intersecting/touching island decisions can fail solely in this duplicate topology layer.

## Explicit exclusions

- No island count/total-vertex caps, region IDs, hole semantics, point-in-polygon parity, tagged scanline output, ownership/nesting policy, Room/Wall authoring, native V25, UI, Actions, release, or LOCAL_PASS behavior changes.

## Validation plan

- Replace only orientation determinant arithmetic with a private scale-safe finite cross helper; preserve `Epsilon`, boundary/touch and nesting policies.
- Add focused multi-region smoke coverage using large finite islands whose cross-island orientation raw products overflow while the topology remains representable; assert normalization reaches the intended topology result rather than numeric overflow.
- Re-fetch target source before implementation and do not overwrite concurrent edits.
- No GitHub Actions will be dispatched and no BricsCAD runtime PASS will be claimed from this web session.

## Completion condition

Cross-island topology validation no longer fails solely on avoidable orientation product overflow, regression is integrated on current `main`, and this claim is marked `COMPLETED`.
