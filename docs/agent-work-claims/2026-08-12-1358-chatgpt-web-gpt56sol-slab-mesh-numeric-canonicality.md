# Work claim — Slab mesh numeric metadata canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-slab-mesh-numeric-canonicality-20260812-1358`
- Registered: `2026-08-12T13:58:00+07:00`
- Baseline main SHA: `ecbb20f1001becccb8f2f52724cf2b015ae0343a`
- Priority: owner-requested continue-all Core health integrity

## Confirmed defect

`SlabMeshSolidBuilder.CommitSemanticUpdate()` writes five generated numeric snapshots with exact round-trip invariant text (`ToString("R", CultureInfo.InvariantCulture)`), while `GeneratedSlabMeshHealthService` accepts any parseable lexical alias. A corrupted value such as `+12`, `12.0`, or `2E-1` can therefore pass health even though the canonical writer never emits that text.

## Owned scope

- `src/QS3D.Core/Diagnostics/GeneratedSlabMeshHealthService.cs`
- planned regression: `tests/QS3D.Core.SmokeTests/GeneratedSlabMeshNumericCanonicalitySmoke.cs`

## Intended contract

Preserve finite/domain validation, then require exact writer-equivalent round-trip invariant text for X/Y diameter, X/Y actual spacing, and cover. Fail visible through existing per-field warnings without normalizing persisted metadata.

## Explicit exclusions

Slab mesh CAD generation, handles/count/enums/footprint-mode casing, stale lifecycle, rebar engineering policy, persistence, and unrelated health services are out of scope.
