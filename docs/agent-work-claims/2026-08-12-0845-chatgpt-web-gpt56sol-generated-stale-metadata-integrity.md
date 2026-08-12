# Work claim — generated stale metadata integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-generated-stale-metadata-integrity-20260812-0845`
- Registered: `2026-08-12T08:45:00+07:00`
- Baseline main SHA: `92fe422a809309bd818fb6be68baa90dfd1f53cd`
- Priority: P1 — make malformed persisted stale-state fail visible without changing output-generation freshness semantics.

## Reserved scope

Normal `ProjectElement.MarkGenerated*Stale(...)` writes a stale state marker and matching stale snapshot together. `IsGenerated*Stale()` intentionally requires the snapshot to match the current output so stale metadata from an old/rebuilt output does not carry forward. However, if persisted/directly-mutated metadata contains `State=stale` with a missing/blank snapshot, all current stale helpers return false and `GeneratedGeometryStaleHealthService` emits no issue. This impossible writer state should be diagnosed as corrupt metadata rather than silently appearing clean.

## Reserved surfaces

- `src/QS3D.Core/Diagnostics/GeneratedGeometryStaleHealthService.cs`
- `tests/QS3D.Core.SmokeTests/GeneratedStaleMetadataIntegritySmoke.cs` (new focused module-initializer regression)
- this claim file

## Intended fix

- In stale-health diagnostics only, detect each known `QS3D.Generated*.State` value equal to canonical `stale` when its paired stale snapshot key is absent/blank.
- Emit Error-level `GENERATED_STALE_METADATA_INVALID` with the element id and affected state-key label.
- Preserve all `ProjectElement.IsGenerated*Stale()` snapshot-match semantics, including stale snapshot mismatch after output rebuild.
- Preserve ordinary stale warnings and all generated handle/ownership/liveness health services.
- Add focused smoke proving missing snapshot is fail-visible while a stale state with a nonblank mismatched snapshot does not get misclassified as corrupt by this lane.

## Validation boundary

Committed deterministic Core smoke coverage plus exact source/diff review. No GitHub Actions dispatch; no licensed BricsCAD V25 runtime PASS claimed.
