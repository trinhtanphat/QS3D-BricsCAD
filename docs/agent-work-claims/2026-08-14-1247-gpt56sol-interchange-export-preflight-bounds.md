# Work claim — Interchange exporter preflight bounds

- Status: `ACTIVE`
- Agent: `gpt56sol-interchange-export-preflight-bounds-20260814-1247`
- Registered: `2026-08-14T12:47:00+07:00`
- Baseline main SHA: `de203ddcce8ca074506f2ae09a8e084a65d94708`
- Priority: `P1` export/resource integrity for the canonical semantic interchange boundary.

## Confirmed gap

`ProjectInterchangeJsonValidator` defines the canonical snapshot resource ceilings (`MaxCollectionItems = 250000`, `MaxElements = 100000`, `MaxFileBytes = 16 MiB`). `ProjectInterchangeJsonExporter.Build()` currently validates semantic integrity and then sorts/serializes the complete Zone/Floor/Family/Element graph before `RequireCanonicalSnapshot()` applies those canonical limits. Oversized in-memory project collections can therefore consume avoidable CPU and allocation before the exporter predictably rejects the generated snapshot.

## Claimed scope

- `src/QS3D.Core/Export/ProjectInterchangeJsonExporter.cs`
- one focused new smoke under `tests/QS3D.Core.SmokeTests/` covering exporter collection preflight bounds.

## Planned bounded fix

- preflight the four exported collection counts against the validator's existing public `MaxCollectionItems` and `MaxElements` constants before semantic validation, ordering or JSON allocation;
- accept the exact limits and fail closed one item beyond them;
- preserve the existing 16 MiB final serialized-size validation, JSON schema/format, semantic-reference validation, deterministic ordering and atomic-file behavior;
- add focused regression coverage proving the oversized boundary is rejected before expensive semantic enumeration/serialization where observable.

## Excluded scope

- no changes to `ProjectInterchangeJsonValidator`, importer/remap/merge/provenance flows, schema/version, persistence, BricsCAD/native commands or UI;
- no new arbitrary limits beyond the validator's existing canonical contract;
- no force-push and no GitHub Actions dispatch.

## Validation boundary

Remote diff/readback and current-main ancestry will be verified. Managed build/smoke execution is only claimed if the environment actually provides `dotnet`; BricsCAD/native PASS will not be claimed without that runtime.
