# Work claim — Polygon multi-region Count snapshot

- Status: `ACTIVE`
- Agent: `/root/fix_source_reconcile_desync`
- Registered: `2026-08-14T16:14:35+07:00`
- Baseline main SHA: `552aa7433ab6fe438076337bc9ba7c86cb9c1cbe`
- Issue: `#83`
- Priority: remote-safe Core resource-bound correctness

## Verified gap

`PolygonalSlabMultiRegionMeshPlanner.Plan(...)` reads the caller-controlled `IReadOnlyList.Count` separately for the 256-region guard, seed-list allocation, and every loop condition. A list that reports `1` on its first Count read and `257` thereafter bypasses the preflight and copies 257 entries before downstream topology rejects it. A Count that keeps increasing can keep `index < Count` true indefinitely, grow the seed list without bound, and never reach the canonical topology cap.

## Reserved scope

- `src/QS3D.Core/Rebar/PolygonalSlabMultiRegionMeshPlanner.cs`: capture `input.Regions.Count` once and use that snapshot for the existing guard, allocation, and copy loop.
- `tests/QS3D.Core.SmokeTests/PolygonalSlabMultiRegionInputBoundSmoke.cs`: add a changing-Count `IReadOnlyList` regression proving one Count read and one planned region; preserve stable 257 rejection and the ordinary single-region control.
- this claim document for closeout only.

## Preserved contracts and exclusions

- Preserve the existing 256-region and 32,768-bar limits, null/input/topology validation, stable RegionId sorting, per-region spacing/count semantics, hole behavior, output ordering, and read-only results.
- No changes to `PolygonRegionSetTopology`, including the active `CrossFinite` claim; no native Slab/Foundation builders, RegionId native ownership, hole association policy, BricsCAD/runtime/LOCAL, GitHub Actions, release, UI, or documentation changes.
- Update `scripts/preflight-polygon-multi-region-mesh.py` only if its existing assertions require reconciliation; no new gate is planned for the two-line source contract.

## Validation

- existing polygon multi-region mesh/topology and polygon region gates;
- `QS3D.Core` and `QS3D.Core.SmokeTests` Release builds;
- full deterministic Core smoke, reporting the first unrelated blocker without expanding scope.
