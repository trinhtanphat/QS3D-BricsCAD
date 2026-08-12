# Work claim — Polygonal slab multi-region preflight bound

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-polygonal-slab-region-preflight-20260812-0914`
- Registered: `2026-08-12T09:14:00+07:00`
- Baseline main SHA: `f56e0df9d7ccd8981395932990471fe091cf70bf`
- Priority: evidence-driven Core resource preflight during owner-requested review/fix continuation

## Confirmed defect

`PolygonRegionSetTopology.NormalizeAndValidate(...)` defines the canonical polygon multi-region capacity as 256 islands and materializes only 257 items before rejecting overflow. `PolygonalSlabMultiRegionMeshPlanner.Plan(...)`, however, previously allocated `new List<PolygonRegionSeed2>(input.Regions.Count)` and copied every supplied region before calling that bounded helper. A malformed/oversized `IReadOnlyList` could therefore force allocation and traversal far beyond the topology contract before being rejected.

## Implemented fix

`PolygonalSlabMultiRegionMeshPlanner.Plan(...)` now rejects `input.Regions.Count > 256` before seed-list allocation/copy, matching the downstream topology contract. Region/null validation, polygon topology/overlap rules, slab mesh numeric/count behavior, 32,768 total-bar cap, output ordering and read-only result ownership remain unchanged.

## Integration evidence

- Claim registration: `1ffdb99ab6c062936b24da3c83f59f77af638e76`.
- Source fix: `6b5a4980e4b65361eaee9844831630f13d898da2`.
- Focused smoke: `01d4e81ef4eb6e4c6387ecdb3fd7fdec3f5d4403`.
- Source read-back on moving `main` confirmed the >256 guard occurs before `new List<PolygonRegionSeed2>(input.Regions.Count)`.
- Smoke read-back confirmed 257 regions fail closed and an ordinary single square region still produces a non-empty mesh layout with a consistent total-bar count.

## Validation boundary

Deterministic source and focused smoke coverage were committed and read back. No GitHub Actions were dispatched, no executable full Core smoke/build PASS is claimed, and no licensed BricsCAD runtime qualification is claimed.