# Work claim — Provenance target-map canonical target id

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T23:20:00+07:00`
- Baseline main SHA: `4edc480c8e8ad539643eeef33db3c06e23bb95b0`
- Priority: evidence-driven remote-safe Core persisted-data hardening

## Reason

`ProjectInterchangeProvenanceTargetMap.Store()` normalizes source/target semantic ids before encoding them, so a valid persisted target id never contains surrounding whitespace. `ReadTargetElementId()` currently decodes a target id and passes it through the same trimming `Required()` helper, silently accepting a tampered record such as `" T1 "` and resolving it as `T1`. Persisted non-canonical identity should fail closed rather than be repaired during read.

## Reserved scope

Require decoded persisted target ids to already be canonical (no surrounding whitespace) before target lookup, while preserving public `Store()` input normalization, max-length/control-character validation, source identity matching, strict UTF-8 decoding, target existence checks, and valid mapping behavior. Add a dedicated CAD-independent regression smoke.

## Expected surfaces

- `src/QS3D.Core/Export/ProjectInterchangeProvenanceTargetMap.cs`
- `tests/QS3D.Core.SmokeTests/ProjectInterchangeProvenanceTargetMapCanonicalIdSmoke.cs`
- this claim file

## Excluded scope

- No changes to one-to-one mapping semantics, source identity tokenization, interchange import/remap behavior, UI, native ownership, or BricsCAD V25 runtime.
- No change to valid persisted record encoding.
- No GitHub Actions dispatch.

## Validation plan

- Seed a target-map record whose decoded target id is `" T1 "` while target `T1` exists; assert `ReadTargetElementId()` fails closed instead of trimming and resolving it.
- Use public `Store()` with padded caller source/target ids and confirm its existing normalization still writes a mapping that reads back as canonical `T1`.
- Re-fetch current `main` and target blob before writes; never force-push.
- Record source/static verification only; do not claim an executed repository `dotnet` run in this hosted session.

## Coordination

The preceding target-map strict UTF-8 claim is `COMPLETED`. This is a separate canonical persisted-identity lane; no current target-map claim was found.

## Completion condition

Current `main` rejects non-canonical persisted target ids without changing valid public Store normalization, includes focused regression coverage, and this claim is marked `COMPLETED`.
