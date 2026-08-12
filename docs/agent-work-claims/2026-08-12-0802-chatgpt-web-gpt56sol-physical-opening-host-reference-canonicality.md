# Work claim — physical opening host reference canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-physical-opening-host-reference-canonicality-20260812-0802`
- Registered: `2026-08-12T08:02:00+07:00`
- Baseline main SHA: `0d8585b10d8de98b6a54929b6c38a4ff0d9d3ad6`
- Priority: P1 — keep physical opening-cut ownership verification fail-closed on canonical semantic relations.

## Reserved scope

`PhysicalOpeningCutTargetStateCodec.Resolve(...)` verifies each persisted cut target by reading its mutable `HostWallId`, but currently compares `linkedHostId?.Trim()` to the canonical host ID. A padded relation such as `" HOST "` is rejected by QSDB persistence yet is still accepted as proof that the opening belongs to the host. Physical cut/rehost safety should not rely on a non-canonical semantic relation.

## Reserved surfaces

- `src/QS3D.Core/Services/PhysicalOpeningCutTargetStateCodec.cs`
- `tests/QS3D.Core.SmokeTests/PhysicalOpeningHostReferenceCanonicalitySmoke.cs` (new focused module-initializer regression)
- this claim file

## Intended fix

- During `Resolve(...)`, require `HostWallId` to be non-empty and canonical without leading/trailing whitespace before case-insensitive host identity comparison.
- Preserve case-insensitive canonical host IDs, target-list normalization/encoding/order/size rules, opening-category checks, project ownership checks, and all native BricsCAD cutting code.
- Add focused Core smoke coverage proving canonical lower-case host relation resolves while padded/whitespace-only host relations fail closed.

## Validation boundary

Committed deterministic Core smoke coverage plus exact source/diff review. No GitHub Actions dispatch; no licensed BricsCAD V25 runtime PASS claimed.
