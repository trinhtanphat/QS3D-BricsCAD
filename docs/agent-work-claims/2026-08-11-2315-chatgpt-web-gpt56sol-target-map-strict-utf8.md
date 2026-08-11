# Work claim — Provenance target-map strict UTF-8 decode

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T23:15:00+07:00`
- Baseline main SHA: `15c80902fbb2a1c4d40cc6b0b9ce7d60d998c599`
- Priority: evidence-driven remote-safe Core persisted-data hardening

## Reason

`ProjectInterchangeProvenanceTargetMap.DecodeRecord()` rejects malformed Base64 syntax but decodes valid Base64 through replacement-fallback UTF-8. A syntactically valid target-map record containing invalid UTF-8 bytes can therefore be accepted with replacement characters instead of failing closed as corrupted persisted source→target lineage.

## Reserved scope

Make provenance target-map record decoding fail closed on invalid UTF-8 while preserving record version/layout, Base64 encoding, token keys, source identity checks, target existence checks, rollback behavior, and valid map semantics. Add a dedicated CAD-independent regression smoke.

## Expected surfaces

- `src/QS3D.Core/Export/ProjectInterchangeProvenanceTargetMap.cs`
- `tests/QS3D.Core.SmokeTests/ProjectInterchangeProvenanceTargetMapUtf8Smoke.cs`
- this claim file

## Excluded scope

- No changes to target-map one-to-one policy, interchange imports, remap/apply semantics, native ownership, UI, or BricsCAD V25 runtime.
- No change to valid persisted record encoding.
- No GitHub Actions dispatch.

## Validation plan

- Seed a structurally valid target-map record with a target-id field whose Base64 bytes are `C3 28` (`wyg=`), then assert `ReadTargetElementId()` throws `InvalidOperationException` before using replacement text.
- Seed a valid Unicode source identity mapping to an existing ASCII target id and confirm it remains readable.
- Re-fetch current `main` and target blob before writes; never force-push.
- Record source/static verification only; do not claim an executed repository `dotnet` run in this hosted session.

## Coordination

No current target-map claim or recent `ProjectInterchangeProvenanceTargetMap` commit was found. The preceding provenance source-handle claims are completed and concern a different codec.

## Completion condition

Current `main` rejects invalid UTF-8 target-map fields, preserves valid target-map records, includes focused regression coverage, and this claim is marked `COMPLETED`.
