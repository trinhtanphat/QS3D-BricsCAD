# Work claim — Preview Review change-domain integrity

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T10:46:00+07:00`
- Baseline main SHA observed: `1fc1a279f71c7a31e514f97ae75c11116d7f4ac7`
- Priority: P1 persisted Preview Review artifact integrity
- Task Key: `CORE-PREVIEW-REVIEW-CHANGE-DOMAIN-INTEGRITY`

## Confirmed defect

`PreviewReviewSnapshotService` and `PreviewReviewSnapshotStore.Load(...)` require every entry `change` value to be nonblank/canonical text, but they do not require it to belong to the producer-owned change vocabulary. Quantity-rule previews emit only `Added`, `Changed`, or `Removed`, and regeneration reviews are sourced from `RevisionService`, which emits the same three values. A persisted artifact can therefore carry a self-consistent but unsupported token such as `Renamed`; query facets and snapshot comparison then treat that invalid state as ordinary review data instead of failing closed.

Recent Preview Review field/category canonicality fixes are complete and do not cover the `change` value domain. Current active claims observed before reservation cover unrelated revision snapshot size, Curtain Frame handles, WallPier freshness, persistence/sidecar and export work.

## Reserved scope

- `src/QS3D.Core/Review/PreviewReviewSnapshot.cs` — shared change-domain validation in create/verify/load only.
- `tests/QS3D.Core.SmokeTests/PreviewReviewChangeDomainIntegritySmoke.cs` — focused auto-registered Core smoke.
- this claim file for close-out.

## Intended contract

- `Added`, `Changed`, and `Removed` remain the only supported persisted Preview Review entry change values.
- Matching is exact/canonical (`Ordinal`); padded/case aliases and unknown tokens fail closed.
- Validate at both in-memory snapshot invariant verification and persisted XML load boundary before query/comparison use.
- Preserve category/field portability checks, fingerprint format/computation, query/filter semantics, comparison ordering, snapshot schema/version and producer behavior.

## Validation plan

- Re-fetch moving `main` and exact PreviewReviewSnapshot source after this claim.
- Add one shared change-domain helper used by snapshot validation and XML load.
- Add focused persisted-artifact smoke for unknown/lowercase/padded tokens plus valid Added/Changed/Removed controls.
- Read back source/test diffs and verify ancestry on current `main`.
- No GitHub Actions dispatch; no executable .NET/full build or BricsCAD V25/V26 runtime PASS claim without actual execution.
