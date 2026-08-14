# Work claim — Interchange exporter preflight bounds

- Status: `COMPLETED`
- Agent: `gpt56sol-interchange-export-preflight-bounds-20260814-1247`
- Registered: `2026-08-14T12:47:00+07:00`
- Baseline main SHA: `de203ddcce8ca074506f2ae09a8e084a65d94708`
- Priority: `P1` export/resource integrity for the canonical semantic interchange boundary.

## Confirmed gap

`ProjectInterchangeJsonValidator` defines the canonical snapshot resource ceilings (`MaxCollectionItems = 250000`, `MaxElements = 100000`, `MaxFileBytes = 16 MiB`). `ProjectInterchangeJsonExporter.Build()` validated semantic integrity and then sorted/serialized the complete Zone/Floor/Family/Element graph before `RequireCanonicalSnapshot()` applied those canonical limits. Oversized in-memory project collections could therefore consume avoidable CPU and allocation before the exporter predictably rejected the generated snapshot.

## Implemented

- Claim-only commit: `563b77a1a814416681ce866edd14663a7e106873`.
- Source fix: `c1e23143c8d51bdb499f891ba23ca1166e416e5a`.
  - `Build()` now preflights the exported Zone/Floor/Family/Element counts before semantic-reference traversal, duplicate-id traversal, ordering or JSON allocation;
  - the preflight reuses `ProjectInterchangeJsonValidator.MaxCollectionItems` and `MaxElements` directly, so no second/arbitrary resource contract was introduced;
  - checks use `>` and therefore preserve the exact canonical limits;
  - the existing final 16 MiB serialized-size validation, schema, deterministic ordering and atomic file behavior are unchanged.
- Focused self-registering smoke: `ddd9ab44452ce9f5bdf703c2572c37d2749470d8`.
  - a small canonical project still builds and passes the canonical validator;
  - 100,001 element references fail with the element preflight error before duplicate semantic validation;
  - 250,001 total collection references fail with the collection preflight error before duplicate semantic validation;
  - repeated references are used so the resource-bound smoke does not allocate hundreds of thousands of domain objects.

## Validation actually performed

- Remote source diff readback confirms the source commit only adds the preflight call and one bounded helper in `ProjectInterchangeJsonExporter.cs`.
- Remote smoke diff readback confirms only `ProjectInterchangeExportBoundsSmoke.cs` was added.
- After the smoke commit, current `main` advanced to `6bf1fe5466b48bf7b09d426f5576af27d37196e7` through an unrelated V25 release diagnostic change whose direct parent is the smoke commit, so both substantive commits are preserved in live ancestry.
- GitHub combined status for the smoke commit has no attached status contexts/checks; no GitHub Actions were dispatched.
- This environment does not provide `dotnet`; managed build/smoke execution is `NOT_RUN` and is not claimed as PASS. No BricsCAD/native runtime validation was executed or claimed.

## Excluded scope preserved

- no changes to `ProjectInterchangeJsonValidator`, importer/remap/merge/provenance flows, schema/version, persistence, BricsCAD/native commands or UI;
- no new arbitrary limits beyond the validator's existing canonical contract;
- no force-push and no GitHub Actions dispatch.

## Completion

`COMPLETED`: claim-first reservation, canonical collection-bound preflight, focused regression, remote diff/readback and live-main ancestry verification are recorded on `main` with explicit managed/native runtime boundaries.
