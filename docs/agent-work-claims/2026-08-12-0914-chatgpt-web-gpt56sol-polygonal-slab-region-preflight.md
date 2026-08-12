# Work claim — Polygonal slab multi-region preflight bound

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-polygonal-slab-region-preflight-20260812-0914`
- Registered: `2026-08-12T09:14:00+07:00`
- Baseline main SHA: `f56e0df9d7ccd8981395932990471fe091cf70bf`
- Priority: evidence-driven Core resource preflight during owner-requested review/fix continuation

## Confirmed defect

`PolygonRegionSetTopology.NormalizeAndValidate(...)` defines the canonical polygon multi-region capacity as 256 islands and materializes only 257 items before rejecting overflow. `PolygonalSlabMultiRegionMeshPlanner.Plan(...)`, however, first allocates `new List<PolygonRegionSeed2>(input.Regions.Count)` and copies every supplied region before calling that bounded helper. A malformed/oversized `IReadOnlyList` can therefore force allocation and traversal far beyond the topology contract before being rejected.

## Reserved scope

- `src/QS3D.Core/Rebar/PolygonalSlabMultiRegionMeshPlanner.cs` — region-count preflight before seed allocation/copy only.
- `tests/QS3D.Core.SmokeTests/PolygonalSlabMultiRegionInputBoundSmoke.cs` — focused CAD-independent regression.
- this claim file.

## Contract

Reject more than 256 regions before allocating/copying the seed list, matching `PolygonRegionSetTopology` exactly. Preserve region/null validation, polygon topology/overlap rules, slab mesh numeric/count behavior, 32,768 total-bar cap, output ordering and read-only result ownership.

## Validation plan

Prove 257 regions fail closed at the planner boundary and a single ordinary square region still produces a non-empty mesh layout within the existing total-bar contract. Re-fetch exact source before write; never force-push. No GitHub Actions dispatch, executable full Core test PASS or licensed BricsCAD runtime qualification claim.