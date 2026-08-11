# Work claim — Polygon region orientation overflow

- Status: `COMPLETED`
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

Individual island loops are normalized by the hardened polygon clipper, but cross-island validation had its own raw `Orientation`: `(b.X-a.X)*(c.Y-a.Y) - (b.Y-a.Y)*(c.X-a.X)`. Large finite nearly parallel vectors can overflow both products while retaining a finite determinant, so disjoint/intersecting/touching island decisions could fail solely in this duplicate topology layer.

## Implementation

- `d6ade47799edd76e1f13b105add43c4454e9b3ce` — replace raw cross-island orientation products with a scale-safe finite determinant helper while preserving `Epsilon`, boundary/touch and nesting policy.
- `02062f068ccb410867ce8a3e9efa48393eb7287b` — add two long finite near-parallel island strips whose boundaries genuinely intersect while raw orientation products overflow; require the explicit `intersect or touch` policy error rather than numeric overflow.

## Concurrency handling

- The first claim close-out update received HTTP 409 while `main` advanced concurrently.
- Re-fetched the claim blob, confirmed it remained `ACTIVE` and unchanged, then retried without force.

## Validation performed

- Re-fetched target source and confirmed raw `Orientation` remained before editing.
- Re-fetched committed source and confirmed orientation deltas plus scale-safe determinant evaluation are now used by segment and on-segment topology checks.
- Re-fetched the regression and confirmed it distinguishes intended topology rejection from `OverflowException`.
- Source/static validation only; no GitHub Actions dispatched and no BricsCAD V25 runtime/build/NETLOAD PASS claimed.

## Explicit exclusions retained

- No island count/total-vertex caps, region IDs, hole semantics, point-in-polygon parity, tagged scanline output, ownership/nesting policy, Room/Wall authoring, native V25, UI, Actions, release, or LOCAL_PASS behavior changes.

## Completion

Cross-island topology validation no longer fails solely on avoidable orientation product overflow, focused regression is integrated on `main`, and this claim is closed.
